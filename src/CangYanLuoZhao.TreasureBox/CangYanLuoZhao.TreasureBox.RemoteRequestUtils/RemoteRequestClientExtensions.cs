#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.RemoteRequestUtils
 * 唯一标识：85671b55-5a17-4612-9442-93f147f2e40a
 * 文件名：RemoteRequestClientExtensions
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/19 21:14:46
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/19 21:14:46
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;

namespace CangYanLuoZhao.TreasureBox.RemoteRequestUtils
{
    /// <summary>
    /// 远程请求客户端的依赖注入扩展
    /// </summary>
    public static class RemoteRequestClientExtensions
    {
        /// <summary>
        /// 添加默认远程请求客户端（单客户端场景）
        /// </summary>
        public static IServiceCollection AddRemoteRequestClient(this IServiceCollection services,
            string clientName = "default",
            Action<HttpClient>? configureClient = null,
            int retryCount = 3,
            int retryDelayMilliseconds = 1000,
            int circuitBreakDurationSeconds = 30,
            int timeoutSeconds = 30,
            JsonSerializerOptions? jsonOptions = null)
        {
            // 1. 注册 HttpClient（由 IHttpClientFactory 管理）
            services.AddHttpClient(clientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                configureClient?.Invoke(client); // 外部配置（如BaseAddress、默认头）
            });

            // 2. 注册 IRemoteRequestClient（瞬时生命周期，与 HttpClient 生命周期匹配）
            services.TryAddTransient<IRemoteRequestClient>(sp =>
                new RemoteRequestClient(
                    httpClientFactory: sp.GetRequiredService<IHttpClientFactory>(),
                    logger: sp.GetService<ILogger<RemoteRequestClient>>(),
                    clientName: clientName,
                    retryCount: retryCount,
                    retryDelayMs: retryDelayMilliseconds,
                    circuitBreakDurationSec: circuitBreakDurationSeconds,
                    timeoutSec: timeoutSeconds,
                    jsonOptions: jsonOptions
                )
            );

            // 3. 同时注册 INamedRemoteRequestClient（兼容命名客户端场景）
            services.TryAddTransient<INamedRemoteRequestClient>(sp =>
                (RemoteRequestClient)sp.GetRequiredService<IRemoteRequestClient>()
            );

            return services;
        }

        /// <summary>
        /// 添加命名远程请求客户端（多客户端场景，如同时调用多个API服务）
        /// </summary>
        public static IServiceCollection AddNamedRemoteRequestClient(this IServiceCollection services,
            string clientName,
            Action<RemoteRequestClientOptions> configureOptions)
        {
            // 验证参数
            if (string.IsNullOrWhiteSpace(clientName))
                throw new ArgumentException("客户端名称不能为空", nameof(clientName));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions), "配置委托不能为空");

            // 1. 注册客户端配置（支持Options模式）
            services.Configure<RemoteRequestClientOptions>(clientName, configureOptions);

            // 2. 注册命名 HttpClient
            services.AddHttpClient(clientName, (sp, httpClient) =>
            {
                // 从Options获取配置
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<RemoteRequestClientOptions>>()
                    .Get(clientName);
                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                options.ConfigureClient?.Invoke(httpClient); // 外部配置 HttpClient
            });

            // 3. 注册命名 IRemoteRequestClient（通过 KeyedService 区分，.NET 6+ 支持）
            services.AddKeyedTransient<IRemoteRequestClient>(clientName, (sp, key) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<RemoteRequestClientOptions>>()
                    .Get(clientName);
                return new RemoteRequestClient(
                    httpClientFactory: sp.GetRequiredService<IHttpClientFactory>(),
                    logger: sp.GetService<ILogger<RemoteRequestClient>>(),
                    clientName: clientName,
                    retryCount: options.RetryCount,
                    retryDelayMs: options.RetryDelayMilliseconds,
                    circuitBreakDurationSec: options.CircuitBreakDurationSeconds,
                    timeoutSec: options.TimeoutSeconds,
                    jsonOptions: options.JsonOptions
                );
            });

            // 4. 注册 INamedRemoteRequestClient（用于强类型命名客户端）
            services.AddTransient<INamedRemoteRequestClient>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsSnapshot<RemoteRequestClientOptions>>()
                    .Get(clientName);
                return new RemoteRequestClient(
                    httpClientFactory: sp.GetRequiredService<IHttpClientFactory>(),
                    logger: sp.GetService<ILogger<RemoteRequestClient>>(),
                    clientName: clientName,
                    retryCount: options.RetryCount,
                    retryDelayMs: options.RetryDelayMilliseconds,
                    circuitBreakDurationSec: options.CircuitBreakDurationSeconds,
                    timeoutSec: options.TimeoutSeconds,
                    jsonOptions: options.JsonOptions
                );
            });

            return services;
        }

        /// <summary>
        /// 远程请求客户端配置选项（用于多客户端场景）
        /// </summary>
        public class RemoteRequestClientOptions
        {
            /// <summary>
            /// 重试次数（默认3次）
            /// </summary>
            public int RetryCount { get; set; } = 3;

            /// <summary>
            /// 重试延迟（毫秒，默认1000ms）
            /// </summary>
            public int RetryDelayMilliseconds { get; set; } = 1000;

            /// <summary>
            /// 熔断持续时间（秒，默认30秒）
            /// </summary>
            public int CircuitBreakDurationSeconds { get; set; } = 30;

            /// <summary>
            /// 请求超时时间（秒，默认30秒）
            /// </summary>
            public int TimeoutSeconds { get; set; } = 30;

            /// <summary>
            /// 配置 HttpClient（如BaseAddress、默认头）
            /// </summary>
            public Action<HttpClient>? ConfigureClient { get; set; }

            /// <summary>
            /// JSON序列化配置（默认支持大小写不敏感）
            /// </summary>
            public JsonSerializerOptions? JsonOptions { get; set; }
        }
    }
}