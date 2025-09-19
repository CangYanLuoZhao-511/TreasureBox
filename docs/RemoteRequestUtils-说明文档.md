# RemoteRequestClientExtensions 用法文档

## 一、概述

`RemoteRequestClientExtensions` 是基于 **.NET Standard 2.1** 的远程请求客户端依赖注入扩展类，封装了 `IHttpClientFactory` 与 Polly 弹性策略（重试、熔断、超时），支持**单客户端**与**多命名客户端**场景，可快速集成到 [ASP.NET](https://asp.net/) Core 或其他 .NET 项目中，简化远程 API 调用的配置与使用。

## 二、依赖环境

### 1. 框架版本

- 目标框架：.NET Standard 2.1（兼容 .NET Core 3.1+、.NET 5+、.NET 6+ 等）
- CLR 版本：4.0.30319.42000 及以上

### 2. 必需 NuGet 包

#### 各包作用说明

| NuGet 包名                        | 版本  | 核心作用                                                     |
| --------------------------------- | ----- | ------------------------------------------------------------ |
| `Microsoft.Extensions.Http`       | 9.0.9 | 提供 `IHttpClientFactory`，管理 `HttpClient` 生命周期，避免 socket 耗尽问题 |
| `Microsoft.Extensions.Http.Polly` | 9.0.9 | 官方集成包，将 Polly 策略与 `HttpClient` 绑定，简化策略配置  |
| `Polly`                           | 8.6.3 | 弹性策略核心库，支持重试、熔断、超时、舱壁等策略             |
| `Polly.Core`                      | 8.6.3 | Polly 8.x 依赖包，提供泛型策略（如 `AsyncCircuitBreakerPolicy<TResult>`）的底层支持 |
| `System.Text.Json`                | 9.0.9 | 处理 JSON 序列化 / 反序列化，适配远程 API 的请求 / 响应格式  |

### 3. 命名空间引用

使用前需在代码文件头部添加以下命名空间：

csharp





```csharp
using CangYanLuoZhao.TreasureBox.RemoteRequestUtils;
using Microsoft.Extensions.DependencyInjection; // 依赖注入相关
using System.Net.Http; // HttpClient 相关
using System.Text.Json; // JSON 配置相关（可选）
```

## 三、快速开始（单客户端场景）

适用于项目中仅需调用**一个远程 API 服务**的场景（如仅对接 “用户中心 API”）。

### 1. 步骤 1：注册服务（Program.cs/ Startup.cs）

在项目的依赖注入配置中，通过 `AddRemoteRequestClient` 注册默认远程请求客户端：

csharp





```csharp
// Program.cs（.NET 6+ 顶级语句）
var builder = WebApplication.CreateBuilder(args);

// 注册远程请求客户端（单客户端场景）
builder.Services.AddRemoteRequestClient(
    clientName: "UserCenterApi", // 客户端名称（自定义，用于标识）
    configureClient: client => 
    {
        // 配置 HttpClient 基础信息
        client.BaseAddress = new Uri("https://api.user-center.com/"); // API 基础地址
        client.DefaultRequestHeaders.Add("Accept", "application/json"); // 默认请求头
        client.DefaultRequestHeaders.Add("X-AppId", "your-app-id"); // 自定义业务头
    },
    retryCount: 2, // 重试次数（默认 3 次，失败时自动重试）
    retryDelayMilliseconds: 800, // 重试延迟（默认 1000ms，每次重试间隔递增）
    circuitBreakDurationSeconds: 60, // 熔断持续时间（默认 30s，连续失败后暂时拒绝请求）
    timeoutSeconds: 15, // 请求超时时间（默认 30s，避免无限等待）
    jsonOptions: new JsonSerializerOptions // 自定义 JSON 序列化配置（可选）
    {
        PropertyNameCaseInsensitive = true, // 忽略属性名大小写（适配 API 返回的驼峰/ Pascal 命名）
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull // 序列化时忽略 null 值
    }
);

// 其他服务注册（如控制器、业务服务等）
builder.Services.AddControllers();

var app = builder.Build();
// ... 后续中间件配置
```

### 2. 步骤 2：注入并使用（业务类 / 控制器）

在需要调用远程 API 的业务类或控制器中，通过构造函数注入 `IRemoteRequestClient`，直接调用 API 方法（支持 GET/POST/PUT/DELETE）。

#### 示例 1：业务服务类中使用

csharp





```csharp
using CangYanLuoZhao.TreasureBox.RemoteRequestUtils;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace YourProject.BusinessServices
{
    public class UserService
    {
        // 注入远程请求客户端
        private readonly IRemoteRequestClient _remoteClient;
        private readonly ILogger<UserService> _logger;

        // 构造函数注入（依赖注入容器自动解析）
        public UserService(IRemoteRequestClient remoteClient, ILogger<UserService> logger)
        {
            _remoteClient = remoteClient;
            _logger = logger;
        }

        /// <summary>
        /// 调用远程 API 获取用户信息
        /// </summary>
        public async Task<RemoteResponse<UserDto>> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. 配置请求头（如 Token 鉴权）
                var headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." // 用户登录后的 Token
                };

                // 2. 调用 GET 接口（自动触发重试/熔断策略）
                // 接口地址：https://api.user-center.com/user/123（BaseAddress + 相对路径）
                var response = await _remoteClient.GetAsync<UserDto>(
                    url: $"user/{userId}", // 相对路径（基于 BaseAddress）
                    headers: headers, // 自定义请求头（可选）
                    cancellationToken: cancellationToken // 取消令牌（可选，用于中断请求）
                );

                // 3. 处理响应结果
                if (response.IsSuccess)
                {
                    _logger.LogInformation("获取用户 {UserId} 成功，用户名：{UserName}", userId, response.Data?.UserName);
                    return response; // 返回成功响应（包含 UserDto 数据）
                }
                else
                {
                    _logger.LogError("获取用户 {UserId} 失败：{Msg}，状态码：{Code}，原始内容：{Raw}",
                        userId, response.Message, response.StatusCode, response.RawContent);
                    return response; // 返回失败响应（包含错误信息）
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户 {UserId} 异常", userId);
                throw; // 或返回自定义异常响应
            }
        }

        /// <summary>
        /// 调用远程 API 创建用户（POST 请求）
        /// </summary>
        public async Task<RemoteResponse<CreateUserResultDto>> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
        {
            // 1. 配置请求头
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                ["X-Request-Id"] = Guid.NewGuid().ToString() // 追踪请求的唯一 ID
            };

            // 2. 调用 POST 接口（自动序列化 request 为 JSON）
            var response = await _remoteClient.PostAsync<CreateUserResultDto, CreateUserRequestDto>(
                url: "user", // 相对路径
                data: request, // POST 提交的数据（会序列化为 application/json）
                headers: headers,
                cancellationToken: cancellationToken
            );

            return response;
        }
    }

    // 数据模型（与远程 API 响应/请求格式对齐）
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
    }

    public class CreateUserRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateUserResultDto
    {
        public bool Success { get; set; }
        public int NewUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
```

#### 示例 2：控制器中使用

csharp





```csharp
using CangYanLuoZhao.TreasureBox.RemoteRequestUtils;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace YourProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IRemoteRequestClient _remoteClient;

        public UserController(IRemoteRequestClient remoteClient)
        {
            _remoteClient = remoteClient;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId, CancellationToken cancellationToken)
        {
            var response = await _remoteClient.GetAsync<UserDto>($"user/{userId}", cancellationToken: cancellationToken);
            if (response.IsSuccess)
            {
                return Ok(response.Data);
            }
            return StatusCode(response.StatusCode, response.Message);
        }
    }
}
```

## 四、多命名客户端场景

适用于项目中需要调用**多个不同远程 API 服务**的场景（如同时对接 “用户中心 API” 和 “订单中心 API”），通过 “客户端名称” 区分不同配置。

### 1. 步骤 1：注册多命名客户端（Program.cs）

使用 `AddNamedRemoteRequestClient` 为每个 API 服务单独注册客户端，配置各自的基础地址、策略参数：

csharp





```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. 注册「用户中心 API」客户端
builder.Services.AddNamedRemoteRequestClient(
    clientName: "UserCenterApi", // 客户端名称（唯一标识，后续通过此名称获取）
    configureOptions: options =>
    {
        options.ConfigureClient = client => 
        {
            client.BaseAddress = new Uri("https://api.user-center.com/");
            client.DefaultRequestHeaders.Add("X-AppId", "user-app-123");
        };
        options.RetryCount = 2; // 重试 2 次
        options.TimeoutSeconds = 15; // 超时 15s
        options.CircuitBreakDurationSeconds = 60; // 熔断 60s
    }
);

// 2. 注册「订单中心 API」客户端
builder.Services.AddNamedRemoteRequestClient(
    clientName: "OrderCenterApi", // 另一个客户端名称
    configureOptions: options =>
    {
        options.ConfigureClient = client => 
        {
            client.BaseAddress = new Uri("https://api.order-center.com/");
            client.DefaultRequestHeaders.Add("X-AppId", "order-app-456");
        };
        options.RetryCount = 3; // 重试 3 次（与用户中心不同）
        options.TimeoutSeconds = 20; // 超时 20s（与用户中心不同）
        options.JsonOptions = new JsonSerializerOptions // 自定义 JSON 配置
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true // 序列化时格式化输出（便于调试）
        };
    }
);

builder.Services.AddControllers();
var app = builder.Build();
```

### 2. 步骤 2：获取并使用命名客户端

有两种方式获取命名客户端，根据项目 .NET 版本选择：

#### 方式 1：通过 `INamedRemoteRequestClient` 注入（通用，推荐）

直接注入 `INamedRemoteRequestClient`，通过客户端名称区分（需确保注册时的名称与使用时一致）：

csharp





```csharp
public class OrderService
{
    private readonly IRemoteRequestClient _userClient; // 用户中心客户端
    private readonly IRemoteRequestClient _orderClient; // 订单中心客户端

    // 构造函数注入（通过工厂模式获取指定名称的客户端）
    public OrderService(Func<string, IRemoteRequestClient> clientFactory)
    {
        // 通过客户端名称获取对应的实例
        _userClient = clientFactory("UserCenterApi");
        _orderClient = clientFactory("OrderCenterApi");
    }

    /// <summary>
    /// 调用「订单中心 API」创建订单
    /// </summary>
    public async Task<RemoteResponse<OrderDto>> CreateOrderAsync(CreateOrderRequestDto request)
    {
        return await _orderClient.PostAsync<OrderDto, CreateOrderRequestDto>(
            url: "order", 
            data: request
        );
    }

    /// <summary>
    /// 调用「用户中心 API」验证用户
    /// </summary>
    public async Task<RemoteResponse<bool>> ValidateUserAsync(int userId)
    {
        return await _userClient.GetAsync<bool>($"user/validate/{userId}");
    }
}
```

#### 方式 2：通过 `AddKeyedTransient` 注入（.NET 6+ 支持）

若项目为 .NET 6+，可通过 `[FromKeyedServices]` 特性直接注入指定名称的客户端：

csharp





```csharp
public class OrderController : ControllerBase
{
    // 通过 [FromKeyedServices] 注入指定名称的客户端
    public OrderController(
        [FromKeyedServices("UserCenterApi")] IRemoteRequestClient userClient,
        [FromKeyedServices("OrderCenterApi")] IRemoteRequestClient orderClient)
    {
        _userClient = userClient;
        _orderClient = orderClient;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequestDto request)
    {
        // 先调用用户中心验证用户
        var userValid = await _userClient.GetAsync<bool>($"user/validate/{request.UserId}");
        if (!userValid.IsSuccess || !userValid.Data)
        {
            return BadRequest("用户无效");
        }

        // 再调用订单中心创建订单
        var orderResponse = await _orderClient.PostAsync<OrderDto, CreateOrderRequestDto>("order", request);
        return Ok(orderResponse.Data);
    }
}
```

## 五、配置选项说明（RemoteRequestClientOptions）

`RemoteRequestClientOptions` 是多客户端场景的核心配置类，用于自定义每个客户端的行为，各属性说明如下：

| 属性名                        | 类型                  | 默认值 | 说明                                                         |
| ----------------------------- | --------------------- | ------ | ------------------------------------------------------------ |
| `RetryCount`                  | int                   | 3      | 重试次数：请求失败（HTTP 非 2xx 状态码、网络错误）时自动重试的次数，0 表示不重试。 |
| `RetryDelayMilliseconds`      | int                   | 1000   | 重试延迟：每次重试的基础间隔（毫秒），实际延迟为 “重试次数 × 基础延迟”（如第 2 次重试延迟 2000ms）。 |
| `CircuitBreakDurationSeconds` | int                   | 30     | 熔断持续时间：连续失败 `RetryCount` 次后，客户端进入 “熔断状态”，此期间拒绝请求的时长（秒）。 |
| `TimeoutSeconds`              | int                   | 30     | 请求超时时间：HttpClient 发送请求的最大等待时间（秒），超时后抛出 `TaskCanceledException`。 |
| `ConfigureClient`             | Action<HttpClient>    | null   | HttpClient 配置委托：用于设置基础地址、默认请求头、代理等 HttpClient 原生配置。 |
| `JsonOptions`                 | JsonSerializerOptions | null   | JSON 序列化配置：自定义请求 / 响应的 JSON 处理（如大小写、null 值处理），默认使用 “忽略大小写” 配置。 |

## 六、注意事项

1. **HttpClient 生命周期**：

   客户端由 `IHttpClientFactory` 管理，无需手动调用 `Dispose()`，注入后直接使用即可（避免手动创建 HttpClient 导致的 socket 耗尽问题）。

2. **Polly 策略生效范围**：

   重试策略仅对 “HTTP 5xx 状态码、HTTP 4xx 状态码（部分 API 4xx 为业务错误，需自行判断是否重试）、网络错误（如超时、连接失败）” 生效；熔断策略在连续失败 `RetryCount` 次后触发，期间请求会快速失败（抛出 `BrokenCircuitException`）。

3. **JSON 序列化兼容**：

   若远程 API 返回的 JSON 采用 “驼峰命名”（如 `userName`），本地模型用 “Pascal 命名”（如 `UserName`），需确保 `JsonOptions.PropertyNameCaseInsensitive = true`（默认已开启），避免反序列化失败。

4. **.NET 版本兼容**：

   - `AddKeyedTransient` 是 .NET 6+ 特性，.NET Core 3.1/.NET 5 项目需使用 “工厂模式（`Func<string, IRemoteRequestClient>`）” 获取命名客户端。

5. **日志输出**：

   注入 `ILogger<RemoteRequestClient>` 后，可输出请求重试、熔断状态切换、异常等日志，便于问题排查（日志类别：`CangYanLuoZhao.TreasureBox.RemoteRequestUtils.RemoteRequestClient`）。

## 七、常见问题

### Q1：调用 API 时提示 “熔断触发”，如何处理？

A：说明客户端连续失败次数达到阈值，进入熔断状态。需先排查远程 API 是否可用（如网络、服务状态），待熔断时长结束后会自动恢复；若需紧急恢复，可重启应用。

### Q2：如何自定义重试策略的触发条件（如仅重试 HTTP 5xx）？

A：当前扩展类默认重试 “HTTP 非 2xx + 网络错误”，若需自定义，可修改 `RemoteRequestClient` 内部的 `_retryPolicy` 配置（如仅保留 `HandleResult<HttpResponseMessage>(r => r.StatusCode >= HttpStatusCode.InternalServerError)`）。

### Q3：多客户端场景中，如何共享部分配置（如统一的 JSON 选项）？

A：可创建全局 `JsonSerializerOptions` 实例，注册多客户端时复用：

csharp





```csharp
var globalJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

// 复用全局 JSON 配置
builder.Services.AddNamedRemoteRequestClient("UserCenterApi", options =>
{
    options.JsonOptions = globalJsonOptions;
    // ... 其他配置
});
builder.Services.AddNamedRemoteRequestClient("OrderCenterApi", options =>
{
    options.JsonOptions = globalJsonOptions;
    // ... 其他配置
});
```