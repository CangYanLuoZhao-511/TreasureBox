## 一、使用示例

### 1. 基础文件操作

csharp











```csharp
using CangYanLuoZhao.TreasureBox.BasicTools.Helpers;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // 初始化文件管理器
        using var fileManager = new EnhancedFileManager();

        // 1. 创建文件
        var testFile = Path.Combine(Environment.CurrentDirectory, "test.txt");
        await fileManager.CreateFileAsync(testFile, "Hello, EnhancedFileManager!", default);
        Console.WriteLine("文件创建完成");

        // 2. 读取文件
        var content = await fileManager.ReadFileAsync(testFile);
        Console.WriteLine($"文件内容：{content}");

        // 3. 复制文件
        var copyFile = Path.Combine(Environment.CurrentDirectory, "test_copy.txt");
        await fileManager.CopyFileAsync(testFile, copyFile, overwrite: true);
        Console.WriteLine("文件复制完成");

        // 4. 移动文件
        var moveDir = Path.Combine(Environment.CurrentDirectory, "moved-files");
        var moveFile = Path.Combine(moveDir, "test_moved.txt");
        fileManager.MoveFile(copyFile, moveFile);
        Console.WriteLine("文件移动完成");

        // 5. 删除文件
        fileManager.DeleteFile(testFile);
        fileManager.DeleteDirectory(moveDir);
        Console.WriteLine("文件清理完成");
    }
}
```

### 2. 大文件分片与合并

csharp











```csharp
using CangYanLuoZhao.TreasureBox.BasicTools.Helpers;
using System;
using System.Threading.Tasks;

class LargeFileDemo
{
    static async Task Main()
    {
        // 大文件路径（示例：2GB视频文件）
        var largeFilePath = @"D:\large-video.mp4";
        var chunkDir = @"D:\video-chunks";
        var mergedFilePath = @"D:\merged-video.mp4";

        // 初始化文件管理器（设置8MB分片，添加进度回调）
        var progress = new Progress<FileProgressInfo>(info =>
        {
            var percent = info.TotalSize > 0 ? (double)info.ProcessedSize / info.TotalSize * 100 : 0;
            Console.WriteLine($"进度：{percent:F2}%，已处理：{info.ProcessedSize / 1024 / 1024}MB");
        });
        using var fileManager = new EnhancedFileManager(chunkSize: 8 * 1024 * 1024, progress: progress);

        try
        {
            // 1. 拆分大文件
            Console.WriteLine("开始拆分大文件...");
            var (totalChunks, chunkPaths) = await fileManager.SplitLargeFileAsync(largeFilePath, chunkDir);
            Console.WriteLine($"拆分完成：共{totalChunks}个分片，保存目录：{chunkDir}");

            // 2. 合并分片
            Console.WriteLine("开始合并分片...");
            await fileManager.MergeLargeFileAsync(chunkDir, mergedFilePath, totalChunks);
            Console.WriteLine($"合并完成：{mergedFilePath}");

            // 3. 校验合并后的文件（对比MD5）
            Console.WriteLine("校验文件完整性...");
            var originalMd5 = await FileHashHelper.CalculateMD5Async(largeFilePath);
            var mergedMd5 = await FileHashHelper.CalculateMD5Async(mergedFilePath);
            Console.WriteLine(originalMd5 == mergedMd5 ? "文件校验通过" : "文件校验失败");
        }
        finally
        {
            // 清理分片目录
            fileManager.DeleteDirectory(chunkDir);
            Console.WriteLine("分片目录已清理");
        }
    }
}
```

### 3. 断点续传下载

csharp











```csharp
using CangYanLuoZhao.TreasureBox.BasicTools.Helpers;
using System;
using System.Net.Http;
using System.Threading.Tasks;

class BreakpointDownloadDemo
{
    static async Task Main()
    {
        // 远程大文件URL（需支持Range请求）
        var remoteUrl = "https://example.com/large-file.zip";
        var localFilePath = @"D:\downloads\large-file.zip";

        // 初始化HTTP客户端（设置超时30分钟）
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };

        // 初始化文件管理器（添加进度回调）
        var progress = new Progress<FileProgressInfo>(info =>
        {
            if (info.TotalSize <= 0) return;
            var percent = (double)info.ProcessedSize / info.TotalSize * 100;
            var speed = info.ProcessedSize / 1024 / 1024 / info.Duration.TotalSeconds;
            Console.WriteLine($"下载进度：{percent:F2}%，速度：{speed:F2}MB/s，剩余：{info.TotalSize - info.ProcessedSize / 1024 / 1024}MB");
        });
        using var fileManager = new EnhancedFileManager(progress: progress);

        try
        {
            Console.WriteLine("开始断点下载...（中断后可重新运行恢复）");
            var fileInfo = await fileManager.BreakpointDownloadAsync(remoteUrl, localFilePath, httpClient);
            Console.WriteLine($"下载完成：{fileInfo.FullName}，大小：{fileInfo.Length / 1024 / 1024}MB");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载中断：{ex.Message}，下次运行可恢复");
        }
    }
}
```

## 二、核心特性说明

### 1. 高性能设计

- **异步 IO 优先**：所有文件操作基于 `async/await`，避免阻塞线程，支持高并发场景；
- **流分片处理**：大文件读写采用固定缓冲区（80KB），不加载整个文件到内存，支持 GB 级文件；
- **高效拷贝**：使用 `Stream.CopyToAsync` 底层优化，比手动缓冲区拷贝效率提升 30%+；
- **哈希增量计算**：文件哈希基于流分片计算，内存占用稳定（仅缓冲区大小）。

### 2. 低内存占用

- **无内存溢出风险**：所有大文件操作（分片、哈希、传输）均基于流，内存占用≤缓冲区大小 + 分片大小；
- **临时文件清理**：断点续传临时分片自动清理，进度文件定期过期清理，避免磁盘占用膨胀；
- **轻量级序列化**：进度信息使用 JSON 序列化，文件体积小（KB 级），读写效率高。

### 3. 功能完整性

| 功能模块     | 核心能力                                                     |
| ------------ | ------------------------------------------------------------ |
| 基础文件操作 | 异步创建 / 读取 / 复制 / 移动 / 删除，目录管理，文件过滤     |
| 大文件处理   | 自定义分片大小（1-32MB），分片拆分 / 合并，分片校验          |
| 断点续传     | 下载（HTTP Range）、上传（分片上传），进度持久化，中断恢复，过期清理 |
| 文件校验     | MD5/SHA256 哈希计算，文件完整性校验，分片哈希合并            |
| 进度监控     | 实时进度回调（百分比、已处理大小、速度），支持 UI 进度条绑定 |

### 4. 健壮性保障

- **异常安全**：所有 IO 操作包含异常捕获，断点续传支持网络中断、程序崩溃后恢复；
- **参数校验**：文件 / 目录路径合法性校验，分片数量 / 大小合理性校验，避免非法输入；
- **资源释放**：所有流实现 `IDisposable`，使用 `using` 确保资源释放，避免句柄泄漏；
- **兼容性**：基于.NET Standard 2.1，兼容.NET Core 3.1+、.NET 5+、.NET 6+、.NET 9+。

## 三、依赖与部署

- **无第三方依赖**：仅依赖.NET Standard 2.1 内置库（`System.IO`、`System.Net.Http`、`System.Security.Cryptography`、`System.Text.Json`）；
- **部署方式**：直接引用源码或打包为类库，集成到[ASP.NET](https://asp.net/) Core、控制台、WPF 等项目；
- **性能建议**：大文件操作建议使用 SSD 存储，网络传输建议配置 HTTP 连接池（`HttpClient` 复用）。