#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.RemoteRequestUtils
 * 唯一标识：1456174f-4b47-4af5-b40a-403ac53d8053
 * 文件名：RemoteResponse
 * 当前用户域：MM-202402051433
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/18 23:02:49
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/18 23:02:49
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CangYanLuoZhao.TreasureBox.RemoteRequestUtils
{
    /// <summary>
    /// 远程请求响应泛型类
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    public class RemoteResponse<T>
    {
        /// <summary>
        /// 是否请求成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 响应数据（成功时非null）
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// HTTP 状态码
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 响应消息（失败时非null）
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 原始响应内容（调试用）
        /// </summary>
        public string? RawContent { get; set; }

        /// <summary>
        /// 异常信息（请求异常时非null）
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 请求耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="data">响应数据</param>
        /// <param name="statusCode">HTTP 状态码</param>
        /// <param name="rawContent">原始内容</param>
        /// <param name="duration">请求耗时</param>
        /// <returns>成功响应实例</returns>
        public static RemoteResponse<T> Success(T data, int statusCode, string? rawContent, TimeSpan duration)
        {
            return new RemoteResponse<T>
            {
                IsSuccess = true,
                Data = data,
                StatusCode = statusCode,
                RawContent = rawContent,
                Duration = duration
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        /// <param name="message">失败消息</param>
        /// <param name="statusCode">HTTP 状态码（无则传0）</param>
        /// <param name="rawContent">原始内容</param>
        /// <param name="exception">异常信息</param>
        /// <param name="duration">请求耗时</param>
        /// <returns>失败响应实例</returns>
        public static RemoteResponse<T> Failure(string message, int statusCode = 0, string? rawContent = null, Exception? exception = null, TimeSpan duration = default)
        {
            return new RemoteResponse<T>
            {
                IsSuccess = false,
                Message = message,
                StatusCode = statusCode,
                RawContent = rawContent,
                Exception = exception,
                Duration = duration
            };
        }
    }

    /// <summary>
    /// 流数据响应对象（需手动释放流资源）
    /// </summary>
    public class RemoteStreamResponse : IDisposable
    {
        /// <summary>
        /// 是否请求成功（HTTP 2xx 状态码）
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 响应流（核心数据，需手动释放！建议用 using 包裹）
        /// </summary>
        public Stream? ResponseStream { get; set; }

        /// <summary>
        /// HTTP 状态码（如 200、206 断点续传、404 等）
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 响应头（如 Content-Length、Content-Type、ETag 等）
        /// </summary>
        public HttpResponseHeaders? ResponseHeaders { get; set; }

        /// <summary>
        /// 错误消息（请求失败时非空）
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 异常信息（请求异常时非空，如网络错误、超时）
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 请求耗时（从发送到获取响应流的时间）
        /// </summary>
        public TimeSpan Duration { get; set; }

        // ========== 资源释放相关 ==========
        private bool _disposed = false;

        /// <summary>
        /// 释放流资源（必须调用！或用 using 语句）
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            // 释放托管资源：响应流和 HTTP 响应对象
            if (disposing)
            {
                ResponseStream?.Dispose();
                _httpResponse?.Dispose();
            }

            _disposed = true;
        }

        ~RemoteStreamResponse()
        {
            Dispose(false);
        }

        /// <summary>
        /// 内部持有 HTTP 响应对象（用于释放资源）
        /// </summary>
        internal HttpResponseMessage? _httpResponse { get; set; }
    }
}