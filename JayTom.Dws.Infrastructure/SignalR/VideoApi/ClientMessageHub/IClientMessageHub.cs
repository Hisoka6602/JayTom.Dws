namespace JayTom.Dws.Infrastructure.SignalR.VideoApi.ClientMessageHub {

    public interface IClientMessageHub {

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
        event Func<ReceiveMessageInfo, Task> ReceiveMessage;

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
    }

    public class ReceiveMessageInfo {

        /// <summary>
        /// 消息类型
        /// </summary>
        public ReceiveMessageType MessageType { get; set; } = ReceiveMessageType.None;

        /// <summary>
        /// 消息数据
        /// </summary>
        public object MessageData { get; set; }

        /// <summary>
        /// 消息时间
        /// </summary>
        public DateTime MessageTime { get; set; }
    }

    public enum ReceiveMessageType {

        /// <summary>
        /// 空
        /// </summary>
        None,

        /// <summary>
        /// 数据汇总
        /// </summary>
        DataStatistics,

        /// <summary>
        /// Item消息
        /// </summary>
        MessageItem,

        /// <summary>
        /// 更新节点
        /// </summary>
        UpDateNodes,

        /// <summary>
        /// 停止服务
        /// </summary>
        StopAutoService,

        /// <summary>
        /// 开启服务
        /// </summary>
        StartAutoService,
    }
}