#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.RemoteRequestUtils
 * 唯一标识：cab8dd92-5d4e-4671-9d6a-df4132255877
 * 文件名：RemoteRequestClient
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/18 23:04:25
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/18 23:04:25
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CangYanLuoZhao.TreasureBox.RemoteRequestUtils
{
    /// <summary>
    /// 基于 IHttpClientFactory + Polly 的远程请求客户端
    /// </summary>
    public class RemoteRequestClient : INamedRemoteRequestClient
    {
        #region 私有字段
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RemoteRequestClient>? _logger;
        private readonly string _clientName;
        private readonly int _retryCount;
        private readonly int _retryDelayMs;
        private readonly int _circuitBreakDurationSec;
        private readonly int _timeoutSec;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;
        private readonly AsyncPolicy<HttpResponseMessage> _combinedPolicy;
        #endregion

        #region 构造函数
        public RemoteRequestClient(
            IHttpClientFactory httpClientFactory,
            ILogger<RemoteRequestClient>? logger,
            string clientName = "default",
            int retryCount = 3,
            int retryDelayMs = 1000,
            int circuitBreakDurationSec = 30,
            int timeoutSec = 30,
            JsonSerializerOptions? jsonOptions = null)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger;
            _clientName = string.IsNullOrWhiteSpace(clientName) ? "default" : clientName;
            _retryCount = Math.Max(retryCount, 0);
            _retryDelayMs = Math.Max(retryDelayMs, 100);
            _circuitBreakDurationSec = Math.Max(circuitBreakDurationSec, 5);
            _timeoutSec = Math.Max(timeoutSec, 3);

            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: _retryCount,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(_retryDelayMs * attempt),
                    onRetryAsync: async (result, delay, attempt, context) =>
                    {
                        var requestUrl = context["RequestUrl"]?.ToString() ?? "未知地址";
                        var exceptionMsg = result.Exception != null ? result.Exception.Message : "HTTP非成功状态码";
                        _logger?.LogWarning(
                            new EventId(1001, "RequestRetry"),
                            result.Exception,
                            "请求重试 {Attempt}/{TotalRetry}，延迟 {DelayMs}ms，地址：{Url}，原因：{Reason}",
                            attempt, _retryCount, delay.TotalMilliseconds, requestUrl, exceptionMsg
                        );
                        await Task.CompletedTask;
                    }
                );

            _circuitBreakerPolicy = Policy
      .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
      .Or<HttpRequestException>()
      .CircuitBreakerAsync(
          handledEventsAllowedBeforeBreaking: 3,
          durationOfBreak: TimeSpan.FromSeconds(_circuitBreakDurationSec),
          onBreak: (result, breakDuration) =>
          {
              var requestUrl = result.Result?.RequestMessage?.RequestUri?.ToString() ?? "未知地址";
              _logger?.LogError(
                  new EventId(1002, "CircuitBreakOpen"),
                  result.Exception,
                  "熔断触发：服务不可用，持续 {BreakSec}s，地址：{Url}",
                  breakDuration.TotalSeconds, requestUrl
              );
          },
          onReset: () =>
          {
              _logger?.LogInformation(1003, "熔断重置：服务恢复可用");
          }
      );

            _combinedPolicy = Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy);
        }
        #endregion

        #region INamedRemoteRequestClient 实现
        public string Name => _clientName;
        #endregion

        #region IRemoteRequestClient 实现（核心请求方法）
        public async Task<RemoteResponse<TResult>> GetAsync<TResult>(string url, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url), "请求地址不能为空");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeadersToRequest(request, headers);
            return await SendWithPolicyAsync<TResult>(request, cancellationToken);
        }

        public async Task<RemoteResponse<TResult>> PostAsync<TResult, TRequest>(string url, TRequest data, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TRequest : class
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(data, _jsonOptions),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            AddHeadersToRequest(request, headers);
            return await SendWithPolicyAsync<TResult>(request, cancellationToken);
        }

        public async Task<RemoteResponse<TResult>> PutAsync<TResult, TRequest>(string url, TRequest data, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TRequest : class
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(data, _jsonOptions),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            AddHeadersToRequest(request, headers);
            return await SendWithPolicyAsync<TResult>(request, cancellationToken);
        }

        public async Task<RemoteResponse<TResult>> DeleteAsync<TResult>(string url, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            AddHeadersToRequest(request, headers);
            return await SendWithPolicyAsync<TResult>(request, cancellationToken);
        }

        public async Task<RemoteResponse<TResult>> SendAsync<TResult>(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.RequestUri == null)
                throw new ArgumentException("请求地址（RequestUri）不能为空", nameof(request));

            return await SendWithPolicyAsync<TResult>(request, cancellationToken);
        }
        #endregion



        #region stream

        public async Task<RemoteStreamResponse> GetStreamAsync(string url, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url), "请求地址不能为空");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeadersToRequest(request, headers);
            return await SendStreamWithPolicyAsync(request, cancellationToken);
        }

        public async Task<RemoteStreamResponse> PostStreamAsync(string url, Stream requestStream, string contentType, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (requestStream == null || !requestStream.CanRead)
                throw new ArgumentException("请求流不可读或为null", nameof(requestStream));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentNullException(nameof(contentType), "需指定请求体类型（如 application/octet-stream）");

            // 用 StreamContent 包装请求流（避免一次性读取到内存）
            using var streamContent = new StreamContent(requestStream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = streamContent
            };
            AddHeadersToRequest(request, headers);
            return await SendStreamWithPolicyAsync(request, cancellationToken);
        }

        public async Task<RemoteStreamResponse> PutStreamAsync(string url, Stream requestStream, string contentType, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (requestStream == null || !requestStream.CanRead)
                throw new ArgumentException("请求流不可读或为null", nameof(requestStream));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentNullException(nameof(contentType));

            using var streamContent = new StreamContent(requestStream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = streamContent
            };
            AddHeadersToRequest(request, headers);
            return await SendStreamWithPolicyAsync(request, cancellationToken);
        }

        public async Task<RemoteStreamResponse> DeleteStreamAsync(string url, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            AddHeadersToRequest(request, headers);
            return await SendStreamWithPolicyAsync(request, cancellationToken);
        }

        public async Task<RemoteStreamResponse> SendStreamAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.RequestUri == null)
                throw new ArgumentException("请求地址（RequestUri）不能为空", nameof(request));

            return await SendStreamWithPolicyAsync(request, cancellationToken);
        }

        /// <summary>
        /// 带弹性策略的流请求执行（复用重试+熔断）
        /// </summary>
        private async Task<RemoteStreamResponse> SendStreamWithPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            HttpResponseMessage? response = null;
            var streamResponse = new RemoteStreamResponse();

            try
            {
                // 1. 从工厂获取 HttpClient（复用生命周期管理）
                using var httpClient = _httpClientFactory.CreateClient(_clientName);
                httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSec);

                // 2. 绑定请求地址到策略上下文（用于日志）
                var policyContext = new Context { ["RequestUrl"] = request.RequestUri?.ToString() };

                // 3. 执行带策略的请求（重试+熔断，与普通请求共享策略）
                response = await _combinedPolicy.ExecuteAsync(
                  (ctx, ct) => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct),
                    context: policyContext,
                    cancellationToken: cancellationToken
                );

                // 4. 标记响应对象（用于后续释放）
                streamResponse._httpResponse = response;

                // 5. 获取响应流（仅读取头信息，不读取流内容，避免内存占用）
                var responseStream = await response.Content.ReadAsStreamAsync();
                stopwatch.Stop();

                // 6. 封装流响应结果
                streamResponse.IsSuccess = response.IsSuccessStatusCode;
                streamResponse.ResponseStream = responseStream;
                streamResponse.StatusCode = (int)response.StatusCode;
                streamResponse.ResponseHeaders = response.Headers;
                streamResponse.Duration = stopwatch.Elapsed;

                // 7. 非成功状态码处理（不抛异常，交给调用方处理）
                if (!response.IsSuccessStatusCode)
                {
                    streamResponse.Message = $"HTTP 请求失败：{response.StatusCode}";
                    _logger?.LogWarning(
                        new EventId(2001, "StreamRequestFailed"),
                        "流请求非成功状态码：{StatusCode}，地址：{Url}",
                        response.StatusCode, request.RequestUri
                    );
                }

                return streamResponse;
            }
            catch (HttpRequestException ex)
            {
                // HTTP 异常（如网络错误、4xx/5xx 未处理）
                stopwatch.Stop();
                streamResponse.IsSuccess = false;
                streamResponse.Message = ex.Message;
                streamResponse.Exception = ex;
                streamResponse.StatusCode = response != null ? (int)response.StatusCode : 0;
                streamResponse.Duration = stopwatch.Elapsed;

                _logger?.LogError(
                    new EventId(2002, "StreamHttpRequestError"),
                    ex,
                    "流请求 HTTP 异常：{Msg}，地址：{Url}",
                    ex.Message, request.RequestUri
                );
                return streamResponse;
            }
            catch (TaskCanceledException ex)
            {
                // 超时或取消异常
                stopwatch.Stop();
                var isTimeout = !cancellationToken.IsCancellationRequested;
                streamResponse.IsSuccess = false;
                streamResponse.Message = isTimeout ? "流请求超时" : "流请求被取消";
                streamResponse.Exception = ex;
                streamResponse.StatusCode = isTimeout ? 408 : 499;
                streamResponse.Duration = stopwatch.Elapsed;

                _logger?.LogError(
                    new EventId(isTimeout ? 2003 : 2004, isTimeout ? "StreamTimeout" : "StreamCanceled"),
                    ex,
                    "{Msg}：地址：{Url}，超时时间：{Sec}s",
                    streamResponse.Message, request.RequestUri, _timeoutSec
                );
                return streamResponse;
            }
            catch (Exception ex)
            {
                // 其他异常（如策略异常、流读取异常）
                stopwatch.Stop();
                streamResponse.IsSuccess = false;
                streamResponse.Message = "流请求未知异常：" + ex.Message;
                streamResponse.Exception = ex;
                streamResponse.Duration = stopwatch.Elapsed;

                _logger?.LogError(
                    new EventId(2005, "StreamUnknownError"),
                    ex,
                    "流请求未知异常：{Msg}，地址：{Url}",
                    ex.Message, request.RequestUri
                );
                return streamResponse;
            }
        }

        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 添加请求头
        /// </summary>
        private void AddHeadersToRequest(HttpRequestMessage request, IDictionary<string, string>? headers)
        {
            if (headers == null || headers.Count == 0)
                return;

            foreach (var (key, value) in headers)
            {
                if (string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase) && request.Content != null)
                {
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
                }
                else if (!request.Headers.TryAddWithoutValidation(key, value))
                {
                    _logger?.LogWarning("请求头 {Key} 添加失败（可能不支持）", key);
                }
            }
        }


        /// <summary>
        /// 带策略的请求执行
        /// </summary>
        private async Task<RemoteResponse<TResult>> SendWithPolicyAsync<TResult>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            HttpResponseMessage? response = null;
            string? rawContent = null;

            try
            {
                using var httpClient = _httpClientFactory.CreateClient(_clientName);
                httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSec);

                var policyContext = new Context { ["RequestUrl"] = request.RequestUri?.ToString() };
                response = await _combinedPolicy.ExecuteAsync(
                    (ctx, ct) => httpClient.SendAsync(request, ct), // 正确的委托格式
                    policyContext,
                    cancellationToken
                );

                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new System.IO.StreamReader(responseStream);
                rawContent = await streamReader.ReadToEndAsync();
                stopwatch.Stop();

                response.EnsureSuccessStatusCode();

                var data = JsonSerializer.Deserialize<TResult>(rawContent, _jsonOptions);
                return RemoteResponse<TResult>.Success(
                    data: data ?? default!,
                    statusCode: (int)response.StatusCode,
                    rawContent: rawContent,
                    duration: stopwatch.Elapsed
                );
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                var statusCode = response != null ? (int)response.StatusCode : 0;
                _logger?.LogError(
                    new EventId(1005, "HttpRequestError"),
                    ex,
                    "HTTP请求异常：{Msg}，地址：{Url}，状态码：{Code}",
                    ex.Message, request.RequestUri, statusCode
                );
                return RemoteResponse<TResult>.Failure(
                    message: ex.Message,
                    statusCode: statusCode,
                    rawContent: rawContent,
                    exception: ex,
                    duration: stopwatch.Elapsed
                );
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _logger?.LogError(
                    new EventId(1006, "JsonDeserializeError"),
                    ex,
                    "JSON反序列化异常：{Msg}，地址：{Url}，原始内容：{Content}",
                    ex.Message, request.RequestUri, rawContent
                );
                return RemoteResponse<TResult>.Failure(
                    message: "响应格式错误，无法解析为目标类型",
                    statusCode: response != null ? (int)response.StatusCode : 0,
                    rawContent: rawContent,
                    exception: ex,
                    duration: stopwatch.Elapsed
                );
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                var isTimeout = !cancellationToken.IsCancellationRequested;
                var msg = isTimeout ? "请求超时" : "请求被取消";
                _logger?.LogError(
                    new EventId(isTimeout ? 1007 : 1008, isTimeout ? "RequestTimeout" : "RequestCanceled"),
                    ex,
                    "{Msg}：地址：{Url}，超时时间：{Sec}s",
                    msg, request.RequestUri, _timeoutSec
                );
                return RemoteResponse<TResult>.Failure(
                    message: msg,
                    statusCode: isTimeout ? 408 : 499,
                    rawContent: rawContent,
                    exception: ex,
                    duration: stopwatch.Elapsed
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(
                    new EventId(1009, "UnknownError"),
                    ex,
                    "请求未知异常：{Msg}，地址：{Url}",
                    ex.Message, request.RequestUri
                );
                return RemoteResponse<TResult>.Failure(
                    message: "请求失败：" + ex.Message,
                    rawContent: rawContent,
                    exception: ex,
                    duration: stopwatch.Elapsed
                );
            }
        }
        #endregion
    }
}