#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.BasicTools.Extensions
 * 唯一标识：1bf99f75-29dc-4735-ba49-e1b21b59e012
 * 文件名：StringExtensions
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/18 21:27:26
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/18 21:27:26
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using CangYanLuoZhao.TreasureBox.BasicTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CangYanLuoZhao.TreasureBox.BasicTools.Extensions
{
    /// <summary>
    /// 字符串扩展
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// 与字符串进行比较，忽略大小写
        /// </summary>
        /// <param name="s"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string s, string value)
        {
            return s.Equals(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 首字母转小写
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string FirstCharToLower(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            string str = s.First().ToString().ToLower() + s.Substring(1);
            return str;
        }

        /// <summary>
        /// 首字母转大写
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string FirstCharToUpper(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            string str = s.First().ToString().ToUpper() + s.Substring(1);
            return str;
        }

        /// <summary>
        /// 转为Base64，UTF-8格式
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToBase64(this string s)
        {
            return s.ToBase64(Encoding.UTF8);
        }

        /// <summary>
        /// 转为Base64
        /// </summary>
        /// <param name="s"></param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static string ToBase64(this string s, Encoding encoding)
        {
            if (Check.IsNull(s))
                return string.Empty;

            byte[] bytes = encoding.GetBytes(s);

            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// 转换路径
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string? ToFilePath(this string s)
        {
            return Check.IsNull(s) ? null : s.Replace("/", @"\");
        }

        /// <summary>
        /// 动态生成编码（支持指定数字部分位数，自动处理包含前缀的编码）
        /// 格式：[前缀][A-Z][数字]{digitLength} 或 [前缀][数字]{digitLength}
        /// </summary>
        /// <param name="prefix">编码前缀（如GY）</param>
        /// <param name="code">基础编码（可包含前缀，如GYA001、AB99999、00001）</param>
        /// <param name="digitLength">数字部分位数（1~5，默认3位）</param>
        /// <returns>生成的新编码，若达到上限（如Z99999且digitLength=5）则返回null</returns>
        public static string DynamicGeneratorCode(this string prefix, string code, int digitLength = 3)
        {
            try
            {
                // 验证数字部分位数范围（1~5）
                if (digitLength < 1 || digitLength > 5)
                {
                    throw new ArgumentOutOfRangeException(nameof(digitLength), "数字部分位数必须为1~5位");
                }

                // 验证基础编码不为空
                if (Check.IsNull(code))
                {
                    throw new ArgumentException("基础编码不能为空", nameof(code));
                }

                // 处理编码：去除空白，并自动截掉前缀（如果包含）
                string trimmedCode = code.Trim();
                string pureCode = trimmedCode.StartsWith(prefix, StringComparison.Ordinal)
                    ? trimmedCode.Substring(prefix.Length) // 截掉前缀
                    : trimmedCode; // 不包含前缀则使用原编码

                // 验证截掉前缀后的纯编码不为空
                if (string.IsNullOrWhiteSpace(pureCode))
                {
                    throw new ArgumentException("编码去除前缀后不能为空", nameof(code));
                }

                char firstChar = pureCode[0];

                // 场景1：首字符为字母（如A001、B99999）
                if (char.IsLetter(firstChar))
                {
                    // 验证字母为大写A-Z
                    if (!char.IsUpper(firstChar))
                    {
                        throw new ArgumentException("首字母必须为大写字母（A-Z）", nameof(code));
                    }

                    // 提取数字部分（去除首字母后剩余字符）
                    string numberPart = pureCode.Substring(1);

                    // 验证数字部分长度与指定位数一致
                    if (numberPart.Length != digitLength)
                    {
                        throw new ArgumentException(
                            $"数字部分长度必须为{digitLength}位（当前：{numberPart.Length}位）",
                            nameof(code)
                        );
                    }

                    // 解析数字部分
                    if (!int.TryParse(numberPart, out int maxNumber))
                    {
                        throw new ArgumentException("数字部分必须为纯数字", nameof(code));
                    }

                    // 检查是否需要进位（如999→1000时位数增加）
                    if ((maxNumber + 1).ToString().Length > digitLength)
                    {
                        // 字母未到Z，字母自增+数字重置为1（补0至指定位数）
                        if (firstChar != 'Z')
                        {
                            char nextChar = (char)((byte)firstChar + 1);
                            string newNumber = "1".PadLeft(digitLength, '0');
                            return $"{prefix}{nextChar}{newNumber}";
                        }
                        // 字母为Z且数字溢出，返回null（已达上限）
                        return string.Empty;
                    }
                    // 无需进位，仅数字自增（补0至指定位数）
                    else
                    {
                        string newNumber = (maxNumber + 1).ToString().PadLeft(digitLength, '0');
                        return $"{prefix}{firstChar}{newNumber}";
                    }
                }
                // 场景2：纯数字编码（如001、99999）
                else
                {
                    // 验证数字部分长度与指定位数一致
                    if (pureCode.Length != digitLength)
                    {
                        throw new ArgumentException(
                            $"纯数字编码长度必须为{digitLength}位（当前：{pureCode.Length}位）",
                            nameof(code)
                        );
                    }

                    // 解析数字并自增
                    if (!int.TryParse(pureCode, out int currentNumber))
                    {
                        throw new ArgumentException("纯数字编码必须为纯数字", nameof(code));
                    }

                    string newNumber = (currentNumber + 1).ToString().PadLeft(digitLength, '0');
                    return $"{prefix}{newNumber}";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"编码生成失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 生成连续的编码
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="startCode"></param>
        /// <param name="digitLength"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<string>? GenerateConsecutiveCodes(this string prefix, string startCode, int digitLength, int count)
        {
            var codes = new List<string>();
            string currentCode = startCode;

            try
            {

                for (int i = 0; i < count; i++)
                {
                    var nextCode = DynamicGeneratorCode(prefix, currentCode, digitLength) ?? string.Empty;

                    // 检测纯数字场景溢出（如999→1000）
                    if (Check.NotNull(nextCode))
                    {
                        string pureCode = nextCode.StartsWith(prefix)
                            ? nextCode.Substring(prefix.Length)
                            : nextCode;

                        // 验证数字部分长度合法性
                        if (pureCode.All(char.IsDigit) && pureCode.Length > digitLength)
                        {
                            Console.WriteLine($"终止：{nextCode} 数字部分溢出（{digitLength}位上限）");
                            break;
                        }
                    }

                    // 处理边界结果
                    if (nextCode == null)
                    {
                        Console.WriteLine($"终止：{currentCode} 已达编码上限");
                        break;
                    }

                    codes.Add(nextCode);
                    currentCode = nextCode;
                }

            }

            catch
            {
                throw;
            }

            return codes;
        }
    }
}