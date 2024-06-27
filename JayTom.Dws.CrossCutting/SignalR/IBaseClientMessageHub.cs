using Microsoft.AspNetCore.SignalR.Client;

namespace JayTom.Dws.CrossCutting.SignalR {

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
        event Func<object, Task> ReceiveMessage;

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="url"></param>
        /// <param name="registerMethod"></param>
        /// <param name="remarks"></param>
        /// <param name="token"></param>
        /// <param name="computerName"></param>
        /// <param name="programName"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        Task StartAsync(string url, Action<HubConnection> registerMethod, string computerName, string programName = "", string module = "", string remarks = "", CancellationToken token = default);

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
        /// 设置客户端信息
        /// </summary>
        /// <param name="computerName"></param>
        /// <param name="programName"></param>
        /// <param name="module"></param>
        /// <param name="remarks"></param>
        /// <returns></returns>
        Task SetClientInfo(string computerName, string programName, string module, string remarks);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessage<T>(string method, T message);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="method"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessage<T>(string client, string method, T message);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clients"></param>
        /// <param name="method"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessage<T>(List<string> clients, string method, T message);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientGroup"></param>
        /// <param name="method"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SendMessageToGroup<T>(string clientGroup, string method, T message);

        /// <summary>
        /// 获取客户端信息
        /// </summary>
        /// <returns></returns>
        Task<List<HubClientInfo>> GetOnlineClients();

        /// <summary>
        /// 获取服务信息
        /// </summary>
        /// <returns></returns>
        Task<HubServerInfo> GetServerInfo();
    }
}