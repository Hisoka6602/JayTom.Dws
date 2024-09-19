using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.ComponentModel;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.SerialPort;

namespace JayTom.Dws.Plugin.Scale {

    public class BaseScaleConnectParam {

        /// <summary>
        /// 串口参数
        /// </summary>
        public SerialPortConnectParam? SerialPortInfo { get; set; }

        /// <summary>
        /// Tcp参数
        /// </summary>
        public TcpConnectParam? TcpConnectInfo { get; set; }

        public ScaleCommunicationMode Mode { get; set; } = ScaleCommunicationMode.SerialPort;
    }

    public class SerialPortConnectParam {

        /// <summary>
        /// 串口名称
        /// </summary>
        public string PortName { get; set; } = string.Empty;     // 串口名称

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// 效验位
        /// </summary>
        public Parity Parity { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; }

        /// <summary>
        /// 数据格式
        /// </summary>
        public FormatType DataFormat { get; set; } = FormatType.Ascii;
    }

    public class TcpConnectParam {

        /// <summary>
        /// 连接模式(客户端、服务端)
        /// </summary>
        public TcpConnectionMode? ConnectionMode { get; set; }

        /// <summary>
        /// 客户端配置
        /// </summary>
        public TcpParamInfo ClientConfig { get; set; } = new();

        /// <summary>
        /// 服务端配置
        /// </summary>
        public TcpParamInfo ServerConfig { get; set; } = new();

        /// <summary>
        /// 数据格式
        /// </summary>
        public FormatType DataFormat { get; set; } = FormatType.Ascii;
    }

    public class TcpParamInfo {

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }
    }
}