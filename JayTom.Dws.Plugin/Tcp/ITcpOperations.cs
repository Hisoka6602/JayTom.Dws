using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;

namespace JayTom.Dws.Plugin.Tcp {

    public interface ITcpOperations : ITcpBase {

        /// <summary>
        /// 连接类型
        /// </summary>
        public ConnectionType ConnectionType { get; }

        /// <summary>
        /// Tcp服务端
        /// </summary>
        ITcpCommServer? TcpServer { get; }

        /// <summary>
        /// Tcp客户端
        /// </summary>
        ITcpCommClient? TcpClient { get; }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        Task<bool> Connect(string ipAddress, int port, ConnectionType type, int timeOut = 1000, FormatType dataType = FormatType.Ascii, int dataLen = 0, CancellationToken token = default);
    }

    public enum ConnectionType {

        /// <summary>
        /// 服务端
        /// </summary>
        Server,

        /// <summary>
        /// 客户端
        /// </summary>
        Client
    }

    public enum ConnectionStatus {

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected
    }
}