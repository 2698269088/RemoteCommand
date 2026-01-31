using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Remote_Command
{
    /// <summary>
    /// 负责执行命令文件并记录执行日志的类
    /// </summary>
    public static class CommandExecutor
    {
        // 添加对UdpCommunicationManager的引用
        public static UdpCommunicationManager UdpManager { get; set; }

        /// <summary>
        /// 执行指定的批处理文件，并将执行日志记录到文件中
        /// </summary>
        /// <param name="batFilePath">要执行的批处理文件路径</param>
        /// <param name="uploadToFtp">是否上传日志到FTP服务器，默认为true</param>
        /// <param name="fromUdpServer">是否来自UDP服务器的命令，默认为false</param>
        /// <param name="isCmdSpecified">是否是由cmd指令指定的代码执行，默认为false</param>
        public static void ExecuteCommand(string batFilePath, bool uploadToFtp = true, bool fromUdpServer = false, bool isCmdSpecified = false)
        {
            if (!File.Exists(batFilePath))
            {
                Logger.LogError($"批处理文件不存在: {batFilePath}");
                // 向基本UDP服务端发送错误信息
                UdpManager?.SendResponseToBaseServer($"MOT-RC ERR 批处理文件不存在: {batFilePath}");
                return;
            }

            try
            {
                // 设置日志文件路径
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "command_execution.log");
                
                // 清空之前的日志内容
                File.WriteAllText(logFilePath, "", Encoding.UTF8);
                
                // 记录开始执行的信息
                string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string startRecord = $"=== CMD命令执行开始 ===\n开始时间: {startTime}\n执行文件: {batFilePath}\n\n";
                File.AppendAllText(logFilePath, startRecord, Encoding.UTF8);
                Logger.LogInfo($"[{startTime}] 开始执行批处理文件: {batFilePath}");

                // 配置进程启动信息，使用更可靠的输出捕获方式
                // 添加chcp 65001命令来设置UTF-8编码
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chcp 65001 > nul && \"{batFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas", // 以管理员身份运行
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                // 创建并启动进程
                using (var process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    
                    // 事件处理程序，实时捕获输出
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            try
                            {
                                File.AppendAllText(logFilePath, e.Data + Environment.NewLine, Encoding.UTF8);
                            }
                            catch
                            {
                                // 忽略写入日志文件的错误
                            }
                        }
                    };
                    
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            try
                            {
                                File.AppendAllText(logFilePath, e.Data + Environment.NewLine, Encoding.UTF8);
                            }
                            catch
                            {
                                // 忽略写入日志文件的错误
                            }
                        }
                    };

                    process.Start();
                    
                    // 开始异步读取输出
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    
                    Logger.LogInfo("批处理命令已在后台执行");
                    process.WaitForExit(); // 等待执行完成
                    
                    // 记录执行完成的信息
                    string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string endRecord = $"\n=== CMD命令执行结束 ===\n结束时间: {endTime}\n退出代码: {process.ExitCode}\n========================";
                    
                    // 将执行结果写入日志文件
                    File.AppendAllText(logFilePath, endRecord, Encoding.UTF8);
                    
                    Logger.LogInfo($"[{endTime}] 命令执行完成，退出代码: {process.ExitCode}");
                    
                    // 向基本UDP服务端发送执行结果
                    UdpManager?.SendResponseToBaseServer($"MOT-RC RES 命令执行完成，退出代码: {process.ExitCode}");
                    
                    // 根据参数决定是否上传日志文件到FTP服务器
                    // 如果是由cmd指令指定的代码执行，则不上传日志
                    if (uploadToFtp && !isCmdSpecified)
                    {
                        // 移除了FTP上传功能
                        Logger.LogInfo("FTP上传功能已移除，跳过FTP日志上传");
                    }
                    else
                    {
                        Logger.LogInfo("跳过FTP日志上传");
                    }
                    
                    // 如果命令来自UDP服务器，则发送命令日志
                    if (fromUdpServer)
                    {
                        // 读取完整的执行日志
                        string executionLog = File.ReadAllText(logFilePath, Encoding.UTF8);
                        // 向基本UDP服务端发送命令日志
                        UdpManager?.SendResponseToBaseServer($"MOT-RC cmdlog [{executionLog.Replace("\"", "\\\"")}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"执行批处理文件时发生错误: {ex.Message}");
                
                // 记录错误到日志文件
                try
                {
                    var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "command_execution.log");
                    string errorTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string errorRecord = $"\n=== CMD命令执行异常 ===\n异常时间: {errorTime}\n异常信息: {ex.Message}\n========================";
                    File.AppendAllText(logFilePath, errorRecord, Encoding.UTF8);
                    
                    // 向基本UDP服务端发送错误信息
                    UdpManager?.SendResponseToBaseServer($"MOT-RC ERR 执行批处理文件时发生错误: {ex.Message}");
                }
                catch
                {
                    // 忽略日志记录错误
                }
            }
        }
    }
}