using System;
using System.IO;

namespace Remote_Command
{
    /// <summary>
    /// 日志记录类，用于将日志信息写入文件而不是控制台
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "application.log");

        /// <summary>
        /// 将消息写入日志文件
        /// </summary>
        /// <param name="message">要记录的消息</param>
        public static void Log(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // 忽略日志记录错误，避免影响主程序流程
            }
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">错误消息</param>
        public static void LogError(string message)
        {
            Log($"[ERROR] {message}");
        }

        /// <summary>
        /// 记录信息性信息
        /// </summary>
        /// <param name="message">信息消息</param>
        public static void LogInfo(string message)
        {
            Log($"[INFO] {message}");
        }
        
        /// <summary>
        ///  记录警告性信息
        /// </summary>
        /// <param name="message"></param>
        
        public static void LogWarning(string message)
        {
            Log($"[WARNING] {message}");
        }
    }
}