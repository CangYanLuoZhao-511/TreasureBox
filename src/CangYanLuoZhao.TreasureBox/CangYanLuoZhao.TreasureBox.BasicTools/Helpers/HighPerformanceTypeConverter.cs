#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.BasicTools.Helpers
 * 唯一标识：b63a7aff-db9d-4150-903f-6ef6427928fa
 * 文件名：HighPerformanceTypeConverter
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/18 22:21:59
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/18 22:21:59
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CangYanLuoZhao.TreasureBox.BasicTools.Helpers
{
    /// <summary>
    /// 高性能类型转换工具类，基于.NET Standard 2.1
    /// 专注于高性能和健壮性，适合高频次类型转换场景
    /// </summary>
    public static class HighPerformanceTypeConverter
    {
        #region 私有字段与缓存

        // 类型转换委托缓存
        private static readonly ConcurrentDictionary<(Type SourceType, Type TargetType), Delegate> _converterCache =
            new ConcurrentDictionary<(Type, Type), Delegate>();

        // 属性映射缓存
        private static readonly ConcurrentDictionary<(Type SourceType, Type TargetType), IEnumerable<(PropertyInfo Source, PropertyInfo Target)>> _propertyMapCache =
            new ConcurrentDictionary<(Type, Type), IEnumerable<(PropertyInfo, PropertyInfo)>>();

        // 基本类型集合
        private static readonly HashSet<Type> _primitiveTypes = new HashSet<Type>
        {
            typeof(string), typeof(char), typeof(bool),
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal),
            typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
            typeof(Guid)
        };

        #endregion

        #region 基本类型安全转换

        /// <summary>
        /// 将对象转换为指定类型（高性能版本）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="value">要转换的值</param>
        /// <param name="defaultValue">转换失败时返回的默认值</param>
        /// <returns>转换后的值或默认值</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ConvertTo<T>(object? value, T defaultValue = default!)
        {
            // 快速路径：值为null
            if (value == null)
            {
                return typeof(T).IsNullableType() ? default! : defaultValue;
            }

            // 快速路径：已经是目标类型
            if (value is T tValue)
            {
                return tValue;
            }

            // 尝试从缓存获取转换器
            var key = (value.GetType(), typeof(T));
            if (_converterCache.TryGetValue(key, out var converter))
            {
                try
                {
                    return ((Func<object, T>)converter)(value);
                }
                catch
                {
                    return defaultValue;
                }
            }

            // 没有缓存的转换器，创建并缓存
            var converterFunc = CreateConverter<T>(value.GetType());
            _converterCache.TryAdd(key, converterFunc);

            try
            {
                return converterFunc(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 为特定源类型和目标类型创建转换器
        /// </summary>
        private static Func<object, T> CreateConverter<T>(Type sourceType)
        {
            Type targetType = typeof(T);
            Type underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // 处理枚举类型
            if (underlyingTargetType.IsEnum)
            {
                return value => (T)Enum.Parse(underlyingTargetType, value.ToString()!, true);
            }

            // 为基本类型创建专用转换器
            if (_primitiveTypes.Contains(underlyingTargetType))
            {
                return CreatePrimitiveConverter<T>(underlyingTargetType);
            }

            // 通用转换器
            return value => (T)Convert.ChangeType(value, underlyingTargetType);
        }

        /// <summary>
        /// 为基本类型创建专用转换器（性能优化）
        /// </summary>
        private static Func<object, T> CreatePrimitiveConverter<T>(Type targetType)
        {
            if (targetType == typeof(int))
                return value => (T)(object)Convert.ToInt32(value);

            if (targetType == typeof(long))
                return value => (T)(object)Convert.ToInt64(value);

            if (targetType == typeof(bool))
                return value => (T)(object)Convert.ToBoolean(value);

            if (targetType == typeof(double))
                return value => (T)(object)Convert.ToDouble(value);

            if (targetType == typeof(decimal))
                return value => (T)(object)Convert.ToDecimal(value);

            if (targetType == typeof(DateTime))
                return value => (T)(object)Convert.ToDateTime(value);

            if (targetType == typeof(string))
                return value => (T)(object)Convert.ToString(value);

            // 默认使用Convert.ChangeType
            return value => (T)Convert.ChangeType(value, targetType);
        }

        #endregion

        #region 基本类型专用转换方法（高性能）

        /// <summary>
        /// 转换为Int32（高性能专用方法）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(object? value, int defaultValue = 0)
        {
            if (value == null)
                return defaultValue;

            // 类型匹配，直接转换
            if (value is int intVal)
                return intVal;

            // 其他数值类型转换
            if (value is long longVal)
                return longVal >= int.MinValue && longVal <= int.MaxValue ? (int)longVal : defaultValue;

            if (value is float floatVal)
                return floatVal >= int.MinValue && floatVal <= int.MaxValue ? (int)floatVal : defaultValue;

            if (value is double doubleVal)
                return doubleVal >= int.MinValue && doubleVal <= int.MaxValue ? (int)doubleVal : defaultValue;

            if (value is decimal decimalVal)
                return decimalVal >= int.MinValue && decimalVal <= int.MaxValue ? (int)decimalVal : defaultValue;

            // 字符串类型转换（避免使用Convert.ToInt32的额外开销）
            if (value is string str)
                return int.TryParse(str, out int result) ? result : defaultValue;

            // 最后尝试通用转换
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 转换为Boolean（高性能专用方法，支持多种表示）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(object? value, bool defaultValue = false)
        {
            if (value == null)
                return defaultValue;

            // 类型匹配，直接返回
            if (value is bool boolVal)
                return boolVal;

            // 数值类型处理
            if (value is int intVal)
                return intVal != 0;

            if (value is long longVal)
                return longVal != 0;

            if (value is float floatVal)
                return floatVal != 0;

            if (value is double doubleVal)
                return doubleVal != 0;

            // 字符串类型处理（优化常见情况）
            if (value is string str)
            {
                var trimmed = str.Trim();
                if (trimmed.Length == 0)
                    return defaultValue;

                if (trimmed.Length == 1)
                {
                    return trimmed[0] == '1' || trimmed[0] == 't' || trimmed[0] == 'T' ||
                           trimmed[0] == 'y' || trimmed[0] == 'Y';
                }

                // 只比较前几个字符提高性能
                if (trimmed.Length >= 4)
                {
                    if (trimmed[0] == 't' && trimmed[1] == 'r' && trimmed[2] == 'u' && trimmed[3] == 'e')
                        return true;
                }

                if (trimmed.Length >= 5)
                {
                    if (trimmed[0] == 'f' && trimmed[1] == 'a' && trimmed[2] == 'l' && trimmed[3] == 's' && trimmed[4] == 'e')
                        return false;
                    if (trimmed[0] == 'y' && trimmed[1] == 'e' && trimmed[2] == 's')
                        return true;
                    if (trimmed[0] == 'n' && trimmed[1] == 'o')
                        return false;
                }
            }

            // 最后尝试通用转换
            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 转换为DateTime（高性能专用方法）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ToDateTime(object? value, DateTime defaultValue = default)
        {
            if (value == null)
                return defaultValue;

            // 类型匹配，直接返回
            if (value is DateTime dateTimeVal)
                return dateTimeVal;

            // 字符串类型处理
            if (value is string str && DateTime.TryParse(str, out DateTime result))
                return result;

            // 数值类型处理（视为OLE自动化日期）
            if (value is double doubleVal)
                return DateTime.FromOADate(doubleVal);

            if (value is decimal decimalVal)
                return DateTime.FromOADate((double)decimalVal);

            // 最后尝试通用转换
            try
            {
                return Convert.ToDateTime(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region 字符串与数组转换

        /// <summary>
        /// 将字符串转换为指定类型的数组（高性能版本）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(string? value, char[]? separators = null, T defaultValue = default!)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<T>();

            // 缓存常用分隔符
            separators ??= new[] { ',', ';', '|', ' ' };

            // 预估数组大小以减少内存分配
            int estimatedSize = Math.Min(value.Length / 2, 1024);
            var result = new List<T>(estimatedSize);

            int start = 0;
            int length = value.Length;

            // 手动解析以避免创建中间字符串数组
            for (int i = 0; i < length; i++)
            {
                if (Array.IndexOf(separators, value[i]) != -1)
                {
                    if (i > start)
                    {
                        string item = value.Substring(start, i - start);
                        result.Add(ConvertTo(item, defaultValue));
                    }
                    start = i + 1;
                }
            }

            // 添加最后一个元素
            if (start < length)
            {
                string item = value.Substring(start);
                result.Add(ConvertTo(item, defaultValue));
            }

            return result.ToArray();
        }

        /// <summary>
        /// 将对象转换为字符串（高性能版本）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(object? value, string? format = null)
        {
            if (value == null)
                return string.Empty;

            // 类型匹配，直接返回
            if (value is string strVal)
                return strVal;

            // 对于基本类型使用专用转换
            if (value is IFormattable formattable)
            {
                return !string.IsNullOrEmpty(format)
                    ? formattable.ToString(format, null)
                    : formattable.ToString() ?? string.Empty;
            }

            // 对于其他类型使用ToString()
            return value.ToString() ?? string.Empty;
        }

        #endregion

        #region 集合转换

        /// <summary>
        /// 将集合转换为指定元素类型的列表（高性能版本）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> ToList<T>(IEnumerable? collection, T defaultValue)
        {
            if (collection == null)
                return new List<T>();

            // 尝试获取已知大小以优化初始容量
            int initialCapacity = collection is ICollection coll ? coll.Count : 4;
            var result = new List<T>(initialCapacity);

            foreach (var item in collection)
            {
                result.Add(ConvertTo(item, defaultValue));
            }

            return result;
        }

        /// <summary>
        /// 转换集合中的所有元素（高性能版本）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TResult> ConvertAll<TSource, TResult>(IEnumerable<TSource>? source, TResult defaultValue = default!)
        {
            if (source == null)
                yield break;

            foreach (var item in source)
            {
                yield return ConvertTo(item, defaultValue);
            }
        }

        #endregion

        #region 对象映射（优化反射性能）

        /// <summary>
        /// 将源对象的属性值映射到目标对象（高性能版本，使用缓存）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDestination Map<TSource, TDestination>(TSource? source, TDestination? destination = null)
            where TSource : class
            where TDestination : class, new()
        {
            if (source == null)
                return destination ?? new TDestination();

            destination ??= new TDestination();

            // 从缓存获取属性映射
            var key = (typeof(TSource), typeof(TDestination));
            var propertyMap = _propertyMapCache.GetOrAdd(key, k => BuildPropertyMap<TSource, TDestination>());

            // 执行属性映射
            foreach (var (sourceProp, destProp) in propertyMap)
            {
                try
                {
                    object? value = sourceProp.GetValue(source);
                    if (value != null)
                    {
                        // 如果类型匹配，直接赋值避免转换开销
                        if (sourceProp.PropertyType == destProp.PropertyType)
                        {
                            destProp.SetValue(destination, value);
                        }
                        else
                        {
                            object? convertedValue = ConvertTo(value, destProp.PropertyType, null);
                            destProp.SetValue(destination, convertedValue);
                        }
                    }
                }
                catch
                {
                    // 忽略映射失败的属性，保证健壮性
                }
            }

            return destination;
        }

        /// <summary>
        /// 构建源类型和目标类型之间的属性映射（只执行一次并缓存）
        /// </summary>
        private static IEnumerable<(PropertyInfo Source, PropertyInfo Target)> BuildPropertyMap<TSource, TDestination>()
        {
            var sourceProps = typeof(TSource)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var destProps = typeof(TDestination)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var destProp in destProps)
            {
                if (sourceProps.TryGetValue(destProp.Name, out var sourceProp))
                {
                    yield return (sourceProp, destProp);
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 通用类型转换方法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? ConvertTo(object? value, Type targetType, object? defaultValue = null)
        {
            if (value == null)
            {
                return targetType.IsNullableType() ? null : defaultValue ?? Activator.CreateInstance(targetType);
            }

            Type valueType = value.GetType();

            // 类型匹配，直接返回
            if (targetType.IsAssignableFrom(valueType))
            {
                return value;
            }

            Type underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // 处理枚举类型
            if (underlyingTargetType.IsEnum)
            {
                return Enum.TryParse(underlyingTargetType, value.ToString(), true, out object? result)
                    ? result
                    : defaultValue;
            }

            // 处理基本类型
            if (_primitiveTypes.Contains(underlyingTargetType))
            {
                return ConvertPrimitiveType(value, underlyingTargetType, defaultValue);
            }

            // 最后尝试通用转换
            try
            {
                return Convert.ChangeType(value, underlyingTargetType);
            }
            catch
            {
                return defaultValue ?? (underlyingTargetType.IsValueType ? Activator.CreateInstance(underlyingTargetType) : null);
            }
        }

        /// <summary>
        /// 基本类型转换（避免使用Convert.ChangeType的额外开销）
        /// </summary>
        private static object? ConvertPrimitiveType(object value, Type targetType, object? defaultValue)
        {
            try
            {
                if (targetType == typeof(int))
                    return ToInt32(value, (int)(defaultValue ?? 0));

                if (targetType == typeof(long))
                    return Convert.ToInt64(value);

                if (targetType == typeof(bool))
                    return ToBoolean(value, (bool)(defaultValue ?? false));

                if (targetType == typeof(double))
                    return Convert.ToDouble(value);

                if (targetType == typeof(decimal))
                    return Convert.ToDecimal(value);

                if (targetType == typeof(DateTime))
                    return ToDateTime(value, (DateTime)(defaultValue ?? default(DateTime)));

                if (targetType == typeof(string))
                    return ToString(value);
            }
            catch
            {
                return defaultValue;
            }

            // 其他基本类型使用Convert.ChangeType
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 检查类型是否为可空类型
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// 尝试转换（不抛出异常）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryConvert<T>(object? value, out T result)
        {
            try
            {
                result = ConvertTo<T>(value);
                return true;
            }
            catch
            {
                result = default!;
                return false;
            }
        }

        /// <summary>
        /// 清除转换缓存（主要用于测试）
        /// </summary>
        public static void ClearCache()
        {
            _converterCache.Clear();
            _propertyMapCache.Clear();
        }

        #endregion
    }
}
