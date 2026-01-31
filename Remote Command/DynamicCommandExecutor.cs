using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Remote_Command
{
    /// <summary>
    /// 负责执行动态命令并返回结果的类
    /// </summary>
    public static class DynamicCommandExecutor
    {
        // 添加对UdpCommunicationManager的引用
        public static UdpCommunicationManager UdpManager { get; set; }

        /// <summary>
        /// 执行动态命令并将结果返回给基本UDP服务端
        /// </summary>
        /// <param name="command">要执行的命令</param>
        public static void ExecuteDynamicCommand(string command)
        {
            ExecuteDynamicCommand(command, false);
        }
        
        /// <summary>
        /// 执行动态命令并将结果返回给基本UDP服务端
        /// </summary>
        /// <param name="command">要执行的命令</param>
        /// <param name="isCmdSpecified">是否是由cmd指令指定的代码执行</param>
        public static void ExecuteDynamicCommand(string command, bool isCmdSpecified = false)
        {
            try
            {
                Logger.LogInfo($"开始执行动态命令: {command}");
                
                // 创建临时批处理文件
                var tempBatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_command.bat");
                File.WriteAllText(tempBatPath, command, Encoding.Default);
                
                // 设置日志文件路径
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dynamic_command_execution.log");
                
                // 清空之前的日志内容
                File.WriteAllText(logFilePath, "", Encoding.UTF8);
                
                // 记录开始执行的信息
                string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string startRecord = $"=== 动态命令执行开始 ===\n开始时间: {startTime}\n执行命令: {command}\n\n";
                File.AppendAllText(logFilePath, startRecord, Encoding.UTF8);
                Logger.LogInfo($"[{startTime}] 开始执行动态命令: {command}");

                // 配置进程启动信息
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chcp 65001 > nul && \"{tempBatPath}\"",
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
                    
                    Logger.LogInfo("动态命令已在后台执行");
                    process.WaitForExit(); // 等待执行完成
                    
                    // 记录执行完成的信息
                    string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string endRecord = $"\n=== 动态命令执行结束 ===\n结束时间: {endTime}\n退出代码: {process.ExitCode}\n========================";
                    
                    // 将执行结果写入日志文件
                    File.AppendAllText(logFilePath, endRecord, Encoding.UTF8);
                    
                    Logger.LogInfo($"[{endTime}] 动态命令执行完成，退出代码: {process.ExitCode}");
                    
                    // 读取完整的执行日志
                    string executionLog = File.ReadAllText(logFilePath, Encoding.UTF8);
                    
                    // 向基本UDP服务端发送执行结果
                    UdpManager?.SendResponseToBaseServer($"MOT-RC RES 动态命令执行完成，退出代码: {process.ExitCode}\n执行日志:\n{executionLog}");
                    
                    // 对于动态命令，如果是由cmd指令指定的代码执行，则不上传日志
                    if (!isCmdSpecified)
                    {
                        // 移除了FTP上传功能
                        Logger.LogInfo("FTP上传功能已移除，跳过FTP日志上传");
                    }
                    else
                    {
                        Logger.LogInfo("跳过FTP日志上传");
                    }
                    
                    // 如果是由cmd指令指定的代码执行，将执行结果发送回服务端
                    if (isCmdSpecified)
                    {
                        string escapedLog = executionLog.Replace("\"", "\\\"");
                        UdpManager?.SendResponseToBaseServer($"MOT-RC cmdlog \"{escapedLog}\"");
                    }
                }
                
                // 执行完成后删除临时文件
                try
                {
                    if (File.Exists(tempBatPath))
                    {
                        File.Delete(tempBatPath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"删除临时批处理文件时出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"执行动态命令时发生错误: {ex.Message}");
                UdpManager?.SendResponseToBaseServer($"MOT-RC ERR 执行动态命令时发生错误: {ex.Message}");
            }
        }
    }
}