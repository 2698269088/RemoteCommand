using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Remote_Command
{
    /// <summary>
    /// 路径白名单管理器，用于管理允许运行的目录路径白名单
    /// </summary>
    public static class PathWhitelistManager
    {
        private static readonly HashSet<string> _whitelistPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly string ConfigFilePath = Path.Combine(GetConfigDirectory(), "path_whitelist.json");
        
        /// <summary>
        /// 初始化路径白名单管理器
        /// </summary>
        public static void Initialize()
        {
            LoadWhitelistFromFile();
        }
        
        /// <summary>
        /// 获取配置文件目录路径
        /// </summary>
        /// <returns>配置文件目录路径</returns>
        private static string GetConfigDirectory()
        {
            // 首先尝试在D:\查找配置目录
            string rcDirectoryPath = @"D:\rc";
            if (Directory.Exists(rcDirectoryPath))
            {
                return rcDirectoryPath;
            }
            
            // 如果D:\rc不存在，则使用程序运行目录
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc");
        }
        
        /// <summary>
        /// 从文件加载白名单路径
        /// </summary>
        private static void LoadWhitelistFromFile()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string jsonContent = File.ReadAllText(ConfigFilePath);
                    var paths = ParseJsonArray(jsonContent);
                    if (paths != null)
                    {
                        _whitelistPaths.Clear();
                        foreach (var path in paths)
                        {
                            _whitelistPaths.Add(path);
                        }
                        Logger.LogInfo($"成功从 {ConfigFilePath} 加载路径白名单，共 {_whitelistPaths.Count} 个路径");
                    }
                }
                else
                {
                    Logger.LogInfo($"路径白名单配置文件 {ConfigFilePath} 不存在，将创建新的配置文件");
                    SaveWhitelistToFile();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"加载路径白名单时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 保存白名单路径到文件
        /// </summary>
        private static void SaveWhitelistToFile()
        {
            try
            {
                var pathList = _whitelistPaths.ToList();
                string jsonContent = SerializeToJsonArray(pathList);
                File.WriteAllText(ConfigFilePath, jsonContent);
                Logger.LogInfo($"路径白名单已保存到 {ConfigFilePath}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"保存路径白名单时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解析JSON数组
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>字符串列表</returns>
        private static List<string> ParseJsonArray(string json)
        {
            var result = new List<string>();
            
            // 移除方括号
            json = json.Trim();
            if (json.StartsWith("[") && json.EndsWith("]"))
            {
                json = json.Substring(1, json.Length - 2);
            }
            
            // 分割字符串并去除引号
            if (!string.IsNullOrWhiteSpace(json))
            {
                // 简单的分割方法，适用于我们的场景
                var matches = Regex.Matches(json, "\"([^\"]*)\"");
                foreach (Match match in matches)
                {
                    result.Add(match.Groups[1].Value);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 将字符串列表序列化为JSON数组
        /// </summary>
        /// <param name="list">字符串列表</param>
        /// <returns>JSON字符串</returns>
        private static string SerializeToJsonArray(List<string> list)
        {
            if (list == null || list.Count == 0)
                return "[]";
            
            var escapedItems = list.Select(item => "\"" + item.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
            return "[" + string.Join(",", escapedItems) + "]";
        }
        
        /// <summary>
        /// 添加路径到白名单
        /// </summary>
        /// <param name="path">要添加的路径</param>
        /// <returns>如果成功添加返回true，如果已存在返回false</returns>
        public static bool AddPathToWhitelist(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;
                
                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                if (_whitelistPaths.Add(normalizedPath))
                {
                    SaveWhitelistToFile();
                    Logger.LogInfo($"路径 {normalizedPath} 已添加到路径白名单");
                    return true;
                }
                
                Logger.LogInfo($"路径 {normalizedPath} 已存在于路径白名单中");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"添加路径到白名单时发生错误: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从白名单中移除路径
        /// </summary>
        /// <param name="path">要移除的路径</param>
        /// <returns>如果成功移除返回true，如果不存在返回false</returns>
        public static bool RemovePathFromWhitelist(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;
                
                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                if (_whitelistPaths.Remove(normalizedPath))
                {
                    SaveWhitelistToFile();
                    Logger.LogInfo($"路径 {normalizedPath} 已从路径白名单中移除");
                    return true;
                }
                
                Logger.LogInfo($"路径 {normalizedPath} 不存在于路径白名单中");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"从白名单中移除路径时发生错误: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查路径是否在白名单中
        /// </summary>
        /// <param name="path">要检查的路径</param>
        /// <returns>如果路径在白名单中返回true，否则返回false</returns>
        public static bool IsPathInWhitelist(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;
                
                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                // 直接匹配
                if (_whitelistPaths.Contains(normalizedPath))
                    return true;
                
                // 检查父目录是否在白名单中
                foreach (var whitelistPath in _whitelistPaths)
                {
                    if (IsSubPathOf(whitelistPath, normalizedPath))
                        return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"检查路径是否在白名单中时发生错误: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查child路径是否是parent路径的子路径
        /// </summary>
        /// <param name="parent">父路径</param>
        /// <param name="child">子路径</param>
        /// <returns>如果是子路径返回true，否则返回false</returns>
        private static bool IsSubPathOf(string parent, string child)
        {
            try
            {
                var parentUri = new Uri(parent.EndsWith(Path.DirectorySeparatorChar.ToString()) ? parent : parent + Path.DirectorySeparatorChar);
                var childUri = new Uri(child);
                return parentUri.IsBaseOf(childUri);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 获取所有白名单路径
        /// </summary>
        /// <returns>白名单路径列表</returns>
        public static List<string> GetAllWhitelistPaths()
        {
            return _whitelistPaths.ToList();
        }
        
        /// <summary>
        /// 清空白名单
        /// </summary>
        public static void ClearWhitelist()
        {
            try
            {
                _whitelistPaths.Clear();
                SaveWhitelistToFile();
                Logger.LogInfo("路径白名单已清空");
            }
            catch (Exception ex)
            {
                Logger.LogError($"清空白名单时发生错误: {ex.Message}");
            }
        }
    }
}