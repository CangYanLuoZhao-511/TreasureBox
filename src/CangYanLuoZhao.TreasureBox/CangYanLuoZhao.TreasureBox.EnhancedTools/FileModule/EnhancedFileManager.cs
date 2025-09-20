#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
 * 唯一标识：27205006-cfe9-408a-a316-deef57d6c3fe
 * 文件名：EnhancedFileManager
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/19 23:23:14
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/19 23:23:14
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
{
    /// <summary>
    /// 增强文件管理工具（整合基础操作+大文件功能）
    /// </summary>
    public class EnhancedFileManager : IDisposable
    {
        private readonly LargeFileProcessor _largeFileProcessor;
        private readonly BreakpointTransferManager _breakpointTransferManager;
        private bool _disposed = false;

        /// <summary>
        /// 初始化增强文件管理器
        /// </summary>
        /// <param name="chunkSize">大文件分片大小（默认4MB）</param>
        /// <param name="progress">进度回调（可选，用于大文件操作）</param>
        /// <param name="progress">进度回调（可选，用于断点续传）</param>
        public EnhancedFileManager(
            int chunkSize = FileConstants.DefaultChunkSize,
            IProgress<ProcessProgress>? progress = null,
            IProgress<FileProgressInfo>? fileProgress = null)
        {
            _largeFileProcessor = new LargeFileProcessor(chunkSize, progress);
            _breakpointTransferManager = new BreakpointTransferManager(progress: fileProgress);
        }

        #region 基础文件操作（高性能异步）
        /// <summary>
        /// 创建文件（写入内容，覆盖已存在文件）
        /// </summary>
        public async Task CreateFileAsync(string filePath, string content, CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                FileShare.None, FileConstants.DefaultBufferSize, useAsync: true);
            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        /// <summary>
        /// 读取文件内容（文本文件）
        /// </summary>
        public async Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, FileConstants.DefaultBufferSize, useAsync: true);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// 复制文件（支持覆盖，高性能流拷贝）
        /// </summary>
        public async Task CopyFileAsync(string sourcePath, string targetPath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("源文件不存在", sourcePath);
            if (File.Exists(targetPath) && !overwrite)
                throw new IOException("目标文件已存在，且不允许覆盖");

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, FileConstants.DefaultBufferSize, useAsync: true);
            using var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write,
                FileShare.None, FileConstants.DefaultBufferSize, useAsync: true);

            await sourceStream.CopyToAsync(targetStream, FileConstants.DefaultBufferSize, cancellationToken);
        }

        /// <summary>
        /// 删除文件（支持递归删除只读文件）
        /// </summary>
        public void DeleteFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            // 解除只读属性
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.IsReadOnly)
                fileInfo.IsReadOnly = false;

            File.Delete(filePath);
        }

        /// <summary>
        /// 移动文件（支持跨目录，原子操作）
        /// </summary>
        public void MoveFile(string sourcePath, string targetPath, bool overwrite = false)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("源文件不存在", sourcePath);
            if (File.Exists(targetPath))
            {
                if (overwrite)
                    DeleteFile(targetPath);
                else
                    throw new IOException("目标文件已存在，且不允许覆盖");
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            File.Move(sourcePath, targetPath);
        }
        #endregion

        #region 目录操作
        /// <summary>
        /// 创建目录（支持多级目录）
        /// </summary>
        public void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        /// <summary>
        /// 删除目录（支持递归删除非空目录）
        /// </summary>
        public void DeleteDirectory(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive);
        }

        /// <summary>
        /// 遍历目录下的所有文件（支持过滤）
        /// </summary>
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("分片目录不存在：" + path);

            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }
        #endregion

        #region 大文件功能（分片/合并/断点续传）
        /// <summary>
        /// 拆分大文件为分片
        /// </summary>
        public async Task<(int TotalChunks, string[] ChunkFilePaths)> SplitLargeFileAsync(
            string sourceFilePath,
            string chunkSaveDir,
            CancellationToken cancellationToken = default)
        {
            return await _largeFileProcessor.SplitFileAsync(sourceFilePath, chunkSaveDir, cancellationToken);
        }

        /// <summary>
        /// 合并分片为完整文件
        /// </summary>
        public async Task MergeLargeFileAsync(
            string chunkDir,
            string targetFilePath,
            int totalChunks,
            CancellationToken cancellationToken = default)
        {
            await _largeFileProcessor.MergeChunksAsync(chunkDir, targetFilePath, totalChunks, cancellationToken);
        }

        /// <summary>
        /// 断点下载文件（需服务器支持Range请求）
        /// </summary>
        public async Task<FileInfo> BreakpointDownloadAsync(
            string remoteUrl,
            string localFilePath,
            System.Net.Http.HttpClient httpClient,
            CancellationToken cancellationToken = default)
        {
            return await _breakpointTransferManager.DownloadFileAsync(remoteUrl, localFilePath, httpClient, cancellationToken);
        }

        /// <summary>
        /// 断点上传文件（需远程接口配合分片合并）
        /// </summary>
        public async Task<string> BreakpointUploadAsync(
            string localFilePath,
            string uploadApiUrl,
            System.Net.Http.HttpClient httpClient,
            CancellationToken cancellationToken = default)
        {
            return await _breakpointTransferManager.UploadFileAsync(localFilePath, uploadApiUrl, httpClient, cancellationToken);
        }

        /// <summary>
        /// 清理过期的断点续传进度文件
        /// </summary>
        public void CleanExpiredTransferProgress(TimeSpan expiration = default)
        {
            _breakpointTransferManager.CleanExpiredProgressFiles(expiration);
        }
        #endregion

        #region 资源释放
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            // 释放托管资源（若有）
            if (disposing)
            {
                // 此处无显式托管资源需释放，可扩展
            }

            _disposed = true;
        }

        ~EnhancedFileManager()
        {
            Dispose(false);
        }
        #endregion
    }
}