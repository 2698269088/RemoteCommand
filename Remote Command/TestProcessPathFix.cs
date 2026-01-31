using System;
using System.Diagnostics;

namespace Remote_Command
{
    /// <summary>
    /// 测试修复后的进程路径获取功能
    /// </summary>
    public class TestProcessPathFix
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing fixed process path retrieval...");
            
            try
            {
                // 获取所有进程
                Process[] processes = Process.GetProcesses();
                
                int successCount = 0;
                int failCount = 0;
                
                Console.WriteLine("\nChecking process paths:");
                foreach (Process process in processes)
                {
                    try
                    {
                        // 只测试具有可见窗口的应用进程
                        if (!string.IsNullOrEmpty(process.MainWindowTitle))
                        {
                            string path = GetProcessPath(process);
                            if (!string.IsNullOrEmpty(path) && path != "Unknown")
                            {
                                Console.WriteLine($"✓ {process.ProcessName}: {path}");
                                successCount++;
                            }
                            else
                            {
                                Console.WriteLine($"✗ {process.ProcessName}: Unable to retrieve path");
                                failCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ {process.ProcessName}: Error - {ex.Message}");
                        failCount++;
                    }
                }
                
                Console.WriteLine($"\nResults: {successCount} successful, {failCount} failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// 安全地获取进程路径
        /// </summary>
        /// <param name="process">进程对象</param>
        /// <returns>进程路径，如果无法获取则返回null</returns>
        private static string GetProcessPath(Process process)
        {
            try
            {
                // 首先尝试通过MainModule获取路径
                return process.MainModule?.FileName;
            }
            catch
            {
                // 如果失败，尝试其他方法获取路径
                try
                {
                    // 使用进程查询方法（更兼容32/64位混合环境）
                    return GetProcessPathAlternative(process.Id);
                }
                catch
                {
                    // 如果所有方法都失败，返回null
                    return null;
                }
            }
        }

        /// <summary>
        /// 替代方法获取进程路径
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>进程路径</returns>
        private static string GetProcessPathAlternative(int processId)
        {
            try
            {
                // 使用WMI查询进程路径（更好的32/64位兼容性）
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string path = item["ExecutablePath"]?.ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            return path;
                        }
                    }
                }
            }
            catch
            {
                // 忽略WMI查询异常
            }
            
            // 如果WMI方法也失败，返回null
            return null;
        }
    }
}