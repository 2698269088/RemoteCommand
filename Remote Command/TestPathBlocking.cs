using System;
using System.IO;

namespace Remote_Command
{
    /// <summary>
    /// 测试路径黑名单的路径阻止功能
    /// </summary>
    public class TestPathBlocking
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Path Blocking functionality...");
            
            // 添加测试路径到黑名单
            PathBlacklistManager.AddPathToBlacklist(@"D:\Test");
            
            // 测试各种路径情况
            string[] testPaths = {
                @"D:\Test\app.exe",              // 应该被阻止（子目录中的文件）
                @"D:\Test\SubDir\app.exe",       // 应该被阻止（深层嵌套目录中的文件）
                @"D:\Test.exe",                  // 应该被允许（同级文件）
                @"D:\Other\app.exe",             // 应该被允许（其他目录中的文件）
                @"D:\Test",                      // 应该被允许（目录本身）
                @"D:\Test\SubDir",               // 应该被阻止（子目录）
            };
            
            Console.WriteLine("\n测试路径阻止功能:");
            foreach (string path in testPaths)
            {
                bool isBlocked = PathBlacklistManager.IsPathBlocked(path);
                Console.WriteLine($"{path}: {(isBlocked ? "阻止" : "允许")}");
            }
            
            // 清理测试数据
            PathBlacklistManager.RemovePathFromBlacklist(@"D:\Test");
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}