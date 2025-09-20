#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
 * 唯一标识：1b413c22-a2a4-49df-a185-0d84a2fae179
 * 文件名：FileHashHelper
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/19 23:13:57
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/19 23:13:57
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
{
    /// <summary>
    /// 文件哈希计算辅助类（低内存占用）
    /// </summary>
    public static class FileHashHelper
    {
        /// <summary>
        /// 计算文件MD5哈希（16字节）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>MD5哈希（小写十六进制字符串）</returns>
        public static async Task<string> CalculateMD5Async(string filePath, CancellationToken cancellationToken = default)
        {
            return await CalculateHashAsync(filePath, MD5.Create(), cancellationToken);
        }

        /// <summary>
        /// 计算文件SHA256哈希（32字节）
        /// </summary>
        public static async Task<string> CalculateSHA256Async(string filePath, CancellationToken cancellationToken = default)
        {
            return await CalculateHashAsync(filePath, SHA256.Create(), cancellationToken);
        }

        /// <summary>
        /// 通用哈希计算（基于流分片）
        /// </summary>
        private static async Task<string> CalculateHashAsync(string filePath, HashAlgorithm algorithm, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            // 流分片读取，每次读取缓冲区大小，避免内存溢出
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: FileConstants.DefaultBufferSize, useAsync: true);

            var buffer = new byte[FileConstants.DefaultBufferSize];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                algorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                cancellationToken.ThrowIfCancellationRequested();
            }

            // 完成哈希计算
            algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var hashBytes = algorithm.Hash ?? Array.Empty<byte>();

            // 转换为小写十六进制字符串
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 验证文件哈希（对比计算值与预期值）
        /// </summary>
        public static async Task<bool> VerifyHashAsync(string filePath, string expectedHash, HashType hashType, CancellationToken cancellationToken = default)
        {
            string actualHash = hashType switch
            {
                HashType.MD5 => await CalculateMD5Async(filePath, cancellationToken),
                HashType.SHA256 => await CalculateSHA256Async(filePath, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(hashType), "不支持的哈希类型")
            };

            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 哈希类型枚举
    /// </summary>
    public enum HashType
    {
        MD5,
        SHA256
    }
}