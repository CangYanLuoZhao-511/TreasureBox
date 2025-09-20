#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 * 机器名称：*************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.ConsoleApp.EnhancedToolsTests
 * 唯一标识：1b18baea-3e9a-4da5-b777-d3b4feae312c
 * 文件名：FileTestRunner
 * 当前用户域：*************************************
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/20 7:51:35
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/20 7:51:35
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using CangYanLuoZhao.TreasureBox.EnhancedTools.FileModule;

namespace CangYanLuoZhao.TreasureBox.ConsoleApp.EnhancedToolsTests
{
    /// <summary>
    /// FileTestRunner 的摘要说明
    /// </summary>
    public class FileTestRunner
    {
        /// <summary>
        /// 显示测试主菜单，允许用户选择要运行的测试
        /// </summary>
        public async Task ShowFileTestMenuAsync()
        {
            bool returnToMainProgram = false;

            while (!returnToMainProgram)
            {
                // 显示菜单
                Console.Clear();
                Console.WriteLine("==================================");
                Console.WriteLine("      文件模块测试菜单            ");
                Console.WriteLine("==================================");
                Console.WriteLine("1. 运行基础文件操作测试");
                Console.WriteLine("2. 运行大文件分片/合并测试");
                Console.WriteLine("3. 运行断点下载测试");
                Console.WriteLine("4. 运行所有测试");
                Console.WriteLine("5. 返回主程序");
                Console.WriteLine("==================================");
                Console.Write("请选择操作 (1-5): ");

                // 处理用户输入
                var key = Console.ReadLine();
                switch (key?.Trim())
                {
                    case "1":
                        await RunTestWithReturnAsync("基础文件操作测试", FileBasicTestAsync);
                        break;
                    case "2":
                        await RunTestWithReturnAsync("大文件分片/合并测试", LargeFileTestAsync);
                        break;
                    case "3":
                        await RunTestWithReturnAsync("断点下载测试", BreakpointDownloadTestAsync);
                        break;
                    case "4":
                        await RunTestWithReturnAsync("所有测试", RunAllTestsAsync);
                        break;
                    case "5":
                        returnToMainProgram = true;
                        Console.WriteLine("返回主程序...");
                        break;
                    default:
                        Console.WriteLine("无效的选择，请输入1-5之间的数字");
                        WaitForKeyPress();
                        break;
                }
            }
        }

        /// <summary>
        /// 运行指定测试并在完成后等待用户按键返回菜单
        /// </summary>
        /// <param name="testName">测试名称</param>
        /// <param name="testAction">要执行的测试</param>
        private async Task RunTestWithReturnAsync(string testName, Func<Task> testAction)
        {
            Console.Clear();
            Console.WriteLine($"===== 开始 {testName} =====");
            Console.WriteLine();

            try
            {
                await testAction();
                Console.WriteLine();
                Console.WriteLine($"===== {testName} 完成 =====");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"===== {testName} 失败 =====");
                Console.WriteLine($"错误信息: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }

            WaitForKeyPress();
        }

        /// <summary>
        /// 等待用户按键
        /// </summary>
        private void WaitForKeyPress()
        {
            Console.WriteLine();
            Console.WriteLine("按任意键返回菜单...");
            Console.ReadKey();
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task RunAllTestsAsync()
        {
            await FileBasicTestAsync();
            await LargeFileTestAsync();
            await BreakpointDownloadTestAsync();
        }

        public async Task FileBasicTestAsync()
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

        public async Task LargeFileTestAsync()
        {
            // 大文件路径（示例：2GB视频文件）
            var largeFilePath = @"D:\工具\下载\CentOS-7-x86_64-DVD-1511.iso";
            var chunkDir = @"D:\chunks\video-chunks";
            var mergedFilePath = @"D:\chunks\merged-centos.iso";

            // 初始化文件管理器（设置8MB分片，添加进度回调）
            var largeProgress = new Progress<ProcessProgress>(info =>
            {
                var percent = info.TotalSize > 0 ? (double)info.ProcessedSize / info.TotalSize * 100 : 0;
                Console.WriteLine($"进度：{percent:F2}%，已处理：{info.ProcessedSize / 1024 / 1024}MB");
            });

            using var fileManager = new EnhancedFileManager(chunkSize: 20 * 1024 * 1024, progress: largeProgress);

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

        public async Task BreakpointDownloadTestAsync()
        {
            // 远程大文件URL（需支持Range请求）
            var remoteUrl = "https://cdn.mysql.com//Downloads/MySQLInstaller/mysql-installer-community-8.0.43.0.msi";
            var localFilePath = @"D:\downloads\large-file.msi";

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
            using var fileManager = new EnhancedFileManager(fileProgress: progress);

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

}