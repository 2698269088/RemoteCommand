using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Remote_Command
{
    /// <summary>
    /// 监听UDP广播消息并处理命令的类
    /// </summary>
    public class UdpListener
    {
        private NetworkAdapterManager _networkManager;
        private UdpCommunicationManager _udpManager;
        private CommandProcessor _commandProcessor;

        /// <summary>
        /// 启动UDP监听器
        /// </summary>
        public void StartListening()
        {
            try
            {
                Logger.LogInfo("开始启动UDP监听器");
                
                // 初始化各个管理器
                _networkManager = new NetworkAdapterManager();
                _udpManager = new UdpCommunicationManager();
                _commandProcessor = new CommandProcessor(_udpManager);
                
                // 设置CommandExecutor和DynamicCommandExecutor的UDP管理器
                CommandExecutor.UdpManager = _udpManager;
                DynamicCommandExecutor.UdpManager = _udpManager;

                // 获取非虚拟网卡的IP地址
                var localIp = _networkManager.GetNonVirtualLocalIpAddress();
                if (localIp == null)
                {
                    Logger.LogWarning("未找到有效的非虚拟网卡IP地址，将监听所有网络接口并启动网络设备热更新");
                    // 启动网络设备热更新检查，每5秒检查一次
                    _networkManager.StartNetworkDeviceCheck(state => CheckNetworkDevices());
                    // 监听所有网络接口
                    localIp = IPAddress.Any;
                }
                else
                {
                    // 记录当前绑定的IP地址
                    _networkManager.SetCurrentBoundIp(localIp);
                    // 启动IP地址变化检查，每10秒检查一次
                    _networkManager.StartIpAddressCheck(state => CheckIpAddressChange());
                }

                // 从配置文件读取UDP服务器地址
                var configuredIp = AppInitializer.ReadUdpServerIp();
                if (configuredIp != null)
                {
                    _udpManager.SetBaseServerEndpoint(new IPEndPoint(configuredIp, 6746));
                    Logger.LogInfo($"从配置文件加载UDP服务器地址: {_udpManager.GetBaseServerEndpoint()}");
                }

                // 启动UDP监听
                _udpManager.StartListening(localIp, ReceiveCallback);
            }
            catch (Exception ex)
            {
                Logger.LogError($"启动UDP监听器时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止UDP监听器
        /// </summary>
        public void StopListening()
        {
            _networkManager.StopNetworkChecks();
            _udpManager.StopListening();
            Logger.LogInfo("UDP监听器已停止");
        }

        /// <summary>
        /// 检查网络设备状态并在发现新的非虚拟网卡时重新绑定
        /// </summary>
        private void CheckNetworkDevices()
        {
            _networkManager.CheckNetworkDevices(ip => RebindToSpecificInterface(ip));
        }

        /// <summary>
        /// 检查IP地址变化并在检测到变化时重新绑定
        /// </summary>
        private void CheckIpAddressChange()
        {
            _networkManager.CheckIpAddressChange(ip => RebindToSpecificInterface(ip));
        }

        /// <summary>
        /// 重新绑定到特定的网络接口
        /// </summary>
        /// <param name="ipAddress">要绑定到的IP地址</param>
        private void RebindToSpecificInterface(IPAddress ipAddress)
        {
            _networkManager.SetCurrentBoundIp(ipAddress);
            _udpManager.RebindToSpecificInterface(ipAddress, ReceiveCallback);
        }

        /// <summary>
        /// 异步接收数据的回调方法
        /// </summary>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (!_udpManager.IsRunning) return;

                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = _udpManager.EndReceive(ar, ref remoteEndPoint);
                string receivedMessage = Encoding.UTF8.GetString(receivedBytes);

                Logger.LogInfo($"收到来自 {remoteEndPoint} 的UDP消息: {receivedMessage}");

                // 处理接收到的消息
                _commandProcessor.ProcessReceivedMessage(
                    receivedMessage, 
                    remoteEndPoint,
                    ExecuteBatchFile,
                    UpdateBatchFile,
                    SendProcessList,
                    SendAppProcessListWithPaths,
                    TerminateProcess
                );

                // 继续接收下一个数据包
                if (_udpManager.IsRunning)
                {
                    _udpManager.BeginReceive(ReceiveCallback);
                }
            }
            catch (ObjectDisposedException)
            {
                // 当_udpClient被关闭时会抛出此异常，属于正常情况
                Logger.LogInfo("UDP客户端已关闭");
            }
            catch (Exception ex)
            {
                Logger.LogError($"接收UDP数据时发生错误: {ex.Message}");
                if (_udpManager.IsRunning)
                {
                    try
                    {
                        _udpManager.BeginReceive(ReceiveCallback);
                    }
                    catch (Exception ex2)
                    {
                        Logger.LogError($"重新开始接收UDP数据时发生错误: {ex2.Message}");
                    }
                }
            }
        }

        #region 命令处理相关方法

        /// <summary>
        /// 执行现有的批处理文件
        /// </summary>
        private void ExecuteBatchFile()
        {
            var batchFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloaded_command.bat");
            // UDP命令触发的执行需要上传FTP日志，cmd指令指定的代码不需要上传
            // 但对于runcmd命令，应该上传日志，所以isCmdSpecified应为false
            CommandExecutor.ExecuteCommand(batchFilePath, true, true, false);
        }

        /// <summary>
        /// 重新获取批处理文件
        /// </summary>
        private void UpdateBatchFile()
        {
            // URL下载功能已移除
            Logger.LogError("URL下载功能已移除，无法获取新的批处理文件");
            _udpManager.SendResponseToBaseServer("MOT-RC ERR URL下载功能已移除，无法获取新的批处理文件");
        }

        /// <summary>
        /// 获取并发送进程列表到基本UDP服务端
        /// </summary>
        private void SendProcessList()
        {
            try
            {
                // 从ProcessListProvider获取进程列表
                string processList = ProcessListProvider.GetProcessList();

                // 构造响应消息
                string responseMessage = $"MOT-RC process pl {processList}";

                // 发送到基本UDP服务端
                _udpManager.SendResponseToBaseServer(responseMessage);

                Logger.LogInfo("进程列表已发送到基本UDP服务端");
            }
            catch (Exception ex)
            {
                Logger.LogError($"获取或发送进程列表时发生错误: {ex.Message}");
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR 获取进程列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取并发送带路径的应用进程列表到基本UDP服务端
        /// </summary>
        private void SendAppProcessListWithPaths()
        {
            try
            {
                // 从ProcessListProvider获取带路径的应用进程列表
                string appProcessList = ProcessListProvider.GetAppProcessListWithPaths();

                // 构造响应消息
                string responseMessage = $"MOT-RC process {appProcessList}";

                // 发送到基本UDP服务端
                _udpManager.SendResponseToBaseServer(responseMessage);

                Logger.LogInfo("带路径的应用进程列表已发送到基本UDP服务端");
            }
            catch (Exception ex)
            {
                Logger.LogError($"获取或发送应用进程列表时发生错误: {ex.Message}");
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR 获取应用进程列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 终止指定进程并向基本UDP服务端发送结果
        /// </summary>
        /// <param name="processName">要终止的进程名</param>
        private void TerminateProcess(string processName)
        {
            try
            {
                string result = ProcessTerminator.TerminateProcessByName(processName);
                string responseMessage = $"MOT-RC process taskkill result: {result}";
                _udpManager.SendResponseToBaseServer(responseMessage);
                Logger.LogInfo($"进程终止操作完成: {result}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"终止进程 '{processName}' 时发生错误: {ex.Message}";
                Logger.LogError(errorMessage);
                _udpManager.SendResponseToBaseServer($"MOT-RC ERR {errorMessage}");
            }
        }

        #endregion
    }
}