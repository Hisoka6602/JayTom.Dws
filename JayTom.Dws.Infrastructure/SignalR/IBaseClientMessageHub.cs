using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Infrastructure.SignalR.VideoApi.ClientMessageHub;

namespace JayTom.Dws.Infrastructure.SignalR {

    public interface IBaseClientMessageHub {

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// 链接Id
        /// </summary>
        public string ConnectionId { get; }

        /// <summary>
        /// 是否自动重连
        /// </summary>
        bool AutoReconnect { get; set; }

        /// <summary>
        /// 断连事件
        /// </summary>
        event Func<Exception, Task> Closed;

        /// <summary>
        /// 重连事件
        /// </summary>
        event Func<string, Task> Reconnected;

        /// <summary>
        /// 正在重连事件
        /// </summary>
        event Func<Exception, Task> Reconnecting;

        /// <summary>
        /// 接收到消息
        /// </summary>
        event Func<ClientReceiveMessageInfo, Task> ReceiveMessage;

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task StartAsync(string url, CancellationToken token = default);

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task StopAsync(CancellationToken token = default);

        /// <summary>
        /// 重连
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> Reconnect(TimeSpan delayTime, CancellationToken token = default);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessage<T>(string messageType, T message);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessage<T>(string client, string messageType, T message);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clients"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessage<T>(List<string> clients, string messageType, T message);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientGroup"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessageToGroup<T>(string clientGroup, string messageType, T message);
    }

    public class ClientReceiveMessageInfo {

        /// <summary>
        /// 消息类型
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// 消息数据
        /// </summary>
        public object MessageData { get; set; }

        /// <summary>
        /// 消息时间
        /// </summary>
        public DateTime MessageTime { get; set; }
    }
}