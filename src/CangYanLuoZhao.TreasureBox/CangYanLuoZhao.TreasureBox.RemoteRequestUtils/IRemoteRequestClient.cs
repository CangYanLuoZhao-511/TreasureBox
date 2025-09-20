#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.RemoteRequestUtils
 * 唯一标识：c419313a-f02e-4eea-aae8-5de16f07ef06
 * 文件名：IRemoteRequestClient
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/18 23:01:56
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/18 23:01:56
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CangYanLuoZhao.TreasureBox.RemoteRequestUtils
{
    /// <summary>
    /// 远程请求客户端接口
    /// </summary>
    public interface IRemoteRequestClient
    {
        /// <summary>
        /// GET 请求
        /// </summary>
        /// <typeparam name="TResult">响应数据类型</typeparam>
        /// <param name="url">请求地址</param>
        /// <param name="headers">请求头</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>泛型响应结果</returns>
        Task<RemoteResponse<TResult>> GetAsync<TResult>(
            string url,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST 请求
        /// </summary>
        /// <typeparam name="TResult">响应数据类型</typeparam>
        /// <typeparam name="TRequest">请求数据类型</typeparam>
        /// <param name="url">请求地址</param>
        /// <param name="data">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>泛型响应结果</returns>
        Task<RemoteResponse<TResult>> PostAsync<TResult, TRequest>(
            string url,
            TRequest data,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default) where TRequest : class;

        /// <summary>
        /// PUT 请求
        /// </summary>
        /// <typeparam name="TResult">响应数据类型</typeparam>
        /// <typeparam name="TRequest">请求数据类型</typeparam>
        /// <param name="url">请求地址</param>
        /// <param name="data">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>泛型响应结果</returns>
        Task<RemoteResponse<TResult>> PutAsync<TResult, TRequest>(
            string url,
            TRequest data,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default) where TRequest : class;

        /// <summary>
        /// DELETE 请求
        /// </summary>
        /// <typeparam name="TResult">响应数据类型</typeparam>
        /// <param name="url">请求地址</param>
        /// <param name="headers">请求头</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>泛型响应结果</returns>
        Task<RemoteResponse<TResult>> DeleteAsync<TResult>(
            string url,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送自定义 HTTP 请求
        /// </summary>
        /// <typeparam name="TResult">响应数据类型</typeparam>
        /// <param name="request">HTTP 请求消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>泛型响应结果</returns>
        Task<RemoteResponse<TResult>> SendAsync<TResult>(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// GET 请求获取流数据（如大文件下载、二进制数据）
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="headers">请求头（如 Range、Authorization）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含流的响应对象（需手动释放流）</returns>
        Task<RemoteStreamResponse> GetStreamAsync(
            string url,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST 流数据（如大文件上传）并获取流响应
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="requestStream">请求体流（如文件流）</param>
        /// <param name="contentType">请求体类型（如 application/octet-stream、multipart/form-data）</param>
        /// <param name="headers">请求头</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含流的响应对象（需手动释放流）</returns>
        Task<RemoteStreamResponse> PostStreamAsync(
            string url,
            Stream requestStream,
            string contentType,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// PUT 流数据（如文件更新）并获取流响应
        /// </summary>
        Task<RemoteStreamResponse> PutStreamAsync(
            string url,
            Stream requestStream,
            string contentType,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// DELETE 请求并获取流响应（部分 API 用流返回删除结果）
        /// </summary>
        Task<RemoteStreamResponse> DeleteStreamAsync(
            string url,
            IDictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送自定义 HTTP 请求（含流）并获取流响应
        /// </summary>
        /// <param name="request">自定义请求消息（可设置 StreamContent 作为请求体）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含流的响应对象（需手动释放流）</returns>
        Task<RemoteStreamResponse> SendStreamAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default);
    }


    /// <summary>
    /// 命名远程请求客户端接口（用于多客户端区分）
    /// </summary>
    public interface INamedRemoteRequestClient : IRemoteRequestClient
    {
        /// <summary>
        /// 客户端名称（与配置对应）
        /// </summary>
        string Name { get; }
    }
}
