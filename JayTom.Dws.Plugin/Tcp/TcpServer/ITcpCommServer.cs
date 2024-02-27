using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Tcp.TcpServer {

    public interface ITcpCommServer : ITcpBase {

        /// <summary>
        /// Ip地址
        /// </summary>
        string IpAddress { get; }

        /// <summary>
        /// 端口
        /// </summary>
        int Port { get; }

        /// <summary>
        /// 数据长度
        /// </summary>
        int DataLen { get; set; }

        /// <summary>
        /// 客户端连接事件
        /// </summary>
        event EventHandler<string> ClientConnected;

        /// <summary>
        /// 客户端断开事件
        /// </summary>
        event EventHandler<string> ClientDisconnected;

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> SendMessage(string ip, string message, CancellationToken token = default);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> SendMessage(string ip, byte[] message, CancellationToken token = default);

        /// <summary>
        /// 获取已连接客户端的Ip
        /// </summary>
        /// <returns></returns>
        Task<List<string>?> GetClientsIp();

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dataLen"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Connect(FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="par"></param>
        /// <returns></returns>
        bool SetParameter(object par);
    }
}