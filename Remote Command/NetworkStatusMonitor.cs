using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace Remote_Command
{
    /// <summary>
    /// 网络状态监控类，用于检测网线拔出和网卡禁用状态
    /// </summary>
    public class NetworkStatusMonitor
    {
        private System.Threading.Timer _networkStatusTimer;
        private bool _cableDisconnectedWarningShown = false;
        private Dictionary<string, int> _adapterFailureCount = new Dictionary<string, int>();
        private readonly string[] _virtualNetworkAdapters = {
            "VirtualBox Host-Only Network",
            "VMware Network Adapter VMnet1",
            "VMware Network Adapter VMnet8"
        };

        /// <summary>
        /// 启动网络状态监控
        /// </summary>
        public void StartMonitoring()
        {
            // 启动定时器，每5秒检查一次网络状态
            _networkStatusTimer = new System.Threading.Timer(CheckNetworkStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            Logger.LogInfo("网络状态监控已启动");
        }

        /// <summary>
        /// 停止网络状态监控
        /// </summary>
        public void StopMonitoring()
        {
            _networkStatusTimer?.Dispose();
            Logger.LogInfo("网络状态监控已停止");
        }

        /// <summary>
        /// 检查网络状态（网线连接状态和网卡启用状态）
        /// </summary>
        private void CheckNetworkStatus(object state)
        {
            // 检查网卡检测功能是否启用
            if (!ConfigManager.GetNicDetectionEnabled())
            {
                Logger.LogInfo("网卡检测功能已禁用，跳过网络状态检查");
                return;
            }
            
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

                    // 如果不是虚拟网卡
                    if (!isVirtual)
                    {
                        // 检查网线连接状态
                        CheckCableStatus(ni);
                        
                        // 检查网卡是否被禁用
                        CheckAdapterStatus(ni);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"检查网络状态时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查网线连接状态
        /// </summary>
        /// <param name="nic">网络接口</param>
        private void CheckCableStatus(NetworkInterface nic)
        {
            // 检查网卡检测功能是否启用
            if (!ConfigManager.GetNicDetectionEnabled())
            {
                return;
            }
            
            try
            {
                // 只对以太网适配器进行网线连接检查
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    // 检查网络接口是否处于活动状态
                    if (nic.OperationalStatus == OperationalStatus.Up)
                    {
                        // 检查网络接口的速度，如果速度为0，可能表示没有连接
                        if (nic.Speed == 0 && !_cableDisconnectedWarningShown)
                        {
                            // 显示警告并计划重启
                            ShowCableDisconnectedWarning();
                            _cableDisconnectedWarningShown = true;
                        }
                        else if (nic.Speed > 0)
                        {
                            // 如果速度大于0，重置警告标志
                            _cableDisconnectedWarningShown = false;
                        }
                    }
                    // 如果网卡处于断开状态但不是因为禁用（比如网线拔出）
                    else if (nic.OperationalStatus == OperationalStatus.Down)
                    {
                        // 检查是否是网线拔出而不是网卡被禁用
                        // 我们可以通过检查是否有物理地址来判断
                        if (!string.IsNullOrEmpty(nic.GetPhysicalAddress().ToString()))
                        {
                            Logger.LogInfo($"检测到网卡 {nic.Name} 网线可能被拔出");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"检查网线连接状态时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查网卡启用状态
        /// </summary>
        /// <param name="nic">网络接口</param>
        private void CheckAdapterStatus(NetworkInterface nic)
        {
            // 检查网卡检测功能是否启用
            if (!ConfigManager.GetNicDetectionEnabled())
            {
                return;
            }
            
            try
            {
                // 如果网卡被禁用，则尝试启用它
                if (nic.OperationalStatus == OperationalStatus.Down)
                {
                    // 检查是否是因为网线拔出导致的Down状态
                    if (string.IsNullOrEmpty(nic.GetPhysicalAddress().ToString()))
                    {
                        Logger.LogInfo($"检测到网卡 {nic.Name} 被禁用，尝试启用...");
                        bool enableSuccess = EnableNetworkAdapter(nic);
                        
                        // 更新失败计数
                        if (!_adapterFailureCount.ContainsKey(nic.Name))
                        {
                            _adapterFailureCount[nic.Name] = 0;
                        }
                        
                        if (!enableSuccess)
                        {
                            _adapterFailureCount[nic.Name]++;
                            Logger.LogWarning($"启用网卡 {nic.Name} 失败，失败次数: {_adapterFailureCount[nic.Name]}");
                            
                            // 如果失败次数超过15次，则显示重启对话框
                            if (_adapterFailureCount[nic.Name] >= 15)
                            {
                                ShowRestartDialog(nic.Name);
                                _adapterFailureCount[nic.Name] = 0; // 重置计数
                            }
                        }
                        else
                        {
                            // 成功启用后重置失败计数
                            _adapterFailureCount[nic.Name] = 0;
                            Logger.LogInfo($"成功启用网卡 {nic.Name}，重置失败计数");
                        }
                    }
                    else
                    {
                        Logger.LogInfo($"检测到网卡 {nic.Name} 可能因网线断开而断开连接");
                    }
                }
                else
                {
                    // 如果网卡正常工作，重置失败计数
                    if (_adapterFailureCount.ContainsKey(nic.Name))
                    {
                        _adapterFailureCount[nic.Name] = 0;
                        Logger.LogInfo($"网卡 {nic.Name} 正常工作，重置失败计数");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"检查网卡启用状态时发生错误: {ex.Message}");
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
            // 检查网卡检测功能是否启用
            if (!ConfigManager.GetNicDetectionEnabled())
            {
                Logger.LogInfo("网卡检测功能已禁用，跳过网卡启用操作");
                return false;
            }
            
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
        /// 显示网线断开警告并计划重启
        /// </summary>
        private void ShowCableDisconnectedWarning()
        {
            try
            {
                Logger.LogInfo("检测到网线断开，显示警告对话框");

                // 在新线程中显示消息框，避免阻塞监控线程
                Thread warningThread = new Thread(() =>
                {
                    try
                    {
                        DialogResult result = MessageBox.Show(
                            "检测到网线已被拔出，将在30秒后重启计算机。\n\n点击【确定】立即重启，点击【取消】取消重启。",
                            "网络连接警告",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.OK)
                        {
                            // 立即重启
                            RestartComputer();
                        }
                        else
                        {
                            // 用户取消重启，等待30秒后重启
                            Thread.Sleep(TimeSpan.FromSeconds(30));
                            RestartComputer();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"显示警告对话框时发生错误: {ex.Message}");
                    }
                });

                warningThread.SetApartmentState(ApartmentState.STA); // 设置线程单元状态以支持WinForms控件
                warningThread.IsBackground = true;
                warningThread.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError($"显示网线断开警告时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示重启对话框（当网卡连续启用失败时）
        /// </summary>
        /// <param name="adapterName">网卡名称</param>
        private void ShowRestartDialog(string adapterName)
        {
            try
            {
                Logger.LogInfo($"网卡 {adapterName} 连续启用失败超过15次，显示重启对话框");

                // 在新线程中显示消息框，避免阻塞监控线程
                Thread restartThread = new Thread(() =>
                {
                    try
                    {
                        DialogResult result = MessageBox.Show(
                            $"网卡 {adapterName} 连续启用失败超过15次，系统可能存在问题。\n\n点击【确定】立即重启计算机，点击【取消】继续运行。",
                            "网络适配器故障",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Error);

                        if (result == DialogResult.OK)
                        {
                            // 立即重启
                            RestartComputer();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"显示重启对话框时发生错误: {ex.Message}");
                    }
                });

                restartThread.SetApartmentState(ApartmentState.STA); // 设置线程单元状态以支持WinForms控件
                restartThread.IsBackground = true;
                restartThread.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError($"显示重启对话框时发生错误: {ex.Message}");
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