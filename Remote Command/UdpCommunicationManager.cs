using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Remote_Command
{
    /// <summary>
    /// UDP通信管理器，负责UDP数据包的接收和发送
    /// </summary>
    public class UdpCommunicationManager
    {
        private const int ListenPort = 6743;
        private const int ResponsePort = 6746;
        private UdpClient _udpClient;
        private bool _isRunning;
        private IPEndPoint _baseServerEndpoint;

        /// <summary>
        /// 获取基本UDP服务端地址
        /// </summary>
        /// <returns>基本UDP服务端地址</returns>
        public IPEndPoint GetBaseServerEndpoint()
        {
            return _baseServerEndpoint;
        }

        /// <summary>
        /// 设置基本UDP服务端地址
        /// </summary>
        /// <param name="endpoint">服务端地址</param>
        public void SetBaseServerEndpoint(IPEndPoint endpoint)
        {
            _baseServerEndpoint = endpoint;
        }

        /// <summary>
        /// 启动UDP监听器
        /// </summary>
        public void StartListening(IPAddress localIp, AsyncCallback receiveCallback)
        {
            try
            {
                Logger.LogInfo($"使用IP地址进行监听: {localIp}");

                // 创建UDP客户端并绑定到指定端口
                var localEndPoint = new IPEndPoint(localIp, ListenPort);
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(localEndPoint);
                _isRunning = true;

                Logger.LogInfo($"UDP监听器已在端口 {ListenPort} 上启动");

                // 允许接收广播
                _udpClient.EnableBroadcast = true;

                // 开始异步接收数据
                _udpClient.BeginReceive(receiveCallback, null);
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
            _isRunning = false;
            _udpClient?.Close();
            Logger.LogInfo("UDP监听器已停止");
        }

        /// <summary>
        /// 检查UDP监听器是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 异步接收数据
        /// </summary>
        public void BeginReceive(AsyncCallback callback)
        {
            if (_isRunning && _udpClient != null)
            {
                _udpClient.BeginReceive(callback, null);
            }
        }

        /// <summary>
        /// 结束异步接收数据
        /// </summary>
        public byte[] EndReceive(IAsyncResult ar, ref IPEndPoint remoteEP)
        {
            if (_udpClient != null)
            {
                return _udpClient.EndReceive(ar, ref remoteEP);
            }
            return new byte[0];
        }

        /// <summary>
        /// 向基本UDP服务端发送响应数据
        /// </summary>
        /// <param name="responseData">要发送的响应数据</param>
        public void SendResponseToBaseServer(string responseData)
        {
            if (_baseServerEndpoint == null)
            {
                Logger.LogError("基本UDP服务端地址未设置，无法发送响应数据");
                return;
            }

            try
            {
                using (UdpClient client = new UdpClient())
                {
                    byte[] data = Encoding.UTF8.GetBytes(responseData);
                    client.Send(data, data.Length, _baseServerEndpoint);
                    Logger.LogInfo($"向基本UDP服务端 {_baseServerEndpoint} 发送响应数据: {responseData}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"向基本UDP服务端发送响应数据时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新绑定到特定的网络接口
        /// </summary>
        /// <param name="ipAddress">要绑定到的IP地址</param>
        /// <param name="receiveCallback">接收回调函数</param>
        public void RebindToSpecificInterface(IPAddress ipAddress, AsyncCallback receiveCallback)
        {
            try
            {
                // 停止当前监听
                _isRunning = false;
                _udpClient?.Close();

                // 等待一小段时间确保端口释放
                Thread.Sleep(100);

                // 在新接口上重新启动监听
                var localEndPoint = new IPEndPoint(ipAddress, ListenPort);
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(localEndPoint);
                _isRunning = true;

                Logger.LogInfo($"已重新绑定到接口 {ipAddress}:{ListenPort}");

                // 重新开始异步接收数据
                _udpClient.BeginReceive(receiveCallback, null);
            }
            catch (Exception ex)
            {
                Logger.LogError($"重新绑定到特定接口时发生错误: {ex.Message}");
            }
        }
    }
}