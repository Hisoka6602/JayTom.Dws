using JayTom.Dws.Plugin.Tcp;
using System;

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
        /// <param name="interval"></param>
        void StartHeartbeat(string heartbeatData, TimeSpan interval);

        /// <summary>
        /// 停止心跳包
        /// </summary>
        void StopHeartbeat();
    }
}