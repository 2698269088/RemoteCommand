using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Remote_Command
{
    /// <summary>
    /// 命令处理器，负责处理各种MOT-RC命令
    /// </summary>
    public class CommandProcessor
    {
        private UdpCommunicationManager _udpManager;

        public CommandProcessor(UdpCommunicationManager udpManager)
        {
            _udpManager = udpManager;
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        /// <param name="message">接收到的消息</param>
        /// <param name="remoteEndpoint">远程终端点</param>
        public void ProcessReceivedMessage(string message, IPEndPoint remoteEndpoint, Action executeBatchFile, Action updateBatchFile, 
                                          Action sendProcessList, Action sendAppProcessListWithPaths, Action<string> terminateProcess)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // 检查是否为链接消息
            if (message.StartsWith("MOT-RC link "))
            {
                string ipAddressStr = message.Substring(12); // 移除"MOT-RC link "前缀
                if (IPAddress.TryParse(ipAddressStr, out IPAddress baseServerIp))
                {
                    // 设置基本UDP服务端地址
                    _udpManager.SetBaseServerEndpoint(new IPEndPoint(baseServerIp, 6746));
                    Logger.LogInfo($"设置基本UDP服务端地址为: {_udpManager.GetBaseServerEndpoint()}");

                    // 保存到配置文件
                    AppInitializer.SaveUdpServerIp(baseServerIp);

                    // 发送确认响应
                    _udpManager.SendResponseToBaseServer("MOT-RC ACK");
                }
                else
                {
                    Logger.LogError($"无效的IP地址格式: {ipAddressStr}");
                }
                return;
            }

            // 检查是否为动态命令消息
            if (message.StartsWith("MOT-RC cmd "))
            {
                string commandStr = message.Substring(11); // 移除"MOT-RC cmd "前缀
                Logger.LogInfo($"收到动态命令: {commandStr}");
                DynamicCommandExecutor.ExecuteDynamicCommand(commandStr, true);
                return;
            }

            // 如果已设置基本服务端地址，检查消息是否来自该地址
            if (_udpManager.GetBaseServerEndpoint() != null &&
                remoteEndpoint.Address.Equals(_udpManager.GetBaseServerEndpoint().Address))
            {
                // 处理来自基本服务端的命令
                ProcessCommandMessage(message, executeBatchFile, updateBatchFile, sendProcessList, sendAppProcessListWithPaths, terminateProcess);
            }
            else
            {
                // 处理广播命令（向后兼容）
                ProcessBroadcastMessage(message, executeBatchFile, updateBatchFile, sendProcessList, sendAppProcessListWithPaths, terminateProcess);
            }
        }

        /// <summary>
        /// 处理来自基本服务端的命令消息
        /// </summary>
        /// <param name="message">命令消息</param>
        private void ProcessCommandMessage(string message, Action executeBatchFile, Action updateBatchFile,
                                          Action sendProcessList, Action sendAppProcessListWithPaths, Action<string> terminateProcess)
        {
            if (!message.StartsWith("MOT-RC "))
                return;

            string command = message.Substring(7); // 移除"MOT-RC "前缀

            // 处理黑白名单模式切换命令
            if (command == "mode whitelist")
            {
                ConfigManager.SetUseWhitelist(true);
                _udpManager.SendResponseToBaseServer("MOT-RC mode result: 已切换到白名单模式");
                Logger.LogInfo("已切换到白名单模式");
                return;
            }
            else if (command == "mode blacklist")
            {
                ConfigManager.SetUseWhitelist(false);
                _udpManager.SendResponseToBaseServer("MOT-RC mode result: 已切换到黑名单模式");
                Logger.LogInfo("已切换到黑名单模式");
                return;
            }

            // 处理黑名单和白名单命令
            if (command.StartsWith("black add "))
            {
                string appList = command.Substring(10); // 移除"black add "前缀
                HandleBlacklistAdd(appList);
                return;
            }
            else if (command.StartsWith("black del "))
            {
                string appList = command.Substring(10); // 移除"black del "前缀
                HandleBlacklistRemove(appList);
                return;
            }
            else if (command.StartsWith("width add "))
            {
                string appList = command.Substring(10); // 移除"width add "前缀
                HandleWhitelistAdd(appList);
                return;
            }
            else if (command.StartsWith("width del "))
            {
                string appList = command.Substring(10); // 移除"width del "前缀
                HandleWhitelistRemove(appList);
                return;
            }
            else if (command.StartsWith("path_black add ")) // 添加路径黑名单处理
            {
                string pathList = command.Substring(15); // 移除"path_black add "前缀
                HandlePathBlacklistAdd(pathList);
                return;
            }
            else if (command.StartsWith("path_black del ")) // 删除路径黑名单处理
            {
                string pathList = command.Substring(15); // 移除"path_black del "前缀
                HandlePathBlacklistRemove(pathList);
                return;
            }
            else if (command.StartsWith("path_width add ") || command.StartsWith("path_white add ")) // 添加路径白名单处理
            {
                string prefix = command.StartsWith("path_width") ? "path_width add " : "path_white add ";
                string pathList = command.Substring(prefix.Length); // 移除前缀
                HandlePathWhitelistAdd(pathList);
                return;
            }
            else if (command.StartsWith("path_width del ") || command.StartsWith("path_white del ")) // 删除路径白名单处理
            {
                string prefix = command.StartsWith("path_width") ? "path_width del " : "path_white del ";
                string pathList = command.Substring(prefix.Length); // 移除前缀
                HandlePathWhitelistRemove(pathList);
                return;
            }
            else if (command == "path_mode whitelist") // 切换到路径白名单模式
            {
                ConfigManager.SetUsePathWhitelist(true);
                _udpManager.SendResponseToBaseServer("MOT-RC path_mode result: 已切换到路径白名单模式");
                Logger.LogInfo("已切换到路径白名单模式");
                return;
            }
            else if (command == "path_mode blacklist") // 切换到路径黑名单模式
            {
                ConfigManager.SetUsePathWhitelist(false);
                _udpManager.SendResponseToBaseServer("MOT-RC path_mode result: 已切换到路径黑名单模式");
                Logger.LogInfo("已切换到路径黑名单模式");
                return;
            }

            // 处理进程终止命令
            if (command.StartsWith("process taskkill "))
            {
                string processName = command.Substring(17); // 移除"process taskkill "前缀
                Logger.LogInfo($"收到终止进程命令: {processName}");
                terminateProcess?.Invoke(processName);
                return;
            }

            // 处理新增的功能控制命令
            if (command == "disable_nic_detection")
            {
                ConfigManager.SetNicDetectionEnabled(false);
                _udpManager.SendResponseToBaseServer("MOT-RC disable_nic_detection result: 已关闭网卡检测功能");
                Logger.LogInfo("已关闭网卡检测功能");
                return;
            }
            else if (command == "disable_all_features")
            {
                ConfigManager.SetAllFeaturesEnabled(false);
                _udpManager.SendResponseToBaseServer("MOT-RC disable_all_features result: 已关闭所有功能，不阻止未允许的应用程序运行");
                Logger.LogInfo("已关闭所有功能，不阻止未允许的应用程序运行");
                return;
            }
            else if (command == "restore_config")
            {
                ConfigManager.RestoreDefaultConfig();
                _udpManager.SendResponseToBaseServer("MOT-RC restore_config result: 已还原所有配置为默认状态");
                Logger.LogInfo("已还原所有配置为默认状态");
                return;
            }
            
            switch (command)
            {
                case "runcmd":
                    Logger.LogInfo("收到runcmd命令，准备执行批处理文件");
                    executeBatchFile?.Invoke();
                    break;
                case "update":
                    Logger.LogInfo("收到update命令，但URL下载功能已移除");
                    _udpManager.SendResponseToBaseServer("MOT-RC ERR URL下载功能已移除");
                    break;
                case "process list":
                    Logger.LogInfo("收到process list命令，准备获取进程列表");
                    sendProcessList?.Invoke();
                    break;
                case "process app":
                    Logger.LogInfo("收到process app命令，准备获取带路径的应用进程列表");
                    sendAppProcessListWithPaths?.Invoke();
                    break;
                default:
                    Logger.LogInfo($"收到未知命令: {command}");
                    break;
            }
        }

        /// <summary>
        /// 处理广播消息（向后兼容）
        /// </summary>
        /// <param name="message">广播消息</param>
        private void ProcessBroadcastMessage(string message, Action executeBatchFile, Action updateBatchFile,
                                            Action sendProcessList, Action sendAppProcessListWithPaths, Action<string> terminateProcess)
        {
            if (!message.StartsWith("MOT-RC "))
                return;

            string command = message.Substring(7); // 移除"MOT-RC "前缀

            // 处理黑白名单模式切换命令
            if (command == "mode whitelist")
            {
                ConfigManager.SetUseWhitelist(true);
                _udpManager.SendResponseToBaseServer("MOT-RC mode result: 已切换到白名单模式");
                Logger.LogInfo("已切换到白名单模式");
                return;
            }
            else if (command == "mode blacklist")
            {
                ConfigManager.SetUseWhitelist(false);
                _udpManager.SendResponseToBaseServer("MOT-RC mode result: 已切换到黑名单模式");
                Logger.LogInfo("已切换到黑名单模式");
                return;
            }

            // 处理黑名单和白名单命令
            if (command.StartsWith("black add "))
            {
                string appList = command.Substring(10); // 移除"black add "前缀
                HandleBlacklistAdd(appList);
                return;
            }
            else if (command.StartsWith("black del "))
            {
                string appList = command.Substring(10); // 移除"black del "前缀
                HandleBlacklistRemove(appList);
                return;
            }
            else if (command.StartsWith("width add "))
            {
                string appList = command.Substring(10); // 移除"width add "前缀
                HandleWhitelistAdd(appList);
                return;
            }
            else if (command.StartsWith("width del "))
            {
                string appList = command.Substring(10); // 移除"width del "前缀
                HandleWhitelistRemove(appList);
                return;
            }
            else if (command.StartsWith("path_black add ")) // 添加路径黑名单处理
            {
                string pathList = command.Substring(15); // 移除"path_black add "前缀
                HandlePathBlacklistAdd(pathList);
                return;
            }
            else if (command.StartsWith("path_black del ")) // 删除路径黑名单处理
            {
                string pathList = command.Substring(15); // 移除"path_black del "前缀
                HandlePathBlacklistRemove(pathList);
                return;
            }
            else if (command.StartsWith("path_width add ") || command.StartsWith("path_white add ")) // 添加路径白名单处理
            {
                string prefix = command.StartsWith("path_width") ? "path_width add " : "path_white add ";
                string pathList = command.Substring(prefix.Length); // 移除前缀
                HandlePathWhitelistAdd(pathList);
                return;
            }
            else if (command.StartsWith("path_width del ") || command.StartsWith("path_white del ")) // 删除路径白名单处理
            {
                string prefix = command.StartsWith("path_width") ? "path_width del " : "path_white del ";
                string pathList = command.Substring(prefix.Length); // 移除前缀
                HandlePathWhitelistRemove(pathList);
                return;
            }
            else if (command == "path_mode whitelist") // 切换到路径白名单模式
            {
                ConfigManager.SetUsePathWhitelist(true);
                _udpManager.SendResponseToBaseServer("MOT-RC path_mode result: 已切换到路径白名单模式");
                Logger.LogInfo("已切换到路径白名单模式");
                return;
            }
            else if (command == "path_mode blacklist") // 切换到路径黑名单模式
            {
                ConfigManager.SetUsePathWhitelist(false);
                _udpManager.SendResponseToBaseServer("MOT-RC path_mode result: 已切换到路径黑名单模式");
                Logger.LogInfo("已切换到路径黑名单模式");
                return;
            }

            // 处理进程终止命令
            if (command.StartsWith("process taskkill "))
            {
                string processName = command.Substring(17); // 移除"process taskkill "前缀
                Logger.LogInfo($"收到广播终止进程命令: {processName}");
                terminateProcess?.Invoke(processName);
                return;
            }

            // 处理新增的功能控制命令
            if (command == "disable_nic_detection")
            {
                ConfigManager.SetNicDetectionEnabled(false);
                _udpManager.SendResponseToBaseServer("MOT-RC disable_nic_detection result: 已关闭网卡检测功能");
                Logger.LogInfo("已关闭网卡检测功能");
                return;
            }
            else if (command == "disable_all_features")
            {
                ConfigManager.SetAllFeaturesEnabled(false);
                _udpManager.SendResponseToBaseServer("MOT-RC disable_all_features result: 已关闭所有功能，不阻止未允许的应用程序运行");
                Logger.LogInfo("已关闭所有功能，不阻止未允许的应用程序运行");
                return;
            }
            else if (command == "restore_config")
            {
                ConfigManager.RestoreDefaultConfig();
                _udpManager.SendResponseToBaseServer("MOT-RC restore_config result: 已还原所有配置为默认状态");
                Logger.LogInfo("已还原所有配置为默认状态");
                return;
            }
            
            switch (command)
            {
                case "runcmd":
                    Logger.LogInfo("收到广播runcmd命令，准备执行批处理文件");
                    executeBatchFile?.Invoke();
                    break;
                case "update":
                    Logger.LogInfo("收到广播update命令，但URL下载功能已移除");
                    _udpManager.SendResponseToBaseServer("MOT-RC ERR URL下载功能已移除");
                    break;
                case "process list":
                    Logger.LogInfo("收到广播process list命令，准备获取进程列表");
                    sendProcessList?.Invoke();
                    break;
                case "process app":
                    Logger.LogInfo("收到广播process app命令，准备获取带路径的应用进程列表");
                    sendAppProcessListWithPaths?.Invoke();
                    break;
                default:
                    Logger.LogInfo($"收到未知广播命令: {command}");
                    break;
            }
        }

        #region 黑白名单处理方法

        /// <summary>
        /// 处理添加到黑名单的命令
        /// </summary>
        /// <param name="appList">以逗号分隔的应用程序列表</param>
        private void HandleBlacklistAdd(string appList)
        {
            try
            {
                string[] apps = appList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int addedCount = 0;

                foreach (string app in apps)
                {
                    string trimmedApp = app.Trim();
                    if (!string.IsNullOrEmpty(trimmedApp))
                    {
                        // ConfigManager会自动处理重复项
                        ConfigManager.AddToBlacklist(trimmedApp);
                        addedCount++;
                    }
                }

                string response = $"MOT-RC black add result: 成功添加 {addedCount} 个应用程序到黑名单";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);
            }
            catch (Exception ex)
            {
                string error = $"处理黑名单添加命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 处理从黑名单移除的命令
        /// </summary>
        /// <param name="appList">以逗号分隔的应用程序列表</param>
        private void HandleBlacklistRemove(string appList)
        {
            try
            {
                string[] apps = appList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int removedCount = 0;

                foreach (string app in apps)
                {
                    string trimmedApp = app.Trim();
                    if (!string.IsNullOrEmpty(trimmedApp))
                    {
                        // ConfigManager会自动处理不存在的项
                        ConfigManager.RemoveFromBlacklist(trimmedApp);
                        removedCount++;
                    }
                }

                string response = $"MOT-RC black del result: 成功从黑名单移除 {removedCount} 个应用程序";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);
            }
            catch (Exception ex)
            {
                string error = $"处理黑名单移除命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 处理添加到白名单的命令
        /// </summary>
        /// <param name="appList">以逗号分隔的应用程序列表</param>
        private void HandleWhitelistAdd(string appList)
        {
            try
            {
                string[] apps = appList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int addedCount = 0;

                foreach (string app in apps)
                {
                    string trimmedApp = app.Trim();
                    if (!string.IsNullOrEmpty(trimmedApp))
                    {
                        // ConfigManager会自动处理重复项
                        ConfigManager.AddToWhitelist(trimmedApp);
                        addedCount++;
                    }
                }

                string response = $"MOT-RC width add result: 成功添加 {addedCount} 个应用程序到白名单";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);
            }
            catch (Exception ex)
            {
                string error = $"处理白名单添加命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 处理从白名单移除的命令
        /// </summary>
        /// <param name="appList">以逗号分隔的应用程序列表</param>
        private void HandleWhitelistRemove(string appList)
        {
            try
            {
                string[] apps = appList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int removedCount = 0;

                foreach (string app in apps)
                {
                    string trimmedApp = app.Trim();
                    if (!string.IsNullOrEmpty(trimmedApp))
                    {
                        // ConfigManager会自动处理不存在的项
                        ConfigManager.RemoveFromWhitelist(trimmedApp);
                        removedCount++;
                    }
                }

                string response = $"MOT-RC width del result: 成功从白名单移除 {removedCount} 个应用程序";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);
            }
            catch (Exception ex)
            {
                string error = $"处理白名单移除命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 处理添加到路径黑名单的命令
        /// </summary>
        /// <param name="pathList">以逗号分隔的路径列表</param>
        private void HandlePathBlacklistAdd(string pathList)
        {
            try
            {
                string[] paths = ParseQuotedPathList(pathList);
                int addedCount = 0;

                foreach (string path in paths)
                {
                    string trimmedPath = path.Trim();
                    if (!string.IsNullOrEmpty(trimmedPath))
                    {
                        // PathBlacklistManager会自动处理重复项
                        if (PathBlacklistManager.AddPathToBlacklist(trimmedPath))
                        {
                            addedCount++;
                        }
                    }
                }

                string response = $"MOT-RC path_black add result: 成功添加 {addedCount} 个路径到路径黑名单";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);

                // 触发一次进程检查
                ThreadPool.QueueUserWorkItem(_ => {
                    Thread.Sleep(1000); // 等待1秒，确保进程已完全启动
                    ProcessMonitor.CheckProcessesOnce();
                });
            }
            catch (Exception ex)
            {
                string error = $"处理路径黑名单添加命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 处理从路径黑名单移除的命令
        /// </summary>
        /// <param name="pathList">以逗号分隔的路径列表</param>
        private void HandlePathBlacklistRemove(string pathList)
        {
            try
            {
                string[] paths = ParseQuotedPathList(pathList);
                int removedCount = 0;

                foreach (string path in paths)
                {
                    string trimmedPath = path.Trim();
                    if (!string.IsNullOrEmpty(trimmedPath))
                    {
                        // PathBlacklistManager会自动处理不存在的项
                        if (PathBlacklistManager.RemovePathFromBlacklist(trimmedPath))
                        {
                            removedCount++;
                        }
                    }
                }

                string response = $"MOT-RC path_black del result: 成功从路径黑名单移除 {removedCount} 个路径";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);
            }
            catch (Exception ex)
            {
                string error = $"处理路径黑名单移除命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 处理添加到路径白名单的命令
        /// </summary>
        /// <param name="pathList">以逗号分隔的路径列表</param>
        private void HandlePathWhitelistAdd(string pathList)
        {
            try
            {
                string[] paths = ParseQuotedPathList(pathList);
                int addedCount = 0;

                foreach (string path in paths)
                {
                    string trimmedPath = path.Trim();
                    if (!string.IsNullOrEmpty(trimmedPath))
                    {
                        // PathWhitelistManager会自动处理重复项
                        if (PathWhitelistManager.AddPathToWhitelist(trimmedPath))
                        {
                            addedCount++;
                        }
                    }
                }

                string response = $"MOT-RC path_width add result: 成功添加 {addedCount} 个路径到路径白名单";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);

                // 触发一次进程检查
                ThreadPool.QueueUserWorkItem(_ => {
                    Thread.Sleep(1000); // 等待1秒，确保进程已完全启动
                    ProcessMonitor.CheckProcessesOnce();
                });
            }
            catch (Exception ex)
            {
                string error = $"处理路径白名单添加命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 处理从路径白名单移除的命令
        /// </summary>
        /// <param name="pathList">以逗号分隔的路径列表</param>
        private void HandlePathWhitelistRemove(string pathList)
        {
            try
            {
                string[] paths = ParseQuotedPathList(pathList);
                int removedCount = 0;

                foreach (string path in paths)
                {
                    string trimmedPath = path.Trim();
                    if (!string.IsNullOrEmpty(trimmedPath))
                    {
                        // PathWhitelistManager会自动处理不存在的项
                        if (PathWhitelistManager.RemovePathFromWhitelist(trimmedPath))
                        {
                            removedCount++;
                        }
                    }
                }

                string response = $"MOT-RC path_width del result: 成功从路径白名单移除 {removedCount} 个路径";
                _udpManager.SendResponseToBaseServer(response);
                Logger.LogInfo(response);
            }
            catch (Exception ex)
            {
                string error = $"处理路径白名单移除命令时发生错误: {ex.Message}";
                Logger.LogError(error);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {error}");
            }
        }

        /// <summary>
        /// 解析带引号的路径列表
        /// 支持格式：
        /// 1. 不带引号的标准路径：C:\Program Files\MyApp,D:\Test Folder
        /// 2. 使用引号包裹的路径："C:\Program Files\MyApp","D:\Test Folder"
        /// 3. 混合使用引号和无引号的路径："C:\Program Files\MyApp",D:\TestFolder,"E:\Another Folder\Subfolder"
        /// 4. 包含逗号的路径（使用引号）："C:\Path,With,Commas","D:\NormalPath"
        /// </summary>
        /// <param name="pathList">原始路径列表字符串</param>
        /// <returns>解析后的路径数组</returns>
        private string[] ParseQuotedPathList(string pathList)
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

        #endregion
    }
}