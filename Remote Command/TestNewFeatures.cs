using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Remote_Command
{
    /// <summary>
    /// 测试新增功能的测试类
    /// </summary>
    public class TestNewFeatures
    {
        private static UdpClient _udpClient;
        private const int TargetPort = 6743;
        private const string TargetIp = "127.0.0.1"; // 本地测试
        
        public static void Main(string[] args)
        {
            Console.WriteLine("开始测试新增的UDP命令功能...");
            
            try
            {
                // 初始化UDP客户端
                _udpClient = new UdpClient();
                
                // 测试各个新功能
                TestDisableNicDetection();
                TestDisableAllFeatures();
                TestRestoreConfig();
                
                Console.WriteLine("\n所有测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
            }
            finally
            {
                _udpClient?.Close();
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// 测试关闭网卡检测功能
        /// </summary>
        private static void TestDisableNicDetection()
        {
            Console.WriteLine("\n=== 测试关闭网卡检测功能 ===");
            
            try
            {
                // 发送关闭网卡检测命令
                string command = "MOT-RC disable_nic_detection";
                SendUdpCommand(command);
                
                Console.WriteLine($"已发送命令: {command}");
                Console.WriteLine("预期结果: 网卡检测功能应被禁用，相关网络监控功能停止工作");
                
                // 等待一段时间让系统处理命令
                System.Threading.Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试关闭网卡检测功能时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试关闭所有功能
        /// </summary>
        private static void TestDisableAllFeatures()
        {
            Console.WriteLine("\n=== 测试关闭所有功能 ===");
            
            try
            {
                // 发送关闭所有功能命令
                string command = "MOT-RC disable_all_features";
                SendUdpCommand(command);
                
                Console.WriteLine($"已发送命令: {command}");
                Console.WriteLine("预期结果: 所有功能应被禁用，进程监控停止，不阻止任何应用程序运行");
                
                // 等待一段时间让系统处理命令
                System.Threading.Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试关闭所有功能时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试还原配置功能
        /// </summary>
        private static void TestRestoreConfig()
        {
            Console.WriteLine("\n=== 测试还原配置功能 ===");
            
            try
            {
                // 发送还原配置命令
                string command = "MOT-RC restore_config";
                SendUdpCommand(command);
                
                Console.WriteLine($"已发送命令: {command}");
                Console.WriteLine("预期结果: 所有配置应还原为默认状态，包括清空白名单、黑名单等");
                
                // 等待一段时间让系统处理命令
                System.Threading.Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试还原配置功能时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 发送UDP命令
        /// </summary>
        /// <param name="command">要发送的命令</param>
        private static void SendUdpCommand(string command)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(command);
                IPEndPoint targetEndpoint = new IPEndPoint(IPAddress.Parse(TargetIp), TargetPort);
                _udpClient.Send(data, data.Length, targetEndpoint);
                Console.WriteLine($"UDP命令已发送到 {TargetIp}:{TargetPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送UDP命令时发生错误: {ex.Message}");
                throw;
            }
        }
    }
}