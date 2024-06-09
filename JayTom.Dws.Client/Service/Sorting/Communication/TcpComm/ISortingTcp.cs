using System;
using JayTom.Dws.Plugin.Tcp;

namespace JayTom.Dws.Client.Service.Sorting.Communication.TcpComm {

    public interface ISortingTcp : ITcpOperations, IDisposable {

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
        void StartHeartbeat(string heartbeatData, FormatType formatType, TimeSpan interval);

        /// <summary>
        /// 停止心跳包
        /// </summary>
        void StopHeartbeat();
    }
}