using System;
using System.Diagnostics;
using System.Linq;

namespace Remote_Command
{
    /// <summary>
    /// 负责终止指定进程的类
    /// </summary>
    public static class ProcessTerminator
    {
        /// <summary>
        /// 根据进程名终止进程
        /// </summary>
        /// <param name="processName">要终止的进程名</param>
        /// <returns>操作结果信息</returns>
        public static string TerminateProcessByName(string processName)
        {
            try
            {
                // 移除.exe扩展名（如果存在）
                string cleanProcessName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
                    ? processName.Substring(0, processName.Length - 4) 
                    : processName;
                
                // 查找匹配的进程
                Process[] processes = Process.GetProcessesByName(cleanProcessName);
                
                if (processes.Length == 0)
                {
                    return $"未找到名为 '{processName}' 的进程";
                }
                
                int terminatedCount = 0;
                Exception lastException = null;
                
                // 终止所有匹配的进程
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000); // 等待最多5秒直到进程退出
                        terminatedCount++;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }
                
                if (terminatedCount > 0)
                {
                    if (lastException != null)
                    {
                        return $"成功终止了 {terminatedCount} 个 '{processName}' 进程，但部分进程终止时出现错误: {lastException.Message}";
                    }
                    else
                    {
                        return $"成功终止了 {terminatedCount} 个 '{processName}' 进程";
                    }
                }
                else
                {
                    return $"未能终止任何 '{processName}' 进程: {lastException?.Message ?? "未知错误"}";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"终止进程 '{processName}' 时发生错误: {ex.Message}");
                return $"终止进程 '{processName}' 失败: {ex.Message}";
            }
        }
    }
}