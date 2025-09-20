#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
 * 唯一标识：78ccf1ce-493a-49fb-9546-b45f1e3dd36f
 * 文件名：BreakpointTransferManager
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/19 23:22:13
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/19 23:22:13
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
{
    /// <summary>
    /// 断点续传管理器（支持上传/下载）
    /// </summary>
    public class BreakpointTransferManager
    {
        private readonly string _progressSaveDir; // 进度文件保存目录
        private readonly int _chunkSize;
        private readonly IProgress<FileProgressInfo>? _progress; // 进度回调（包含完整进度信息）
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 初始化断点续传管理器
        /// </summary>
        /// <param name="progressSaveDir">进度文件保存目录（默认：AppData/Local/FileTransferProgress）</param>
        /// <param name="chunkSize">分片大小（默认4MB）</param>
        /// <param name="progress">进度回调（实时返回进度信息）</param>
        public BreakpointTransferManager(
            string? progressSaveDir = null,
            int chunkSize = FileConstants.DefaultChunkSize,
            IProgress<FileProgressInfo>? progress = null)
        {
            _progressSaveDir = string.IsNullOrWhiteSpace(progressSaveDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileTransferProgress")
                : progressSaveDir;
            _chunkSize = chunkSize;
            _progress = progress;

            // 创建进度文件目录
            Directory.CreateDirectory(_progressSaveDir);
        }

        #region 断点下载（从远程URL到本地文件）
        /// <summary>
        /// 断点下载文件（支持HTTP Range请求，需服务器支持）
        /// </summary>
        /// <param name="remoteUrl">远程文件URL（需支持Range请求）</param>
        /// <param name="localFilePath">本地保存路径</param>
        /// <param name="httpClient">HTTP客户端（外部传入，便于统一配置）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载完成后的文件信息</returns>
        public async Task<FileInfo> DownloadFileAsync(
            string remoteUrl,
            string localFilePath,
            System.Net.Http.HttpClient httpClient,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remoteUrl))
                throw new ArgumentNullException(nameof(remoteUrl));
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            // 1. 获取或创建进度信息
            var progressInfo = await GetOrCreateProgressInfoAsync(remoteUrl, localFilePath, cancellationToken);
            if (progressInfo.Status == 2) // 已完成
            {
                _progress?.Report(progressInfo);
                return new FileInfo(localFilePath);
            }

            // 2. 打开本地文件流（追加模式，支持断点续传）
            using var localStream = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.None, FileConstants.DefaultBufferSize, useAsync: true);
            localStream.Seek(progressInfo.ProcessedSize, SeekOrigin.Begin); // 跳转到已处理位置

            // 3. 发送Range请求，获取剩余文件内容
            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, remoteUrl);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(progressInfo.ProcessedSize, progressInfo.TotalSize - 1);

            using var response = await httpClient.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            // 4. 读取远程流并写入本地
            using var remoteStream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[FileConstants.DefaultBufferSize];
            int bytesRead;

            while ((bytesRead = await remoteStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await localStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                progressInfo.ProcessedSize += bytesRead;
                progressInfo.LastProcessTime = DateTime.Now;
                progressInfo.Status = 1; // 处理中

                // 更新进度（回调+本地文件）
                _progress?.Report(progressInfo);
                await SaveProgressInfoAsync(progressInfo, cancellationToken);
            }

            // 5. 下载完成，标记状态
            progressInfo.Status = 2; // 已完成
            await SaveProgressInfoAsync(progressInfo, cancellationToken);
            _progress?.Report(progressInfo);

            return new FileInfo(localFilePath);
        }
        #endregion

        #region 断点上传（从本地文件到远程接口）
        /// <summary>
        /// 断点上传文件（基于分片上传，需远程接口配合）
        /// </summary>
        /// <param name="localFilePath">本地文件路径</param>
        /// <param name="uploadApiUrl">远程上传接口URL（需支持分片上传：参数含chunkIndex、totalChunks、fileId）</param>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>远程返回的文件标识（如文件ID）</returns>
        public async Task<string> UploadFileAsync(
            string localFilePath,
            string uploadApiUrl,
            System.Net.Http.HttpClient httpClient,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException("本地文件不存在", localFilePath);
            if (string.IsNullOrWhiteSpace(uploadApiUrl))
                throw new ArgumentNullException(nameof(uploadApiUrl));
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            // 1. 获取文件信息与进度
            var localFileInfo = new FileInfo(localFilePath);
            var fileId = await FileHashHelper.CalculateMD5Async(localFilePath, cancellationToken); // 用文件MD5作为唯一标识
            var progressInfo = await GetOrCreateProgressInfoAsync(fileId, localFilePath, cancellationToken, localFileInfo.Length);
            if (progressInfo.Status == 2)
            {
                _progress?.Report(progressInfo);
                return fileId; // 已上传完成，返回文件标识
            }

            // 2. 初始化分片处理器
            var largeFileProcessor = new LargeFileProcessor(progressInfo.ChunkSize);
            var totalChunks = (int)Math.Ceiling((double)progressInfo.TotalSize / progressInfo.ChunkSize);
            var tempChunkDir = Path.Combine(_progressSaveDir, $"temp-chunks-{fileId}");
            Directory.CreateDirectory(tempChunkDir);

            try
            {
                // 3. 拆分文件为分片（仅拆分未完成的分片）
                var (_, chunkPaths) = await largeFileProcessor.SplitFileAsync(localFilePath, tempChunkDir, cancellationToken);

                // 4. 上传未完成的分片
                for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    if (progressInfo.CompletedChunkIndexes.Contains(chunkIndex))
                        continue; // 跳过已完成的分片

                    cancellationToken.ThrowIfCancellationRequested();

                    // 读取分片文件并上传
                    var chunkPath = chunkPaths[chunkIndex];
                    using var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read,
                        FileShare.Read, FileConstants.DefaultBufferSize, useAsync: true);

                    // 构建表单数据（包含分片信息）
                    using var formData = new System.Net.Http.MultipartFormDataContent();
                    formData.Add(new System.Net.Http.StreamContent(chunkStream), "chunkFile", Path.GetFileName(chunkPath));
                    formData.Add(new System.Net.Http.StringContent(fileId), "fileId");
                    formData.Add(new System.Net.Http.StringContent(chunkIndex.ToString()), "chunkIndex");
                    formData.Add(new System.Net.Http.StringContent(totalChunks.ToString()), "totalChunks");

                    // 发送上传请求
                    var response = await httpClient.PostAsync(uploadApiUrl, formData, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    // 标记分片为已完成
                    progressInfo.CompletedChunkIndexes.Add(chunkIndex);
                    progressInfo.ProcessedSize += new FileInfo(chunkPath).Length;
                    progressInfo.LastProcessTime = DateTime.Now;
                    progressInfo.Status = 1;

                    // 更新进度
                    _progress?.Report(progressInfo);
                    await SaveProgressInfoAsync(progressInfo, cancellationToken);
                }

                // 5. 所有分片上传完成，通知服务器合并
                var mergeResponse = await httpClient.PostAsync($"{uploadApiUrl}/merge",
                    new System.Net.Http.FormUrlEncodedContent(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string>("fileId", fileId),
                        new System.Collections.Generic.KeyValuePair<string, string>("totalChunks", totalChunks.ToString())
                    }), cancellationToken);
                mergeResponse.EnsureSuccessStatusCode();

                // 6. 上传完成
                progressInfo.Status = 2;
                await SaveProgressInfoAsync(progressInfo, cancellationToken);
                _progress?.Report(progressInfo);

                return await mergeResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                // 标记为中断状态
                progressInfo.Status = 3;
                progressInfo.LastProcessTime = DateTime.Now;
                await SaveProgressInfoAsync(progressInfo, cancellationToken);
                throw new InvalidOperationException("文件上传中断", ex);
            }
            finally
            {
                // 清理临时分片目录
                if (Directory.Exists(tempChunkDir))
                {
                    Directory.Delete(tempChunkDir, recursive: true);
                }
            }
        }
        #endregion

        #region 进度信息管理（本地JSON文件）
        /// <summary>
        /// 获取或创建进度信息（从本地文件加载，无则创建）
        /// </summary>
        private async Task<FileProgressInfo> GetOrCreateProgressInfoAsync(
            string fileId,
            string localFilePath,
            CancellationToken cancellationToken,
            long? totalSize = null)
        {
            var progressFilePath = GetProgressFilePath(fileId);

            // 若进度文件存在，加载并校验
            if (File.Exists(progressFilePath))
            {
                try
                {
                    using var stream = new FileStream(progressFilePath, FileMode.Open, FileAccess.Read,
                        FileShare.Read, FileConstants.DefaultBufferSize, useAsync: true);
                    var progressInfo = await JsonSerializer.DeserializeAsync<FileProgressInfo>(stream, _jsonOptions, cancellationToken);
                    if (progressInfo != null && !string.IsNullOrEmpty(progressInfo.FileId))
                    {
                        // 校验文件是否存在（本地文件被删除则重新开始）
                        if (!string.IsNullOrEmpty(localFilePath) && !File.Exists(localFilePath))
                        {
                            return CreateNewProgressInfo(fileId, totalSize ?? 0);
                        }
                        return progressInfo;
                    }
                }
                catch (JsonException)
                {
                    // 进度文件损坏，删除并重新创建
                    File.Delete(progressFilePath);
                }
            }

            // 创建新进度信息
            var fileTotalSize = totalSize ?? (File.Exists(localFilePath) ? new FileInfo(localFilePath).Length : 0);
            return CreateNewProgressInfo(fileId, fileTotalSize);
        }

        /// <summary>
        /// 创建新的进度信息
        /// </summary>
        private FileProgressInfo CreateNewProgressInfo(string fileId, long totalSize)
        {
            return new FileProgressInfo
            {
                FileId = fileId,
                TotalSize = totalSize,
                ProcessedSize = 0,
                ChunkSize = _chunkSize,
                LastProcessTime = DateTime.Now,
                Status = 0
            };
        }

        /// <summary>
        /// 保存进度信息到本地JSON文件
        /// </summary>
        private async Task SaveProgressInfoAsync(FileProgressInfo progressInfo, CancellationToken cancellationToken)
        {
            var progressFilePath = GetProgressFilePath(progressInfo.FileId);
            using var stream = new FileStream(progressFilePath, FileMode.Create, FileAccess.Write,
                FileShare.None, FileConstants.DefaultBufferSize, useAsync: true);
            await JsonSerializer.SerializeAsync(stream, progressInfo, _jsonOptions, cancellationToken);
        }

        /// <summary>
        /// 获取进度文件路径
        /// </summary>
        private string GetProgressFilePath(string fileId)
        {
            // 用文件ID哈希作为文件名，避免特殊字符
            var safeFileName = FileHashHelper.CalculateMD5Async(fileId).Result + FileConstants.ProgressFileSuffix;
            return Path.Combine(_progressSaveDir, safeFileName);
        }

        /// <summary>
        /// 清理过期的进度文件（默认清理3天前的中断任务）
        /// </summary>
        public void CleanExpiredProgressFiles(TimeSpan expiration = default)
        {
            expiration = expiration == default ? TimeSpan.FromDays(3) : expiration;
            var cutoffTime = DateTime.Now.Subtract(expiration);

            foreach (var progressFile in Directory.GetFiles(_progressSaveDir, $"*{FileConstants.ProgressFileSuffix}"))
            {
                try
                {
                    var fileInfo = new FileInfo(progressFile);
                    if (fileInfo.LastWriteTime < cutoffTime)
                    {
                        File.Delete(progressFile);
                    }
                }
                catch (IOException)
                {
                    // 忽略正在使用的文件
                }
            }
        }
        #endregion
    }
}