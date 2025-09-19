#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.BasicTools.Helpers
 * 唯一标识：94f1f9ee-f849-4a29-ba2b-549f1af7a76d
 * 文件名：Check
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/18 21:33:27
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/18 21:33:27
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CangYanLuoZhao.TreasureBox.BasicTools.Helpers
{
    /// <summary>
    /// Check 的摘要说明
    /// </summary>
    public class Check
    {
        /// <summary>
        /// 判断字符串是否为Null、空
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNull(string? s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// 判断字符串是否不为Null、空
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool NotNull(string? s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// 判断集合是否为Null、空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool IsNull<T>(IEnumerable<T> values)
        {
            return values is null || !values.Any();
        }

        /// <summary>
        /// 判断对象是否为Null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNullObject(object? obj)
        {
            return obj is null;
        }

        /// <summary>
        /// 判断对象是否不为Null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool NotNullObject(object? obj)
        {
            return obj != null;
        }

        /// <summary>
        /// 判断DataTable是否为Null、空
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public bool IsNullTable(DataTable dataTable)
        {
            return dataTable == null || dataTable.Rows.Count == 0;
        }

        /// <summary>
        /// 判断DataTable是否不为Null、空
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        public bool NotNullTable(DataTable dataTable)
        {
            return dataTable != null && dataTable.Rows.Count > 0;
        }

        /// <summary>
        /// 判断DataSet是否为Null、空
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public bool IsNullDataSet(DataSet dataSet)
        {
            return dataSet == null || dataSet.Tables.Count == 0;
        }

        /// <summary>
        /// 判断DataSet是否不为Null、空
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public bool NotNullDataSet(DataSet dataSet)
        {
            return dataSet != null && dataSet.Tables.Count > 0;
        }

        /// <summary>
        /// 判断DataRow数组是否为Null、空
        /// </summary>
        /// <param name="dataRows"></param>
        /// <returns></returns>
        public bool IsNullRows(DataRow[] dataRows)
        {
            return dataRows == null || dataRows.Length == 0;
        }

        /// <summary>
        /// 判断DataRow数组是否不为Null、空
        /// </summary>
        /// <param name="dataRows"></param>
        /// <returns></returns>
        public bool NotNullRows(DataRow[] dataRows)
        {
            return dataRows != null && dataRows.Length > 0;
        }

        /// <summary>
        /// 判断集合是否不为Null、空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool NotNull<T>(IEnumerable<T> values)
        {
            return values != null && values.Any();
        }
    }
}