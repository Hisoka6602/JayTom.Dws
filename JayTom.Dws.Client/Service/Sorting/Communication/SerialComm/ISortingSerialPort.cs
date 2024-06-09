using System;
using System.IO.Ports;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Plugin.SerialPort;

namespace JayTom.Dws.Client.Service.Sorting.Communication.SerialComm {

    public interface ISortingSerialPort : ISerialPort {

        /// <summary>
        /// 心跳包异常事件
        /// </summary>
        public event EventHandler<Exception> HeartbeatError;

        /// <summary>
        /// 开启心跳包
        /// </summary>
        /// <param name="heartbeatData"></param>
        /// <param name="formatType"></param>
        /// <param name="interval"></param>
        void StartHeartbeat(string heartbeatData, SerialPortFormat formatType, TimeSpan interval);

        /// <summary>
        /// 停止心跳包
        /// </summary>
        void StopHeartbeat();
    }
}