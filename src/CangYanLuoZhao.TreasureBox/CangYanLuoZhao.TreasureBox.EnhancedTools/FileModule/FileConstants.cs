#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
 * 唯一标识：e4b63253-b97c-4ff6-ae39-0b95c1fb49fb
 * 文件名：FileConstants
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/19 23:11:37
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/19 23:11:37
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

namespace CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule
{
    /// <summary>
    /// 文件管理工具常量
    /// </summary>
    internal static class FileConstants
    {
        /// <summary>
        /// 默认分片大小（4MB，平衡IO效率与内存占用）
        /// </summary>
        public const int DefaultChunkSize = 4 * 1024 * 1024;

        /// <summary>
        /// 流操作默认缓冲区大小（80KB，优化IO性能）
        /// </summary>
        public const int DefaultBufferSize = 81920;

        /// <summary>
        /// 断点续传进度文件后缀
        /// </summary>
        public const string ProgressFileSuffix = ".transfer-progress.json";

        /// <summary>
        /// 分片文件后缀（格式：.chunk-{索引}）
        /// </summary>
        public const string ChunkFileSuffixTemplate = ".chunk-{0}";
    }
}