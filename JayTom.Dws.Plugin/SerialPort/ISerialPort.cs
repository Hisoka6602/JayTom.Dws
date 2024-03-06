using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.SerialPort {

    public interface ISerialPort : IDisposable {
        /// <summary>
        /// 格式
        /// </summary>

        SerialPortFormat FormatType { get; }

        /// <summary>
        /// 状态
        /// </summary>
        SerialPortStatus Status { get; }

        event EventHandler<ISerialPort> ConnectionChanged; // 连接状态改变事件

        event EventHandler<MessageEventArgs> DataReceived; // 接收到数据事件

        event EventHandler<ISerialPort> Disconnected; // 断开连接事件

        event EventHandler<ExceptionEventArgs> ErrorOccurred; // 异常发生事件

        event EventHandler<ExceptionEventArgs> SendError; //发送异常

        //收发事件
        public event EventHandler<CommunicationInfo>? Communication;

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="dataBits"></param>
        /// <param name="parity"></param>
        /// <param name="stopBits"></param>
        /// <param name="dataFormat"></param>
        bool Connect(string portName, int baudRate, int dataBits,
            Parity parity, StopBits stopBits, SerialPortFormat dataFormat);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="message"></param>
        void Send(string message);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="message"></param>
        void Send(byte[] message);
    }

    public class ExceptionEventArgs : EventArgs {
        public Exception Exception { get; set; }

        public ExceptionEventArgs(Exception exception) {
            Exception = exception;
        }
    }

    public class MessageEventArgs : EventArgs {

        /// <summary>
        /// Ascii消息
        /// </summary>
        public string AsciiMessage { get; set; } = string.Empty;

        /// <summary>
        /// Hex消息
        /// </summary>
        public byte[]? HexMessage { get; set; }
    }

    public enum SerialPortStatus {

        /// <summary>
        /// 未连接
        /// </summary>
        NotConnected,

        /// <summary>
        /// 已断开
        /// </summary>
        Disconnected,

        /// <summary>
        /// 运行中
        /// </summary>
        Running
    }

    public enum SerialPortFormat {

        /// <summary>
        /// 十六进制
        /// </summary>
        Hex,

        /// <summary>
        /// ASCII码
        /// </summary>
        Ascii
    }
}