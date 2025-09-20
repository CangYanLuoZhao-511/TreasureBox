#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
 * 唯一标识：aa6e9290-793f-4145-ae40-7dc9f3a224dd
 * 文件名：ProcessProgress
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/20 18:21:21
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/20 18:21:21
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;

namespace CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
{
    /// <summary>
    /// 完整的进度数据（包含已处理大小和总大小，用于计算百分比）
    /// </summary>
    public struct ProcessProgress
    {
        /// <summary>
        /// 已处理大小（字节）
        /// </summary>
        public long ProcessedSize { get; set; }

        /// <summary>
        /// 总大小（字节）
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// 处理状态描述（可选，如“分片1/20”）
        /// </summary>
        public string? StatusDesc { get; set; }

        /// <summary>
        /// 计算进度百分比（0~100）
        /// </summary>
        public double ProgressPercent => TotalSize > 0 ? Math.Round((double)ProcessedSize / TotalSize * 100, 2) : 0;
    }
}