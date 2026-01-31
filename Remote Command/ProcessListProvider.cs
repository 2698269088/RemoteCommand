using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Remote_Command
{
    /// <summary>
    /// 提供系统进程列表信息的类
    /// </summary>
    public static class ProcessListProvider
    {
        /// <summary>
        /// 获取系统进程列表并按应用进程和后台进程分类
        /// </summary>
        /// <returns>包含应用进程和后台进程列表的字符串，格式为：app:"应用进程" bg:"后台进程"</returns>
        public static string GetProcessList()
        {
            try
            {
                // 获取所有进程
                Process[] processes = Process.GetProcesses();
                
                // 分类进程：应用进程(有可见窗口)和后台进程(无可见窗口)
                var appProcesses = processes.Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
                var backgroundProcesses = processes.Where(p => string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
                
                // 构造应用进程名称列表
                string appProcessNames = string.Join(", ", appProcesses.Select(p => p.ProcessName));
                
                // 构造后台进程名称列表
                string backgroundProcessNames = string.Join(", ", backgroundProcesses.Select(p => p.ProcessName));
                
                // 返回格式化的结果
                return $"app:\"{appProcessNames}\" bg:\"{backgroundProcessNames}\"";
            }
            catch (Exception ex)
            {
                Logger.LogError($"获取进程列表时发生错误: {ex.Message}");
                return $"err:\"获取进程列表失败: {ex.Message}\"";
            }
        }
        
        /// <summary>
        /// 获取应用进程列表及其完整路径
        /// </summary>
        /// <returns>包含应用进程路径和名称的字符串，格式为：apl:"路径1+进程名1,路径2+进程名2"</returns>
        public static string GetAppProcessListWithPaths()
        {
            try
            {
                // 获取所有进程
                Process[] processes = Process.GetProcesses();
                
                // 筛选出应用进程(有可见窗口)
                var appProcesses = processes.Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
                
                // 构造应用进程路径和名称列表
                var appProcessDetails = appProcesses.Select(p => {
                    try 
                    {
                        // 尝试获取进程的完整路径
                        string fullPath = GetProcessPath(p) ?? "Unknown";
                        return $"{fullPath}+{p.ProcessName}";
                    }
                    catch 
                    {
                        // 如果无法获取路径，则使用Unknown
                        return $"Unknown+{p.ProcessName}";
                    }
                });
                
                string appProcessList = string.Join(",", appProcessDetails);
                
                // 返回格式化的结果
                return $"apl:\"{appProcessList}\"";
            }
            catch (Exception ex)
            {
                Logger.LogError($"获取应用进程列表时发生错误: {ex.Message}");
                return $"err:\"获取应用进程列表失败: {ex.Message}\"";
            }
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