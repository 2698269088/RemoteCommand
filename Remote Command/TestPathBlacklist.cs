using System;
using System.IO;

namespace Remote_Command
{
    /// <summary>
    /// 测试路径黑名单功能
    /// </summary>
    public class TestPathBlacklist
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Path Blacklist functionality...");
            
            // 初始化配置管理器
            ConfigManager.Initialize();
            
            // 添加一些测试路径到黑名单
            ConfigManager.AddToPathBlacklist(@"D:\TestFolder");
            ConfigManager.AddToPathBlacklist(@"C:\Program Files\MyApp");
            
            // 测试路径检查功能
            string[] testPaths = {
                @"D:\TestFolder\app.exe",
                @"D:\TestFolder\SubFolder\app.exe",
                @"D:\TestFolder.exe",
                @"C:\Program Files\MyApp\SubDir\program.exe",
                @"C:\Windows\notepad.exe",
                @"D:\OtherFolder\app.exe"
            };
            
            Console.WriteLine("\nTesting path blacklist checks:");
            foreach (string path in testPaths)
            {
                bool isInBlacklist = ConfigManager.IsInPathBlacklist(path);
                Console.WriteLine($"{path}: {(isInBlacklist ? "Blocked" : "Allowed")}");
            }
            
            // 显示当前的路径黑名单
            Console.WriteLine("\nCurrent path blacklist:");
            var pathBlacklist = ConfigManager.GetPathBlacklist();
            foreach (string path in pathBlacklist)
            {
                Console.WriteLine($"  {path}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}