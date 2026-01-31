using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Remote_Command
{
    /// <summary>
    /// 网络适配器管理器，负责网络接口管理和IP地址检查
    /// </summary>
    public class NetworkAdapterManager
    {
        private Timer _networkCheckTimer; // 用于定期检查网络设备的定时器
        private Timer _ipAddressCheckTimer; // 用于定期检查IP地址变化的定时器
        private IPAddress _currentBoundIp; // 当前绑定的IP地址
        private static int _noAdapterDetectionCount = 0; // 未检测到非虚拟网卡的计数
        private const int FAILURE_THRESHOLD = 15; // 失败阈值

        // 虚拟网卡名称列表
        private readonly string[] _virtualNetworkAdapters = {
            "VirtualBox Host-Only Network",
            "VMware Network Adapter VMnet1",
            "VMware Network Adapter VMnet8"
        };

        /// <summary>
        /// 获取当前绑定的IP地址
        /// </summary>
        public IPAddress CurrentBoundIp => _currentBoundIp;

        /// <summary>
        /// 设置当前绑定的IP地址
        /// </summary>
        /// <param name="ip">IP地址</param>
        public void SetCurrentBoundIp(IPAddress ip)
        {
            _currentBoundIp = ip;
        }

        /// <summary>
        /// 启动网络设备热更新检查
        /// </summary>
        public void StartNetworkDeviceCheck(TimerCallback callback)
        {
            _networkCheckTimer = new Timer(callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// 启动IP地址变化检查
        /// </summary>
        public void StartIpAddressCheck(TimerCallback callback)
        {
            _ipAddressCheckTimer = new Timer(callback, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// 停止所有网络检查定时器
        /// </summary>
        public void StopNetworkChecks()
        {
            _networkCheckTimer?.Dispose();
            _ipAddressCheckTimer?.Dispose();
        }

        /// <summary>
        /// 检查网络设备状态并在发现新的非虚拟网卡时重新绑定
        /// </summary>
        public void CheckNetworkDevices(Action<IPAddress> rebindAction)
        {
            try
            {
                var localIp = GetNonVirtualLocalIpAddress();
                if (localIp != null)
                {
                    Logger.LogInfo($"发现新的非虚拟网卡IP地址: {localIp}，重新绑定监听");
                    rebindAction?.Invoke(localIp);

                    // 停止定时器，因为我们已经找到了合适的网卡
                    _networkCheckTimer?.Dispose();

                    // 重置计数器
                    _noAdapterDetectionCount = 0;
                    Logger.LogInfo("重置未检测到非虚拟网卡计数器");

                    // 记录当前绑定的IP地址
                    _currentBoundIp = localIp;
                    // 启动IP地址变化检查，每10秒检查一次
                    _ipAddressCheckTimer = new Timer(state => CheckIpAddressChange(rebindAction), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                }
                else
                {
                    // 未检测到非虚拟网卡，增加计数
                    _noAdapterDetectionCount++;
                    Logger.LogWarning($"未检测到非虚拟网卡，这是第 {_noAdapterDetectionCount} 次检测失败");
                    
                    // 尝试启用所有禁用的物理网卡
                    TryEnableDisabledAdapters();
                    
                    // 检查是否达到阈值
                    if (_noAdapterDetectionCount >= FAILURE_THRESHOLD)
                    {
                        Logger.LogError($"未检测到非虚拟网卡已连续 {_noAdapterDetectionCount} 次，达到阈值 {FAILURE_THRESHOLD}，准备重启计算机");
                        RestartComputer();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"检查网络设备时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试启用所有禁用的物理网卡
        /// </summary>
        private void TryEnableDisabledAdapters()
        {
            try
            {
                Logger.LogInfo("尝试启用禁用的物理网卡");
                
                // 获取所有网络接口
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                int enabledCount = 0;
                foreach (NetworkInterface ni in networkInterfaces)
                {
                    // 检查是否为虚拟网卡
                    bool isVirtual = _virtualNetworkAdapters.Any(adapter =>
                        ni.Name.Equals(adapter, StringComparison.OrdinalIgnoreCase) ||
                        ni.Description.IndexOf(adapter, StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    // 如果不是虚拟网卡且处于禁用状态
                    if (!isVirtual && ni.OperationalStatus == OperationalStatus.Down)
                    {
                        Logger.LogInfo($"尝试启用网卡: {ni.Name}");
                        if (EnableNetworkAdapter(ni))
                        {
                            enabledCount++;
                        }
                    }
                }
                
                Logger.LogInfo($"尝试启用了 {enabledCount} 个网卡");
            }
            catch (Exception ex)
            {
                Logger.LogError($"尝试启用禁用的网卡时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用网络适配器
        /// 注意：由于权限限制，普通应用程序通常无法直接启用网络适配器
        /// 这里提供一种通过netsh命令的方式
        /// </summary>
        /// <param name="nic">网络接口</param>
        /// <returns>启用是否成功</returns>
        private bool EnableNetworkAdapter(NetworkInterface nic)
        {
            try
            {
                // 尝试通过netsh命令启用网络适配器
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface set interface \"{nic.Name}\" admin=enabled",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Logger.LogInfo($"成功启用网卡 {nic.Name}");
                    return true;
                }
                else
                {
                    Logger.LogWarning($"尝试启用网卡 {nic.Name} 失败，可能需要管理员权限");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"启用网卡 {nic.Name} 时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查IP地址变化并在检测到变化时重新绑定
        /// </summary>
        public void CheckIpAddressChange(Action<IPAddress> rebindAction)
        {
            try
            {
                var currentNonVirtualIp = GetNonVirtualLocalIpAddress();

                // 如果当前绑定的是特定IP地址而不是IPAddress.Any，并且IP地址发生了变化
                if (_currentBoundIp != null &&
                    !_currentBoundIp.Equals(IPAddress.Any) &&
                    !Equals(_currentBoundIp, currentNonVirtualIp))
                {
                    if (currentNonVirtualIp != null)
                    {
                        Logger.LogInfo($"检测到IP地址发生变化，从 {_currentBoundIp} 变更为 {currentNonVirtualIp}，重新绑定监听");
                        rebindAction?.Invoke(currentNonVirtualIp);
                        _currentBoundIp = currentNonVirtualIp;
                    }
                    else
                    {
                        Logger.LogWarning("检测到当前非虚拟网卡IP地址不可用，切换到监听所有网络接口");
                        rebindAction?.Invoke(IPAddress.Any);
                        _currentBoundIp = IPAddress.Any;

                        // 启动网络设备热更新检查，每5秒检查一次
                        _networkCheckTimer?.Dispose();
                        _networkCheckTimer = new Timer(state => CheckNetworkDevices(rebindAction), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
                        _ipAddressCheckTimer?.Dispose();
                        
                        // 重置计数器
                        _noAdapterDetectionCount = 0;
                        Logger.LogInfo("重置未检测到非虚拟网卡计数器");
                    }
                }
                // 如果当前绑定的是IPAddress.Any但发现了有效的非虚拟网卡IP地址
                else if (_currentBoundIp != null &&
                         _currentBoundIp.Equals(IPAddress.Any) &&
                         currentNonVirtualIp != null)
                {
                    Logger.LogInfo($"检测到有效的非虚拟网卡IP地址: {currentNonVirtualIp}，重新绑定监听");
                    rebindAction?.Invoke(currentNonVirtualIp);
                    _currentBoundIp = currentNonVirtualIp;

                    // 停止网络设备热更新检查
                    _networkCheckTimer?.Dispose();

                    // 启动IP地址变化检查，每10秒检查一次
                    _ipAddressCheckTimer?.Dispose();
                    _ipAddressCheckTimer = new Timer(state => CheckIpAddressChange(rebindAction), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                    
                    // 重置计数器
                    _noAdapterDetectionCount = 0;
                    Logger.LogInfo("重置未检测到非虚拟网卡计数器");
                }
                // 如果当前没有有效的IP地址
                else if (currentNonVirtualIp == null && _currentBoundIp != null && !_currentBoundIp.Equals(IPAddress.Any))
                {
                    Logger.LogWarning("检测到当前非虚拟网卡IP地址不可用，切换到监听所有网络接口");
                    rebindAction?.Invoke(IPAddress.Any);
                    _currentBoundIp = IPAddress.Any;

                    // 启动网络设备热更新检查，每5秒检查一次
                    _networkCheckTimer?.Dispose();
                    _networkCheckTimer = new Timer(state => CheckNetworkDevices(rebindAction), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
                    _ipAddressCheckTimer?.Dispose();
                    
                    // 重置计数器
                    _noAdapterDetectionCount = 0;
                    Logger.LogInfo("重置未检测到非虚拟网卡计数器");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"检查IP地址变化时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取非虚拟网卡的本地IP地址
        /// </summary>
        /// <returns>非虚拟网卡的IPv4地址，如果没有找到则返回null</returns>
        public IPAddress GetNonVirtualLocalIpAddress()
        {
            try
            {
                // 获取所有网络接口
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface ni in networkInterfaces)
                {
                    // 检查是否为虚拟网卡
                    bool isVirtual = _virtualNetworkAdapters.Any(adapter =>
                        ni.Name.Equals(adapter, StringComparison.OrdinalIgnoreCase) ||
                        ni.Description.IndexOf(adapter, StringComparison.OrdinalIgnoreCase) >= 0);

                    // 如果不是虚拟网卡且处于活动状态
                    if (!isVirtual && ni.OperationalStatus == OperationalStatus.Up)
                    {
                        IPInterfaceProperties properties = ni.GetIPProperties();

                        // 查找IPv4地址
                        foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(ip.Address))
                            {
                                Logger.LogInfo($"找到非虚拟网卡IP地址: {ip.Address} ({ni.Name})");
                                return ip.Address;
                            }
                        }
                    }
                }

                // 如果没有找到非虚拟网卡，则返回null，迫使用户解决网络问题
                Logger.LogInfo("未找到非虚拟网卡");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"获取本地IP地址时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 重启计算机
        /// </summary>
        private void RestartComputer()
        {
            try
            {
                Logger.LogInfo("正在重启计算机...");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 0",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Logger.LogError($"重启计算机时发生错误: {ex.Message}");
            }
        }
    }
}