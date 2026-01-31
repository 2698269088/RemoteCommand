using System;
using System.IO;
using System.Threading;

namespace Remote_Command
{
    internal class Program
    {
        private static UdpListener _udpListener;
        private static NetworkStatusMonitor _networkStatusMonitor;
        private static bool _isExiting = false;
        
        public static void Main(string[] args)
        {
            Logger.LogInfo("程序开始运行");
            
            // 注册退出事件处理器
            AppDomain.CurrentDomain.ProcessExit += OnExit;
            
            // 初始化应用程序
            AppInitializer.Initialize();
            
            // 初始化配置管理器
            ConfigManager.Initialize();
            
            // 初始化路径白名单管理器
            PathWhitelistManager.Initialize();
            
            // 启动进程监控
            ProcessMonitor.StartMonitoring();
            
            // 启动网络状态监控
            StartNetworkStatusMonitoring();
            
            // 启动UDP监听器
            StartUdpListening();
            
            // 保持程序运行
            Logger.LogInfo("程序已启动并正在监听UDP消息");
            
            // 保持主线程运行
            while (!_isExiting)
            {
                Thread.Sleep(1000);
            }
        }
        
        /// <summary>
        /// 启动UDP监听
        /// </summary>
        private static void StartUdpListening()
        {
            try
            {
                _udpListener = new UdpListener();
                _udpListener.StartListening();
            }
            catch (Exception ex)
            {
                Logger.LogError($"启动UDP监听时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 启动网络状态监控
        /// </summary>
        private static void StartNetworkStatusMonitoring()
        {
            try
            {
                _networkStatusMonitor = new NetworkStatusMonitor();
                _networkStatusMonitor.StartMonitoring();
            }
            catch (Exception ex)
            {
                Logger.LogError($"启动网络状态监控时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 程序退出事件处理
        /// </summary>
        private static void OnExit(object sender, EventArgs e)
        {
            ExitApplication();
        }
        
        /// <summary>
        /// 退出应用程序
        /// </summary>
        private static void ExitApplication()
        {
            if (!_isExiting)
            {
                _isExiting = true;
                Logger.LogInfo("正在清理资源...");
                _udpListener?.StopListening();
                _networkStatusMonitor?.StopMonitoring();
                ProcessMonitor.StopMonitoring();
                Logger.LogInfo("程序已退出");
            }
        }
    }
}