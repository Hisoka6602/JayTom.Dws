using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.Tcp {
    public interface ITcpBase {
        /// <summary>
        /// 发送格式
        /// </summary>
        public FormatType FormatType { get; set; }
        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; }

        /// <summary>
        /// 连接异常事件(直接返回异常信息的Json)
        /// </summary>
        event EventHandler<string> ConnectionException;

        /// <summary>
        /// 异常事件
        /// </summary>
        event EventHandler<Exception> Exception;

        /// <summary>
        /// 断开事件(直接返回断开信息的Json)
        /// </summary>
        event EventHandler<string> Disconnected;

        /// <summary>
        /// 通讯消息事件
        /// </summary>
        event EventHandler<CommunicationInfo> Communication;

        /// <summary>
        /// 完成连接事件
        /// </summary>
        event EventHandler<string> Connected;

        event EventHandler<Exception> SendError; //发送异常

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        Task<bool> Connect(string ipAddress, int port, int timeOut = 1000, FormatType dataType = FormatType.Ascii, CancellationToken token = default);

        /// <summary>
        /// 重新连接
        /// </summary>
        /// <returns></returns>
        Task<bool> Reconnect(int count, CancellationToken token = default);

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> SendMessage(string message, CancellationToken token = default);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> SendMessage(byte[] message, CancellationToken token = default);

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        void Close();
    }

    public class CommunicationInfo {
        public DateTime Time { get; set; }
        public string Content { get; set; } = string.Empty;
        public CommunicationType Type { get; set; }
        public FormatType FormatType { get; set; }
    }

    public class TcpConnectParam {

        /// <summary>
        /// 地址
        /// </summary>
        public string? Address { get; set; } = "127.0.0.1";

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        public FormatType DataFormatType { get; set; } = FormatType.Ascii;
    }

    public enum CommunicationType {
        Send, Receive
    }

    public enum FormatType {
        Hex,
        Ascii
    }
}