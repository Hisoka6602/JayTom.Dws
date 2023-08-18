using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class TcpSettingsInfo {

        /// <summary>
        /// 连接模式(客户端、服务端)
        /// </summary>
        public TcpConnectionMode? ConnectionMode { get; set; }

        /// <summary>
        /// 客户端配置
        /// </summary>
        public TcpInfo ClientConfig { get; set; } = new();

        /// <summary>
        /// 服务端配置
        /// </summary>
        public TcpInfo ServerConfig { get; set; } = new();
    }

    public enum TcpConnectionMode {

        /// <summary>
        /// 客户端
        /// </summary>
        Client,

        /// <summary>
        /// 服务端
        /// </summary>
        Server
    }
}