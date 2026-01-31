using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Remote_Command
{
    /// <summary>
    /// 配置管理类，用于处理应用程序的黑名单和白名单
    /// </summary>
    public static class ConfigManager
    {
        private static List<string> _blacklist = new List<string>();
        private static List<string> _whitelist = new List<string>();
        private static string _configFilePath;
        private static bool _useWhitelist = false;
        private static bool _usePathWhitelist = false; // 是否使用路径白名单
        
        // 添加事件，当黑白名单发生变化时通知其他组件
        public static event Action ConfigChanged;
        
        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // 确定配置文件路径
                string rcDirectoryPath = @"D:\rc";
                if (Directory.Exists(rcDirectoryPath))
                {
                    _configFilePath = Path.Combine(rcDirectoryPath, "app_config.txt");
                }
                else
                {
                    _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc", "app_config.txt");
                }
                
                // 加载配置
                LoadConfig();
                
                // 从UDP配置文件中读取useWhitelist标志
                _useWhitelist = AppInitializer.ReadUseWhitelist();
                
                // 订阅配置变化事件
                ConfigChanged += () => {
                    // 当配置变化时，立即触发一次进程检查
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => {
                        System.Threading.Thread.Sleep(1000); // 等待1秒，确保进程已完全启动
                        ProcessMonitor.CheckProcessesOnce();
                    });
                };
            }
            catch (Exception ex)
            {
                Logger.LogError($"初始化配置管理器时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载配置文件
        /// </summary>
        private static void LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string[] lines = File.ReadAllLines(_configFilePath);
                    _blacklist.Clear();
                    _whitelist.Clear();
                    
                    bool readingBlacklist = false;
                    bool readingWhitelist = false;
                    
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine))
                            continue;
                            
                        if (trimmedLine == "[BLACKLIST]")
                        {
                            readingBlacklist = true;
                            readingWhitelist = false;
                            continue;
                        }
                        else if (trimmedLine == "[WHITELIST]")
                        {
                            readingBlacklist = false;
                            readingWhitelist = true;
                            continue;
                        }
                        
                        if (readingBlacklist)
                        {
                            _blacklist.Add(trimmedLine);
                        }
                        else if (readingWhitelist)
                        {
                            _whitelist.Add(trimmedLine);
                        }
                    }
                    
                    Logger.LogInfo("成功加载应用程序配置");
                }
                else
                {
                    // 如果配置文件不存在，创建默认配置
                    SaveConfig();
                    Logger.LogInfo("创建默认应用程序配置文件");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"加载配置文件时发生错误: {ex.Message}");
                // 使用默认空列表
                _blacklist = new List<string>();
                _whitelist = new List<string>();
            }
        }
        
        /// <summary>
        /// 保存配置到文件
        /// </summary>
        private static void SaveConfig()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("[BLACKLIST]");
                lines.AddRange(_blacklist);
                lines.Add("");
                lines.Add("[WHITELIST]");
                lines.AddRange(_whitelist);
                
                File.WriteAllLines(_configFilePath, lines);
            }
            catch (Exception ex)
            {
                Logger.LogError($"保存配置文件时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取黑名单列表
        /// </summary>
        /// <returns>黑名单列表的副本</returns>
        public static List<string> GetBlacklist()
        {
            return new List<string>(_blacklist);
        }
        
        /// <summary>
        /// 获取白名单列表
        /// </summary>
        /// <returns>白名单列表的副本</returns>
        public static List<string> GetWhitelist()
        {
            return new List<string>(_whitelist);
        }
        
        /// <summary>
        /// 添加应用程序到黑名单
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        public static void AddToBlacklist(string appName)
        {
            if (!_blacklist.Contains(appName, StringComparer.OrdinalIgnoreCase))
            {
                _blacklist.Add(appName);
                SaveConfig();
                Logger.LogInfo($"应用程序 '{appName}' 已添加到黑名单");
                
                // 触发配置变更事件
                ConfigChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 从黑名单中移除应用程序
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        public static void RemoveFromBlacklist(string appName)
        {
            bool removed = _blacklist.RemoveAll(x => x.Equals(appName, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                SaveConfig();
                Logger.LogInfo($"应用程序 '{appName}' 已从黑名单中移除");
                
                // 触发配置变更事件
                ConfigChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 添加应用程序到白名单
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        public static void AddToWhitelist(string appName)
        {
            if (!_whitelist.Contains(appName, StringComparer.OrdinalIgnoreCase))
            {
                _whitelist.Add(appName);
                SaveConfig();
                Logger.LogInfo($"应用程序 '{appName}' 已添加到白名单");
                
                // 触发配置变更事件
                ConfigChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 从白名单中移除应用程序
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        public static void RemoveFromWhitelist(string appName)
        {
            bool removed = _whitelist.RemoveAll(x => x.Equals(appName, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                SaveConfig();
                Logger.LogInfo($"应用程序 '{appName}' 已从白名单中移除");
                
                // 触发配置变更事件
                ConfigChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 检查应用程序是否在黑名单中
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        /// <returns>如果在黑名单中返回true，否则返回false</returns>
        public static bool IsInBlacklist(string appName)
        {
            return _blacklist.Contains(appName, StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 检查应用程序是否在白名单中
        /// </summary>
        /// <param name="appName">应用程序名称</param>
        /// <returns>如果在白名单中返回true，否则返回false</returns>
        public static bool IsInWhitelist(string appName)
        {
            return _whitelist.Contains(appName, StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 设置是否使用白名单模式
        /// </summary>
        /// <param name="useWhitelist">true表示使用白名单，false表示使用黑名单</param>
        public static void SetUseWhitelist(bool useWhitelist)
        {
            _useWhitelist = useWhitelist;
            AppInitializer.SaveUseWhitelist(useWhitelist);
            Logger.LogInfo($"设置使用白名单模式: {useWhitelist}");
        }
        
        /// <summary>
        /// 获取是否使用白名单模式
        /// </summary>
        /// <returns>true表示使用白名单，false表示使用黑名单</returns>
        public static bool GetUseWhitelist()
        {
            return _useWhitelist;
        }
        
        /// <summary>
        /// 设置是否使用路径白名单模式
        /// </summary>
        /// <param name="usePathWhitelist">true表示使用路径白名单，false表示使用路径黑名单</param>
        public static void SetUsePathWhitelist(bool usePathWhitelist)
        {
            _usePathWhitelist = usePathWhitelist;
            // 这里可以考虑保存到配置文件，但目前我们只在运行时使用
            Logger.LogInfo($"设置使用路径白名单模式: {usePathWhitelist}");
        }
        
        /// <summary>
        /// 获取是否使用路径白名单模式
        /// </summary>
        /// <returns>true表示使用路径白名单，false表示使用路径黑名单</returns>
        public static bool GetUsePathWhitelist()
        {
            return _usePathWhitelist;
        }
    }
}