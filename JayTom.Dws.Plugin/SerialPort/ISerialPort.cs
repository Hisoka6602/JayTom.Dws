using System;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.SerialPort {

    public interface ISerialPort {
        //串口连接(串口名、波特率、效验位、数据位、停止位)
        //串口接收到消息事件
        //串口发送消息(字节)
        //串口发送消息(字符串)
        //串口接发送消息事件
        //串口消息断开事件
        //串口连接事件
        //串口释放
        //串口异常事件

        /*event Action<string> MessageReceived;
        event Action<string> MessageSent;
        event Action<string> ConnectionEstablished;
        event Action<string> ConnectionClosed;
        event Action<string> ErrorOccurred;

        void Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        void SendBytes(byte[] data);
        void SendString(string data);
        void Release();*/
    }
}