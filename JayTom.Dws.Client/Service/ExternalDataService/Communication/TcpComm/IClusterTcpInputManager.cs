using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.ContentInputSettingsModels;

namespace JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm {

    public interface IClusterTcpInputManager {

        /// <summary>
        /// 事件：接收到信息。
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// 事件：连接成功。
        /// </summary>
        event EventHandler<TcpInputBindingInfo> ConnectionSuccessful;

        /// <summary>
        /// 连接失败
        /// </summary>
        event EventHandler<TcpInputBindingInfo> ConnectionFailed;

        /// <summary>
        /// 断开事件
        /// </summary>
        event EventHandler<TcpInputBindingInfo> Disconnected;

        /// <summary>
        /// 连接到指定的 TCP 输入绑定。
        /// </summary>
        /// <param name="tcpInput">要连接的 TCP 输入绑定信息。</param>
        Task<bool> Connect(TcpInputBindingInfo tcpInput);

        /// <summary>
        /// 断开与指定的 TCP 输入绑定的连接。
        /// </summary>
        /// <param name="tcpInput">要断开的 TCP 输入绑定信息。</param>
        void Disconnect(TcpInputBindingInfo tcpInput);

        /// <summary>
        /// 重新连接到指定的 TCP 输入绑定。
        /// </summary>
        /// <param name="tcpInput">要重新连接的 TCP 输入绑定信息。</param>
        Task<bool> Reconnect(TcpInputBindingInfo tcpInput);

        /// <summary>
        /// 批量连接
        /// </summary>
        /// <param name="tcpInputs"></param>
        Task<KeyValuePair<bool, string>> ConnectBatch(List<TcpInputBindingInfo> tcpInputs);

        /// <summary>
        /// 连接所有 TCP 输入绑定。
        /// </summary>
        Task<bool> ConnectAll();

        /// <summary>
        /// 断开所有 TCP 输入绑定的连接。
        /// </summary>
        Task DisconnectAll();
    }

    public class MessageReceivedEventArgs : EventArgs {
        public TcpInputBindingInfo? Info { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}