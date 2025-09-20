using CangYanLuoZhao.TreasureBox.BasicTools.Helpers;
using CangYanLuoZhao.TreasureBox.ConsoleApp.EnhancedToolsTests;

namespace CangYanLuoZhao.TreasureBox.ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 初始化主菜单系统
            var mainMenu = new MainMenu();

            // 注册测试模块（未来添加新测试只需在此处注册）
            mainMenu.RegisterModule("文件增强工具测试", () =>
            {
                var fileTestRunner = new FileTestRunner();
                return fileTestRunner.ShowFileTestMenuAsync();
            });

            // 启动主菜单
            await mainMenu.ShowAsync();
        }
    }

    /// <summary>
    /// 主菜单系统，负责管理所有测试模块
    /// </summary>
    public class MainMenu
    {
        private readonly Dictionary<int, (string Name, Func<Task> Action)> _modules = new Dictionary<int, (string, Func<Task>)>();
        private int _nextModuleId = 1;

        /// <summary>
        /// 注册测试模块
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="action">模块启动方法</param>
        public void RegisterModule(string moduleName, Func<Task> action)
        {
            _modules.Add(_nextModuleId++, (moduleName, action));
        }

        /// <summary>
        /// 显示主菜单并处理用户选择
        /// </summary>
        public async Task ShowAsync()
        {
            bool isRunning = true;

            while (isRunning)
            {
                Console.Clear();
                DisplayHeader();
                DisplayMenuOptions();

                var userInput = Console.ReadLine()?.Trim();
                isRunning = await ProcessUserInput(userInput);

                if (isRunning && !string.IsNullOrEmpty(userInput))
                {
                    Console.WriteLine("\n按任意键返回主菜单...");
                    Console.ReadKey();
                }
            }

            Console.WriteLine("\n程序已退出。");
        }

        /// <summary>
        /// 显示程序头部信息
        /// </summary>
        private void DisplayHeader()
        {
            Console.WriteLine("==================================");
            Console.WriteLine("  【苍煙落照】的百宝箱 - 测试中心        ");
            Console.WriteLine("==================================");
            Console.WriteLine();
        }

        /// <summary>
        /// 显示菜单选项
        /// </summary>
        private void DisplayMenuOptions()
        {
            foreach (var module in _modules)
            {
                Console.WriteLine($"{module.Key}. {module.Value.Name}");
            }

            Console.WriteLine($"{_nextModuleId}. 退出程序");
            Console.WriteLine();
            Console.Write("请选择操作: ");
        }

        /// <summary>
        /// 处理用户输入
        /// </summary>
        /// <param name="input">用户输入</param>
        /// <returns>是否继续运行程序</returns>
        private async Task<bool> ProcessUserInput(string? input)
        {
            if (Check.IsNull(input))
            {
                Console.WriteLine("无效输入，请输入数字选择操作。");
                return true;
            }

            if (!int.TryParse(input, out int selection))
            {
                Console.WriteLine("无效输入，请输入数字选择操作。");
                return true;
            }

            // 检查是否选择退出
            if (selection == _nextModuleId)
            {
                return false;
            }

            // 检查是否选择了已注册的模块
            if (_modules.TryGetValue(selection, out var module))
            {
                Console.WriteLine($"\n===== 进入 {module.Name} =====");
                await module.Action();
                return true;
            }

            Console.WriteLine("无效的选择，请重试。");
            return true;
        }
    }
}

