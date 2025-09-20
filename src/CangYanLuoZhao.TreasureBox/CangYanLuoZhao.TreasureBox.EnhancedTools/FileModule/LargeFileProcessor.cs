#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
 * 唯一标识：9f604972-7177-4583-bc6a-b1566af90510
 * 文件名：LargeFileProcessor
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/19 23:16:28
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/19 23:16:28
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
{
    /// <summary>
    /// 大文件分片/合并处理器
    /// </summary>
    public class LargeFileProcessor
    {
        private readonly int _chunkSize;
        private readonly IProgress<ProcessProgress>? _progress;

        /// <summary>
        /// 初始化大文件处理器
        /// </summary>
        /// <param name="chunkSize">分片大小（默认4MB，最小1MB，最大32MB）</param>
        /// <param name="progress">进度回调（实时返回已处理字节数）</param>
        public LargeFileProcessor(int chunkSize = FileConstants.DefaultChunkSize, IProgress<ProcessProgress>? progress = null)
        {
            // 校验分片大小合理性
            _chunkSize = chunkSize < 1 * 1024 * 1024 ? FileConstants.DefaultChunkSize :
                        (chunkSize > 32 * 1024 * 1024 ? 32 * 1024 * 1024 : chunkSize);
            _progress = progress;
        }

        /// <summary>
        /// 拆分大文件为分片
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="chunkSaveDir">分片保存目录（自动创建）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分片信息（总数量、分片路径列表）</returns>
        public async Task<(int TotalChunks, string[] ChunkFilePaths)> SplitFileAsync(
           string sourceFilePath,
           string chunkSaveDir,
           CancellationToken cancellationToken = default)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("源文件不存在", sourceFilePath);

            Directory.CreateDirectory(chunkSaveDir);
            var sourceFileInfo = new FileInfo(sourceFilePath);
            var totalSize = sourceFileInfo.Length; // 关键：获取源文件总大小
            var totalChunks = (int)Math.Ceiling((double)totalSize / _chunkSize);
            var chunkFilePaths = new string[totalChunks];
            long processedSize = 0;

            using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, FileConstants.DefaultBufferSize, useAsync: true);

            for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentChunkSize = chunkIndex == totalChunks - 1
                    ? (int)(totalSize - processedSize)
                    : _chunkSize;

                var chunkFileName = $"{Path.GetFileNameWithoutExtension(sourceFilePath)}" +
                                  string.Format(FileConstants.ChunkFileSuffixTemplate, chunkIndex);
                var chunkFilePath = Path.Combine(chunkSaveDir, chunkFileName);
                chunkFilePaths[chunkIndex] = chunkFilePath;

                using var chunkStream = new FileStream(chunkFilePath, FileMode.Create, FileAccess.Write,
                    FileShare.None, FileConstants.DefaultBufferSize, useAsync: true);

                var buffer = new byte[currentChunkSize];
                var bytesRead = await sourceStream.ReadAsync(buffer, 0, currentChunkSize, cancellationToken);
                await chunkStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                // 4. 传递完整进度数据（包含 totalSize）
                processedSize += bytesRead;
                _progress?.Report(new ProcessProgress
                {
                    ProcessedSize = processedSize,
                    TotalSize = totalSize, // 关键：传递总大小
                    StatusDesc = $"分片 {chunkIndex + 1}/{totalChunks}"
                });
            }

            return (totalChunks, chunkFilePaths);
        }

        /// <summary>
        /// 合并分片为完整文件
        /// </summary>
        /// <param name="chunkDir">分片目录（包含所有分片文件）</param>
        /// <param name="targetFilePath">目标文件路径（覆盖已存在文件）</param>
        /// <param name="totalChunks">预期总分片数（校验完整性）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task MergeChunksAsync(
             string chunkDir,
             string targetFilePath,
             int totalChunks,
             CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(chunkDir))
                throw new DirectoryNotFoundException(string.Concat("分片目录不存在", chunkDir));

            var chunkFiles = Directory.GetFiles(chunkDir, $"*{string.Format(FileConstants.ChunkFileSuffixTemplate, "*")}")
                .Select(path => new
                {
                    Path = path,
                    Index = int.TryParse(Path.GetExtension(path).Split('-').Last().Trim('.'), out int idx) ? idx : -1
                })
                .Where(item => item.Index >= 0 && item.Index < totalChunks)
                .OrderBy(item => item.Index)
                .ToList();

            if (chunkFiles.Count != totalChunks)
                throw new InvalidDataException($"分片数量不完整：预期{totalChunks}个，实际{chunkFiles.Count}个");

            // 6. 关键：计算所有分片的总大小（合并后的文件总大小）
            long totalSize = chunkFiles.Sum(f => new FileInfo(f.Path).Length);
            long processedSize = 0;

            using var targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write,
                FileShare.None, FileConstants.DefaultBufferSize, useAsync: true);

            for (int i = 0; i < chunkFiles.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunkFile = chunkFiles[i];

                using var chunkStream = new FileStream(chunkFile.Path, FileMode.Open, FileAccess.Read,
                    FileShare.Read, FileConstants.DefaultBufferSize, useAsync: true);

                await chunkStream.CopyToAsync(targetStream, FileConstants.DefaultBufferSize, cancellationToken);

                // 7. 传递完整进度数据
                processedSize += chunkStream.Length;
                _progress?.Report(new ProcessProgress
                {
                    ProcessedSize = processedSize,
                    TotalSize = totalSize, // 传递总大小
                    StatusDesc = $"合并分片 {i + 1}/{totalChunks}"
                });
            }
        }


        /// <summary>
        /// 清理分片文件（合并完成后调用）
        /// </summary>
        public void CleanChunks(string chunkDir)
        {
            if (!Directory.Exists(chunkDir)) return;

            var chunkFiles = Directory.GetFiles(chunkDir, $"*{string.Format(FileConstants.ChunkFileSuffixTemplate, "*")}");
            foreach (var file in chunkFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException ex)
                {
                    // 忽略正在使用的文件（避免并发冲突）
                    Console.WriteLine($"清理分片文件失败：{file}，原因：{ex.Message}");
                }
            }

            // 若目录为空，删除目录
            if (!Directory.EnumerateFiles(chunkDir).Any())
            {
                Directory.Delete(chunkDir);
            }
        }
    }
}