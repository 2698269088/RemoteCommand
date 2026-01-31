# Remote Command 远程命令执行系统

## 项目概述

Remote Command 是一个基于UDP协议的远程命令执行和进程管理系统，主要用于企业环境中对客户端计算机进行集中管控。该系统支持远程执行命令、进程监控、黑白名单管理等功能。

## 主要功能

### 1. 远程命令执行
- 通过UDP协议接收并执行远程命令
- 支持动态命令执行
- 自动记录命令执行日志
- 支持批处理文件(.bat)执行

### 2. 进程监控与管理
- 实时监控系统进程
- 自动识别应用进程（具有可见窗口的进程）
- 支持进程终止功能
- 提供进程列表查询功能

### 3. 黑白名单管控
#### 应用程序黑白名单
- **黑名单模式**：禁止黑名单中的应用程序运行
- **白名单模式**：只允许白名单中的应用程序运行
- 支持批量添加/删除应用程序

#### 路径黑白名单
- **路径黑名单**：阻止指定路径下的程序运行
- **路径白名单**：只允许指定路径下的程序运行
- 支持子目录匹配和路径标准化

### 4. 网络通信
- UDP协议通信，支持广播和单播
- 自动网络适配器检测和切换
- 动态IP地址变化监测
- 支持服务端地址配置保存

## 系统架构

### 核心组件

```
Program.cs              - 程序入口点和主控制循环
AppInitializer.cs       - 应用程序初始化和配置管理
CommandProcessor.cs     - 命令解析和处理中心
UdpListener.cs          - UDP消息监听和处理
ProcessMonitor.cs       - 进程监控和管控逻辑
ConfigManager.cs        - 应用程序黑白名单管理
PathBlacklistManager.cs - 路径黑名单管理
PathWhitelistManager.cs - 路径白名单管理
CommandExecutor.cs      - 命令执行引擎
Logger.cs               - 日志记录系统
```

### 网络组件
```
NetworkAdapterManager.cs    - 网络适配器管理和IP地址检测
NetworkStatusMonitor.cs     - 网络状态监控
UdpCommunicationManager.cs  - UDP通信管理
```

### 辅助组件
```
ProcessListProvider.cs      - 进程列表提供
ProcessTerminator.cs        - 进程终止工具
DynamicCommandExecutor.cs   - 动态命令执行器
```

## 配置文件说明

### 主配置目录
程序会在以下位置创建配置目录（按优先级）：
1. `D:\rc\` （首选）
2. 程序运行目录下的 `rc\` 子目录

### 配置文件
- `app_config.txt` - 应用程序黑白名单配置
- `applist.json` - 应用列表（预留）
- `udp_config.json` - UDP通信配置
- `application.log` - 系统运行日志
- `command_execution.log` - 命令执行日志

## 支持的命令格式

### 基本命令格式
所有命令都以 `MOT-RC` 前缀开头：

```
MOT-RC [命令类型] [参数]
```

### 核心命令

#### 连接管理
```
MOT-RC link [IP地址]        # 建立与服务端的连接
MOT-RC ACK                  # 确认响应
```

#### 命令执行
```
MOT-RC runcmd               # 执行本地批处理文件
MOT-RC cmd [命令内容]       # 执行动态命令
```

#### 进程管理
```
MOT-RC process list         # 获取进程列表
MOT-RC process app          # 获取应用进程列表（包含路径）
MOT-RC process taskkill [进程名]  # 终止指定进程
```

#### 黑白名单管理
```
# 应用程序黑白名单
MOT-RC black add [应用列表]     # 添加到黑名单
MOT-RC black del [应用列表]     # 从黑名单删除
MOT-RC width add [应用列表]     # 添加到白名单
MOT-RC width del [应用列表]     # 从白名单删除
MOT-RC mode whitelist           # 切换到白名单模式
MOT-RC mode blacklist           # 切换到黑名单模式

# 路径黑白名单
MOT-RC path_black add [路径列表]    # 添加路径到黑名单
MOT-RC path_black del [路径列表]    # 从路径黑名单删除
MOT-RC path_width add [路径列表]    # 添加路径到白名单
MOT-RC path_width del [路径列表]    # 从路径白名单删除
MOT-RC path_mode whitelist          # 切换到路径白名单模式
MOT-RC path_mode blacklist          # 切换到路径黑名单模式
```

### 路径列表格式
支持多种路径格式：
```
# 不带引号的标准路径
C:\Program Files\MyApp,D:\Test Folder

# 使用引号包裹的路径
"C:\Program Files\MyApp","D:\Test Folder"

# 混合使用
"C:\Program Files\MyApp",D:\TestFolder,"E:\Another Folder\Subfolder"

# 包含逗号的路径（必须使用引号）
"C:\Path,With,Commas","D:\NormalPath"
```

## 系统特性

### 安全机制
- 关键系统进程保护（不会终止系统核心进程）
- 路径标准化和验证
- 重复项自动过滤
- 配置变更实时生效

### 稳定性保障
- 异常处理和错误恢复
- 线程安全设计
- 资源自动清理
- 日志记录完整

### 网络适应性
- 自动检测网络适配器变化
- 支持多网卡环境
- IP地址变化自动重连
- 广播和单播双重支持

## 部署要求

### 系统环境
- Windows操作系统
- .NET Framework 4.6 或更高版本
- 管理员权限运行（用于进程管控）

### 网络要求
- UDP端口6746通信
- 网络连通性（用于与服务端通信）

## 使用示例

### 1. 基本部署
```
1. 编译生成可执行文件
2. 在目标机器上以管理员权限运行
3. 程序自动创建配置目录和文件
```

### 2. 配置黑白名单
```bash
# 添加应用程序到黑名单
MOT-RC black add notepad,calc,mspaint

# 切换到白名单模式
MOT-RC mode whitelist

# 添加允许的应用到白名单
MOT-RC width add chrome,firefox,vscode
```

### 3. 路径管控
```bash
# 添加路径到黑名单（阻止该路径下所有程序运行）
MOT-RC path_black add "C:\Games","D:\Downloads"

# 切换到路径白名单模式
MOT-RC path_mode whitelist

# 添加允许的路径到白名单
MOT-RC path_width add "C:\Program Files","C:\Windows\System32"
```

### 4. 远程命令执行
```bash
# 建立连接
MOT-RC link 192.168.1.100

# 执行系统命令
MOT-RC cmd ipconfig /all

# 获取进程信息
MOT-RC process app
```

## 注意事项

1. **权限要求**：程序需要管理员权限才能有效管控系统进程
2. **防火墙设置**：确保UDP端口6746未被防火墙阻止
3. **关键进程保护**：系统会自动保护关键进程不被误杀
4. **配置持久化**：所有配置会自动保存到配置文件
5. **日志查看**：可通过 `application.log` 和 `command_execution.log` 查看详细日志

## 开发信息

### 技术栈
- C# .NET Framework 4.6
- UDP Socket编程
- 多线程处理
- WMI系统管理

### 项目结构
```
Remote Command/
├── Properties/                 # 项目属性
├── bin/                       # 编译输出
├── obj/                       # 编译中间文件
├── *.cs                       # 源代码文件
├── *.csproj                   # 项目文件
└── app.manifest              # 应用程序清单
```

### 编译构建
```bash
# 使用Visual Studio或MSBuild编译
msbuild "Remote Command.csproj" /p:Configuration=Release
```

## 版本历史

### 当前版本特性
- 完整的UDP通信框架
- 应用程序和路径黑白名单管理
- 实时进程监控
- 动态命令执行
- 完善的日志系统
- 网络自适应能力

---
*注：本系统仅限于合法的企业IT管理用途，请遵守相关法律法规。*