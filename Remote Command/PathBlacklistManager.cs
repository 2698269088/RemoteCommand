using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Remote_Command
{
    /// <summary>
    /// 路径黑名单管理类，用于处理路径黑名单的添加、移除和重复检查
    /// </summary>
    public static class PathBlacklistManager
    {
        private static List<string> _pathBlacklist = new List<string>();
        
        /// <summary>
        /// 初始化路径黑名单管理器
        /// </summary>
        static PathBlacklistManager()
        {
            LoadPathBlacklist();
        }
        
        /// <summary>
        /// 加载路径黑名单配置
        /// </summary>
        private static void LoadPathBlacklist()
        {
            try
            {
                string configFilePath = GetConfigFilePath();
                if (File.Exists(configFilePath))
                {
                    string[] lines = File.ReadAllLines(configFilePath);
                    _pathBlacklist.Clear();
                    
                    bool readingPathBlacklist = false;
                    
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine))
                            continue;
                            
                        if (trimmedLine == "[PATH_BLACKLIST]")
                        {
                            readingPathBlacklist = true;
                            continue;
                        }
                        else if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                        {
                            // 遇到其他节，停止读取路径黑名单
                            readingPathBlacklist = false;
                        }
                        
                        if (readingPathBlacklist)
                        {
                            string normalizedPath = NormalizePath(trimmedLine);
                            if (!string.IsNullOrEmpty(normalizedPath) && !_pathBlacklist.Contains(normalizedPath, StringComparer.OrdinalIgnoreCase))
                            {
                                _pathBlacklist.Add(normalizedPath);
                            }
                        }
                    }
                    
                    Logger.LogInfo("成功加载路径黑名单配置");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"加载路径黑名单配置时发生错误: {ex.Message}");
                _pathBlacklist = new List<string>();
            }
        }
        
        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        /// <returns>配置文件路径</returns>
        private static string GetConfigFilePath()
        {
            string rcDirectoryPath = @"D:\rc";
            if (Directory.Exists(rcDirectoryPath))
            {
                return Path.Combine(rcDirectoryPath, "app_config.txt");
            }
            else
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rc", "app_config.txt");
            }
        }
        
        /// <summary>
        /// 保存路径黑名单配置
        /// </summary>
        private static void SavePathBlacklist()
        {
            try
            {
                string configFilePath = GetConfigFilePath();
                
                // 读取现有配置文件内容
                List<string> allLines = new List<string>();
                if (File.Exists(configFilePath))
                {
                    allLines.AddRange(File.ReadAllLines(configFilePath));
                }
                
                // 查找或创建[PATH_BLACKLIST]节
                int startIndex = allLines.FindIndex(line => line.Trim() == "[PATH_BLACKLIST]");
                if (startIndex == -1)
                {
                    // 如果没有找到节，添加节标题
                    if (allLines.Count > 0 && !string.IsNullOrWhiteSpace(allLines[allLines.Count - 1]))
                    {
                        allLines.Add(""); // 添加空行分隔
                    }
                    allLines.Add("[PATH_BLACKLIST]");
                    startIndex = allLines.Count - 1;
                }
                
                // 查找节结束位置（下一个节或文件末尾）
                int endIndex = allLines.FindIndex(startIndex + 1, line => line.Trim().StartsWith("[") && line.Trim().EndsWith("]"));
                if (endIndex == -1)
                {
                    endIndex = allLines.Count;
                }
                
                // 移除旧的路径黑名单条目
                allLines.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
                
                // 插入新的路径黑名单条目
                allLines.InsertRange(startIndex + 1, _pathBlacklist);
                
                // 保存文件
                File.WriteAllLines(configFilePath, allLines);
            }
            catch (Exception ex)
            {
                Logger.LogError($"保存路径黑名单配置时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 标准化路径格式
        /// </summary>
        /// <param name="path">原始路径</param>
        /// <returns>标准化后的路径</returns>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
                
            try
            {
                return Path.GetFullPath(path).TrimEnd('\\');
            }
            catch
            {
                return path.TrimEnd('\\');
            }
        }
        
        /// <summary>
        /// 检查路径是否已经在黑名单中
        /// </summary>
        /// <param name="path">要检查的路径</param>
        /// <returns>如果路径已存在返回true，否则返回false</returns>
        public static bool IsPathInBlacklist(string path)
        {
            string normalizedPath = NormalizePath(path);
            return _pathBlacklist.Contains(normalizedPath, StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 添加路径到路径黑名单（自动去重）
        /// </summary>
        /// <param name="path">要添加的路径</param>
        /// <returns>如果成功添加返回true，如果已存在返回false</returns>
        public static bool AddPathToBlacklist(string path)
        {
            string normalizedPath = NormalizePath(path);
            
            // 检查路径是否已存在
            if (_pathBlacklist.Contains(normalizedPath, StringComparer.OrdinalIgnoreCase))
            {
                Logger.LogInfo($"路径 '{normalizedPath}' 已存在于路径黑名单中");
                return false;
            }
            
            _pathBlacklist.Add(normalizedPath);
            SavePathBlacklist();
            Logger.LogInfo($"路径 '{normalizedPath}' 已添加到路径黑名单");
            return true;
        }
        
        /// <summary>
        /// 从路径黑名单中移除路径
        /// </summary>
        /// <param name="path">要移除的路径</param>
        /// <returns>如果成功移除返回true，如果路径不存在返回false</returns>
        public static bool RemovePathFromBlacklist(string path)
        {
            string normalizedPath = NormalizePath(path);
            
            bool removed = _pathBlacklist.RemoveAll(x => x.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                SavePathBlacklist();
                Logger.LogInfo($"路径 '{normalizedPath}' 已从路径黑名单中移除");
            }
            else
            {
                Logger.LogInfo($"路径 '{normalizedPath}' 不在路径黑名单中");
            }
            
            return removed;
        }
        
        /// <summary>
        /// 获取所有路径黑名单
        /// </summary>
        /// <returns>路径黑名单列表的副本</returns>
        public static List<string> GetPathBlacklist()
        {
            return new List<string>(_pathBlacklist);
        }
        
        /// <summary>
        /// 检查指定路径是否在路径黑名单中（包括子目录检查）
        /// </summary>
        /// <param name="path">要检查的路径</param>
        /// <returns>如果路径在黑名单中返回true，否则返回false</returns>
        public static bool IsPathBlocked(string path)
        {
            string normalizedPath = NormalizePath(path);
            
            foreach (string blacklistedPath in _pathBlacklist)
            {
                // 比较路径是否匹配黑名单中的路径（不包括根目录本身）
                if (IsSubdirectoryOf(blacklistedPath, normalizedPath))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查child路径是否为parent路径的子目录（不包括parent本身）
        /// </summary>
        /// <param name="parent">父目录路径</param>
        /// <param name="child">子目录路径</param>
        /// <returns>如果是子目录返回true，否则返回false</returns>
        private static bool IsSubdirectoryOf(string parent, string child)
        {
            // 标准化路径并确保以反斜杠结尾以便正确比较
            string parentPath = NormalizePath(parent).TrimEnd('\\') + "\\";
            string childPath = NormalizePath(child).TrimEnd('\\') + "\\";
            
            // 检查child是否在parent目录下（但不等于parent）
            return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase) && 
                   !childPath.Equals(parentPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}