using System;
using System.IO.Ports;

namespace JayTom.Dws.Client.Service.Sorting.Communication.SerialComm {

    public interface ISortingSerialPort : IDisposable {

        /// <summary>
        /// 状态
        /// </summary>
        SortingSerialPortStatus Status { get; }

        event EventHandler<ISortingSerialPort> ConnectionChanged; // 连接状态改变事件

        event EventHandler<MessageEventArgs> DataReceived; // 接收到数据事件

        event EventHandler<ISortingSerialPort> Disconnected; // 断开连接事件

        event EventHandler<ExceptionEventArgs> ErrorOccurred; // 异常发生事件

        event EventHandler<ExceptionEventArgs> SendError; //发送异常

        /// <summary>
        /// 心跳包异常事件
        /// </summary>
        public event EventHandler<Exception> HeartbeatError;

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
            Parity parity, StopBits stopBits, SortingSerialPortFormat dataFormat);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="message"></param>
        void Send(string message);

        /// <summary>
        /// 开启心跳包
        /// </summary>
        /// <param name="heartbeatData"></param>
        /// <param name="interval"></param>
        void StartHeartbeat(string heartbeatData, TimeSpan interval);

        /// <summary>
        /// 停止心跳包
        /// </summary>
        void StopHeartbeat();
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

    public enum SortingSerialPortStatus {

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

    public enum SortingSerialPortFormat {

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