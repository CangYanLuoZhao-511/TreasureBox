#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
 * 唯一标识：d12a93ce-26db-47b6-8493-e64663544331
 * 文件名：FileProgressInfo
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/19 23:12:48
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/19 23:12:48
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.Collections.Generic;

namespace CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
{
    /// <summary>
    /// 断点续传进度信息（序列化到本地文件，支持中断后恢复）
    /// </summary>
    public class FileProgressInfo
    {
        /// <summary>
        /// 文件唯一标识（本地路径/远程URL，避免同名冲突）
        /// </summary>
        public string FileId { get; set; } = string.Empty;

        /// <summary>
        /// 文件总大小（字节）
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// 已处理大小（字节）
        /// </summary>
        public long ProcessedSize { get; set; }

        /// <summary>
        /// 分片大小（字节）
        /// </summary>
        public int ChunkSize { get; set; } = FileConstants.DefaultChunkSize;

        /// <summary>
        /// 已完成的分片索引（用于分片传输场景）
        /// </summary>
        public HashSet<int> CompletedChunkIndexes { get; set; } = new HashSet<int>();

        /// <summary>
        /// 最后处理时间（用于过期进度清理）
        /// </summary>
        public DateTime LastProcessTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 处理状态（0：未开始，1：处理中，2：已完成，3：已中断）
        /// </summary>
        public int Status { get; set; } = 0;

        /// <summary>
        /// 已消耗的处理时间（用于计算速度、预估剩余时间）
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    }
}