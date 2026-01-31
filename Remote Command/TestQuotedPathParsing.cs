using System;
using System.Collections.Generic;

namespace Remote_Command
{
    /// <summary>
    /// 测试带引号路径解析功能
    /// </summary>
    public class TestQuotedPathParsing
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing Quoted Path Parsing functionality...");
            
            // 测试用例
            string[] testCases = {
                // 不带引号的标准路径
                @"C:\Program Files\MyApp,D:\Test Folder",
                
                // 使用引号包裹的路径
                @"""C:\Program Files\MyApp"",""D:\Test Folder""",
                
                // 混合使用引号和无引号的路径
                @"""C:\Program Files\MyApp"",D:\TestFolder,""E:\Another Folder\Subfolder""",
                
                // 包含逗号的路径（使用引号）
                @"""C:\Path,With,Commas"",""D:\NormalPath""",
                
                // 混合情况：普通路径和带逗号的路径
                @"D:\SimplePath,""C:\Path,With,Commas"",E:\AnotherPath"
            };
            
            UdpListenerMock mockListener = new UdpListenerMock();
            
            for (int i = 0; i < testCases.Length; i++)
            {
                Console.WriteLine($"\n=== 测试用例 {i+1} ===");
                Console.WriteLine($"输入: {testCases[i]}");
                
                string[] parsedPaths = mockListener.ParseQuotedPathList(testCases[i]);
                
                Console.WriteLine("解析结果:");
                for (int j = 0; j < parsedPaths.Length; j++)
                {
                    Console.WriteLine($"  [{j}]: {parsedPaths[j]}");
                }
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
    
    /// <summary>
    /// UdpListener的模拟类，用于测试内部方法
    /// </summary>
    public class UdpListenerMock
    {
        /// <summary>
        /// 解析带引号的路径列表
        /// </summary>
        /// <param name="pathList">原始路径列表字符串</param>
        /// <returns>解析后的路径数组</returns>
        public string[] ParseQuotedPathList(string pathList)
        {
            if (string.IsNullOrEmpty(pathList))
                return new string[0];
            
            var paths = new List<string>();
            bool inQuotes = false;
            int lastSplit = 0;
            
            for (int i = 0; i < pathList.Length; i++)
            {
                char c = pathList[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    string path = pathList.Substring(lastSplit, i - lastSplit).Trim();
                    if (!string.IsNullOrEmpty(path))
                    {
                        // 移除路径两端的引号（如果有）
                        if (path.StartsWith("\"") && path.EndsWith("\"") && path.Length >= 2)
                        {
                            path = path.Substring(1, path.Length - 2);
                        }
                        paths.Add(path);
                    }
                    lastSplit = i + 1;
                }
            }
            
            // 处理最后一个路径
            if (lastSplit <= pathList.Length)
            {
                string path = pathList.Substring(lastSplit).Trim();
                if (!string.IsNullOrEmpty(path))
                {
                    // 移除路径两端的引号（如果有）
                    if (path.StartsWith("\"") && path.EndsWith("\"") && path.Length >= 2)
                    {
                        path = path.Substring(1, path.Length - 2);
                    }
                    paths.Add(path);
                }
            }
            
            return paths.ToArray();
        }
    }
}