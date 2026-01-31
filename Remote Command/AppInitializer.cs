using System;
using System.IO;
using System.Net;

namespace Remote_Command
{
    /// <summary>
    /// 应用程序初始化类，负责创建必要的目录和文件
    /// </summary>
    public static class AppInitializer
    {
        /// <summary>
        /// 初始化应用程序所需的目录和文件
        /// </summary>
        public static void Initialize()
        {
            try
            {
                string rcDirectoryPath = @"D:\rc";
                string applistFilePath = Path.Combine(rcDirectoryPath, "applist.json");
                
                // 尝试在D:\创建隐藏目录
                if (CreateHiddenDirectory(rcDirectoryPath))
                {
                    // 创建applist.json文件
                    CreateEmptyJsonFile(applistFilePath);
                    
                    // 创建UDP配置文件
                    string udpConfigFilePath = Path.Combine(rcDirectoryPath, "udp_config.json");
                    CreateDefaultUdpConfigFile(udpConfigFilePath);
                    
                    Logger.LogInfo($"成功在D:\\创建隐藏目录rc和相关文件");
                }
                else
                {
                    // 如果无法在D:\创建，则在程序运行目录创建
                    string localRcDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc");
                    string localApplistFilePath = Path.Combine(localRcDirectoryPath, "applist.json");
                    
                    if (CreateHiddenDirectory(localRcDirectoryPath))
                    {
                        CreateEmptyJsonFile(localApplistFilePath);
                        
                        // 创建UDP配置文件
                        string localUdpConfigFilePath = Path.Combine(localRcDirectoryPath, "udp_config.json");
                        CreateDefaultUdpConfigFile(localUdpConfigFilePath);
                        
                        Logger.LogInfo($"成功在程序运行目录创建隐藏目录rc和相关文件");
                    }
                    else
                    {
                        Logger.LogError("无法创建隐藏目录rc");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"初始化应用程序时发生错误: {ex.Message}");
                
                // 出现异常时，在程序运行目录创建
                try
                {
                    string localRcDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc");
                    string localApplistFilePath = Path.Combine(localRcDirectoryPath, "applist.json");
                    
                    if (CreateHiddenDirectory(localRcDirectoryPath))
                    {
                        CreateEmptyJsonFile(localApplistFilePath);
                        
                        // 创建UDP配置文件
                        string localUdpConfigFilePath = Path.Combine(localRcDirectoryPath, "udp_config.json");
                        CreateDefaultUdpConfigFile(localUdpConfigFilePath);
                        
                        Logger.LogInfo($"异常处理：成功在程序运行目录创建隐藏目录rc和相关文件");
                    }
                }
                catch (Exception fallbackEx)
                {
                    Logger.LogError($"备用初始化方案也失败了: {fallbackEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// 创建隐藏目录
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>创建是否成功</returns>
        private static bool CreateHiddenDirectory(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    DirectoryInfo di = Directory.CreateDirectory(directoryPath);
                    // 设置为隐藏属性
                    di.Attributes |= FileAttributes.Hidden;
                }
                else
                {
                    // 如果目录已存在，确保它是隐藏的
                    DirectoryInfo di = new DirectoryInfo(directoryPath);
                    di.Attributes |= FileAttributes.Hidden;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"创建隐藏目录 {directoryPath} 失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 创建空的JSON文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private static void CreateEmptyJsonFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "{}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"创建文件 {filePath} 失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建默认的UDP配置文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private static void CreateDefaultUdpConfigFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    string defaultConfig = "{\n  \"serverIp\": null,\n  \"useWhitelist\": false\n}";
                    File.WriteAllText(filePath, defaultConfig);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"创建UDP配置文件 {filePath} 失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 读取UDP服务器地址配置
        /// </summary>
        /// <returns>服务器IP地址，如果未配置则返回null</returns>
        public static IPAddress ReadUdpServerIp()
        {
            try
            {
                // 首先尝试从D:\rc\读取
                string udpConfigFilePath = Path.Combine(@"D:\rc", "udp_config.json");
                if (!File.Exists(udpConfigFilePath))
                {
                    // 如果D:\rc\不存在，则尝试从程序运行目录读取
                    udpConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc", "udp_config.json");
                    if (!File.Exists(udpConfigFilePath))
                    {
                        Logger.LogInfo("未找到UDP配置文件");
                        return null;
                    }
                }
                
                string configContent = File.ReadAllText(udpConfigFilePath);
                // 简单解析JSON获取IP地址
                string ipString = ExtractJsonValue(configContent, "serverIp");
                
                if (!string.IsNullOrEmpty(ipString) && IPAddress.TryParse(ipString, out IPAddress ip))
                {
                    Logger.LogInfo($"从配置文件读取到UDP服务器地址: {ip}");
                    return ip;
                }
                
                Logger.LogInfo("配置文件中未设置有效的UDP服务器地址");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"读取UDP服务器地址配置时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 保存UDP服务器地址配置
        /// </summary>
        /// <param name="ipAddress">要保存的IP地址</param>
        public static void SaveUdpServerIp(IPAddress ipAddress)
        {
            try
            {
                // 首先尝试保存到D:\rc\
                string udpConfigFilePath = Path.Combine(@"D:\rc", "udp_config.json");
                if (!File.Exists(udpConfigFilePath))
                {
                    // 如果D:\rc\不存在，则保存到程序运行目录
                    udpConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc", "udp_config.json");
                    if (!File.Exists(udpConfigFilePath))
                    {
                        Logger.LogError("无法找到UDP配置文件路径");
                        return;
                    }
                }
                
                string configContent = File.ReadAllText(udpConfigFilePath);
                string updatedConfig = UpdateJsonValue(configContent, "serverIp", ipAddress.ToString());
                File.WriteAllText(udpConfigFilePath, updatedConfig);
                
                Logger.LogInfo($"UDP服务器地址已保存到配置文件: {ipAddress}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"保存UDP服务器地址配置时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 读取是否使用白名单配置
        /// </summary>
        /// <returns>是否使用白名单，如果未配置则返回false</returns>
        public static bool ReadUseWhitelist()
        {
            try
            {
                // 首先尝试从D:\rc\读取
                string udpConfigFilePath = Path.Combine(@"D:\rc", "udp_config.json");
                if (!File.Exists(udpConfigFilePath))
                {
                    // 如果D:\rc\不存在，则尝试从程序运行目录读取
                    udpConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc", "udp_config.json");
                    if (!File.Exists(udpConfigFilePath))
                    {
                        Logger.LogInfo("未找到UDP配置文件");
                        return false;
                    }
                }
                
                string configContent = File.ReadAllText(udpConfigFilePath);
                // 简单解析JSON获取useWhitelist值
                string useWhitelistStr = ExtractJsonValue(configContent, "useWhitelist");
                
                if (bool.TryParse(useWhitelistStr, out bool useWhitelist))
                {
                    Logger.LogInfo($"从配置文件读取到使用白名单标志: {useWhitelist}");
                    return useWhitelist;
                }
                
                Logger.LogInfo("配置文件中未设置有效的使用白名单标志，默认使用黑名单");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"读取使用白名单配置时发生错误: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 保存是否使用白名单配置
        /// </summary>
        /// <param name="useWhitelist">是否使用白名单</param>
        public static void SaveUseWhitelist(bool useWhitelist)
        {
            try
            {
                // 首先尝试保存到D:\rc\
                string udpConfigFilePath = Path.Combine(@"D:\rc", "udp_config.json");
                if (!File.Exists(udpConfigFilePath))
                {
                    // 如果D:\rc\不存在，则保存到程序运行目录
                    udpConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc", "udp_config.json");
                    if (!File.Exists(udpConfigFilePath))
                    {
                        Logger.LogError("无法找到UDP配置文件路径");
                        return;
                    }
                }
                
                string configContent = File.ReadAllText(udpConfigFilePath);
                string updatedConfig = UpdateJsonValue(configContent, "useWhitelist", useWhitelist.ToString().ToLower());
                File.WriteAllText(udpConfigFilePath, updatedConfig);
                
                Logger.LogInfo($"使用白名单标志已保存到配置文件: {useWhitelist}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"保存使用白名单配置时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从JSON字符串中提取指定键的值
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="key">键名</param>
        /// <returns>键值</returns>
        private static string ExtractJsonValue(string json, string key)
        {
            try
            {
                // 简单的JSON值提取实现
                string searchString = $"\"{key}\":";
                int startIndex = json.IndexOf(searchString);
                if (startIndex == -1)
                    return null;
                
                startIndex += searchString.Length;
                // 跳过空白字符
                while (startIndex < json.Length && char.IsWhiteSpace(json[startIndex]))
                    startIndex++;
                
                if (startIndex >= json.Length)
                    return null;
                
                // 处理字符串值
                if (json[startIndex] == '"')
                {
                    startIndex++; // 跳过开始引号
                    int endIndex = json.IndexOf('"', startIndex);
                    if (endIndex == -1)
                        return null;
                    return json.Substring(startIndex, endIndex - startIndex);
                }
                else if (json[startIndex] == 'n' && 
                         startIndex + 4 <= json.Length && 
                         json.Substring(startIndex, 4) == "null")
                {
                    return null;
                }
                else if (json[startIndex] == 't' && 
                         startIndex + 4 <= json.Length && 
                         json.Substring(startIndex, 4) == "true")
                {
                    return "true";
                }
                else if (json[startIndex] == 'f' && 
                         startIndex + 5 <= json.Length && 
                         json.Substring(startIndex, 5) == "false")
                {
                    return "false";
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 更新JSON字符串中指定键的值
        /// </summary>
        /// <param name="json">原始JSON字符串</param>
        /// <param name="key">键名</param>
        /// <param name="value">新值</param>
        /// <returns>更新后的JSON字符串</returns>
        private static string UpdateJsonValue(string json, string key, string value)
        {
            try
            {
                string searchString = $"\"{key}\":";
                int startIndex = json.IndexOf(searchString);
                if (startIndex == -1)
                {
                    // 如果找不到键，简单地添加它（非常基础的实现）
                    int insertPos = json.LastIndexOf("}");
                    if (insertPos != -1)
                    {
                        string separator = json.Contains(":") ? "," : ""; // 如果已有内容需要逗号分隔
                        return json.Insert(insertPos, $"{separator}\n  \"{key}\": \"{value}\"\n");
                    }
                    return json;
                }
                
                startIndex += searchString.Length;
                // 跳过空白字符
                while (startIndex < json.Length && char.IsWhiteSpace(json[startIndex]))
                    startIndex++;
                
                if (startIndex >= json.Length)
                    return json;
                
                int valueStartIndex = startIndex;
                int valueEndIndex;
                
                // 处理字符串值
                if (json[startIndex] == '"')
                {
                    startIndex++; // 跳过开始引号
                    valueEndIndex = json.IndexOf('"', startIndex);
                    if (valueEndIndex == -1)
                        return json;
                    valueEndIndex++; // 包含结束引号
                }
                else if (json[startIndex] == 'n' && 
                         startIndex + 4 <= json.Length && 
                         json.Substring(startIndex, 4) == "null")
                {
                    valueEndIndex = startIndex + 4;
                }
                else if (json[startIndex] == 't' && 
                         startIndex + 4 <= json.Length && 
                         json.Substring(startIndex, 4) == "true")
                {
                    valueEndIndex = startIndex + 4;
                }
                else if (json[startIndex] == 'f' && 
                         startIndex + 5 <= json.Length && 
                         json.Substring(startIndex, 5) == "false")
                {
                    valueEndIndex = startIndex + 5;
                }
                else
                {
                    return json;
                }
                
                // 替换值
                string newValue = value.StartsWith("\"") ? value : $"\"{value}\"";
                return json.Substring(0, valueStartIndex) + newValue + json.Substring(valueEndIndex);
            }
            catch
            {
                return json;
            }
        }
    }
}