using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Remote_Command
{
    /// <summary>
    /// 进程监控类，用于定期检查并根据黑白名单规则终止不符合要求的进程
    /// </summary>
    public static class ProcessMonitor
    {
        private static Thread _monitorThread;
        private static bool _isMonitoring = false;
        private static readonly int CheckInterval = 10000; // 10秒检查一次

        /// <summary>
        /// 启动进程监控
        /// </summary>
        public static void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _monitorThread = new Thread(MonitorProcesses)
            {
                IsBackground = true
            };
            _monitorThread.Start();
            Logger.LogInfo("进程监控已启动，每10秒检查一次");
        }

        /// <summary>
        /// 停止进程监控
        /// </summary>
        public static void StopMonitoring()
        {
            _isMonitoring = false;
            Logger.LogInfo("进程监控已停止");
        }

        /// <summary>
        /// 立即执行一次进程检查
        /// </summary>
        public static void CheckProcessesOnce()
        {
            CheckAndTerminateProcesses();
        }

        /// <summary>
        /// 监控进程的主循环
        /// </summary>
        private static void MonitorProcesses()
        {
            while (_isMonitoring)
            {
                try
                {
                    CheckAndTerminateProcesses();
                    Thread.Sleep(CheckInterval);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"进程监控时发生错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 检查并根据黑白名单规则终止进程
        /// </summary>
        private static void CheckAndTerminateProcesses()
        {
            // 检查所有功能是否启用
            if (!ConfigManager.GetAllFeaturesEnabled())
            {
                Logger.LogInfo("所有功能已禁用，不阻止任何应用程序运行");
                return;
            }
            
            try
            {
                // 获取所有正在运行的进程
                Process[] processes = Process.GetProcesses();
                
                bool useWhitelist = ConfigManager.GetUseWhitelist();
                bool usePathWhitelist = ConfigManager.GetUsePathWhitelist();
                
                if (useWhitelist)
                {
                    // 白名单模式：只允许白名单中的进程运行，其他进程将被终止
                    var whitelist = ConfigManager.GetWhitelist();
                    
                    foreach (Process process in processes)
                    {
                        try
                        {
                            // 只处理应用进程（具有可见窗口的进程）
                            if (string.IsNullOrEmpty(process.MainWindowTitle))
                                continue;
                                
                            // 跳过系统关键进程
                            if (IsCriticalProcess(process.ProcessName))
                                continue;
                                
                            // 检查进程是否在白名单中
                            if (!IsProcessInList(process.ProcessName, whitelist))
                            {
                                // 终止不在白名单中的进程
                                Logger.LogInfo($"白名单模式：终止应用进程 {process.ProcessName} (PID: {process.Id})");
                                TerminateProcess(process);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 忽略单个进程处理异常，继续处理其他进程
                            Logger.LogError($"处理进程 {process.ProcessName} 时发生错误: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // 黑名单模式：禁止黑名单中的进程运行
                    var blacklist = ConfigManager.GetBlacklist();
                    
                    foreach (Process process in processes)
                    {
                        try
                        {
                            // 只处理应用进程（具有可见窗口的进程）
                            if (string.IsNullOrEmpty(process.MainWindowTitle))
                                continue;
                            
                            // 检查进程是否在普通黑名单中
                            if (IsProcessInList(process.ProcessName, blacklist))
                            {
                                // 终止黑名单中的进程
                                Logger.LogInfo($"黑名单模式：终止应用进程 {process.ProcessName} (PID: {process.Id})");
                                TerminateProcess(process);
                                continue;
                            }
                            
                            // 检查进程是否在路径黑名单中
                            try
                            {
                                string processPath = GetProcessPath(process);
                                if (!string.IsNullOrEmpty(processPath))
                                {
                                    // 检查是否使用路径白名单模式
                                    if (usePathWhitelist)
                                    {
                                        // 路径白名单模式：只允许路径白名单中的进程运行
                                        if (!PathWhitelistManager.IsPathInWhitelist(processPath))
                                        {
                                            Logger.LogInfo($"路径白名单模式：终止应用进程 {process.ProcessName} (PID: {process.Id}) 路径: {processPath}");
                                            TerminateProcess(process);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // 路径黑名单模式：禁止路径黑名单中的进程运行
                                        if (PathBlacklistManager.IsPathBlocked(processPath))
                                        {
                                            Logger.LogInfo($"路径黑名单模式：终止应用进程 {process.ProcessName} (PID: {process.Id}) 路径: {processPath}");
                                            TerminateProcess(process);
                                            continue;
                                        }
                                    }
                                }
                            }
                            catch (Exception pathEx)
                            {
                                // 忽略路径获取异常，继续处理其他进程
                                Logger.LogError($"获取进程 {process.ProcessName} 路径时发生错误: {pathEx.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 忽略单个进程处理异常，继续处理其他进程
                            Logger.LogError($"处理进程 {process.ProcessName} 时发生错误: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"检查和终止进程时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 终止指定进程
        /// </summary>
        /// <param name="process">要终止的进程</param>
        private static void TerminateProcess(Process process)
        {
            try
            {
                process.Kill();
                process.WaitForExit(5000); // 等待最多5秒直到进程退出
            }
            catch (Exception ex)
            {
                Logger.LogError($"终止进程 {process.ProcessName} 时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查进程是否在指定列表中
        /// </summary>
        /// <param name="processName">进程名称</param>
        /// <param name="list">检查列表</param>
        /// <returns>如果在列表中返回true，否则返回false</returns>
        private static bool IsProcessInList(string processName, System.Collections.Generic.List<string> list)
        {
            // 移除.exe扩展名进行比较
            string cleanProcessName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
                ? processName.Substring(0, processName.Length - 4) 
                : processName;
                
            // 检查完整名称和清理后的名称是否在列表中（大小写不敏感）
            return list.Any(item => 
                string.Equals(item, processName, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(item, cleanProcessName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 检查是否为关键系统进程
        /// </summary>
        /// <param name="processName">进程名称</param>
        /// <returns>如果是关键进程返回true，否则返回false</returns>
        private static bool IsCriticalProcess(string processName)
        {
            // 关键系统进程列表（不完整，可根据需要扩展）
            string[] criticalProcesses = {
                "csrss", "winlogon", "wininit", "services", "lsass", "smss",
                "svchost", "explorer", "dwm", "System", "Idle", "Registry",
                "Remote_Command" // 排除自身进程
            };

            string cleanProcessName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
                ? processName.Substring(0, processName.Length - 4) 
                : processName;

            return criticalProcesses.Contains(cleanProcessName, StringComparer.OrdinalIgnoreCase) ||
                   criticalProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase);
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