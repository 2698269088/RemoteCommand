using System;
using System.IO;

namespace Remote_Command
{
    /// <summary>
    /// 测试路径黑名单的移除和重复检查功能
    /// </summary>
    public class TestPathBlacklistRemoval
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Path Blacklist Removal and Duplicate Check functionality...");
            
            // 测试添加路径
            Console.WriteLine("\n=== 测试添加路径 ===");
            bool added1 = PathBlacklistManager.AddPathToBlacklist(@"D:\TestFolder");
            Console.WriteLine($"添加 D:\\TestFolder: {(added1 ? "成功" : "失败(已存在)")}");
            
            bool added2 = PathBlacklistManager.AddPathToBlacklist(@"C:\Program Files\MyApp");
            Console.WriteLine($"添加 C:\\Program Files\\MyApp: {(added2 ? "成功" : "失败(已存在)")}");
            
            bool added3 = PathBlacklistManager.AddPathToBlacklist(@"D:\TestFolder"); // 重复添加
            Console.WriteLine($"再次添加 D:\\TestFolder: {(added3 ? "成功" : "失败(已存在)")}");
            
            // 显示当前黑名单
            Console.WriteLine("\n=== 当前路径黑名单 ===");
            var blacklist = PathBlacklistManager.GetPathBlacklist();
            foreach (string path in blacklist)
            {
                Console.WriteLine($"  {path}");
            }
            
            // 测试路径检查功能
            Console.WriteLine("\n=== 测试路径检查 ===");
            string[] testPaths = {
                @"D:\TestFolder\app.exe",
                @"D:\TestFolder\SubFolder\app.exe",
                @"D:\TestFolder.exe",
                @"C:\Program Files\MyApp\SubDir\program.exe",
                @"C:\Windows\notepad.exe",
                @"D:\OtherFolder\app.exe"
            };
            
            foreach (string path in testPaths)
            {
                bool isBlocked = PathBlacklistManager.IsPathBlocked(path);
                Console.WriteLine($"{path}: {(isBlocked ? "阻止" : "允许")}");
            }
            
            // 测试移除路径
            Console.WriteLine("\n=== 测试移除路径 ===");
            bool removed1 = PathBlacklistManager.RemovePathFromBlacklist(@"D:\TestFolder");
            Console.WriteLine($"移除 D:\\TestFolder: {(removed1 ? "成功" : "失败(不存在)")}");
            
            bool removed2 = PathBlacklistManager.RemovePathFromBlacklist(@"D:\NonExistentFolder"); // 移除不存在的路径
            Console.WriteLine($"移除 D:\\NonExistentFolder: {(removed2 ? "成功" : "失败(不存在)")}");
            
            // 再次显示当前黑名单
            Console.WriteLine("\n=== 移除后的路径黑名单 ===");
            var updatedBlacklist = PathBlacklistManager.GetPathBlacklist();
            foreach (string path in updatedBlacklist)
            {
                Console.WriteLine($"  {path}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}