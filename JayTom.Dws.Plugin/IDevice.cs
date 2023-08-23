namespace JayTom.Dws.Plugin {

    public interface IDevice {

        /// <summary>
        /// 连接状态
        /// </summary>
        DeviceStatus Status { get; }

        /// <summary>
        /// 重连
        /// </summary>
        /// <returns></returns>
        bool Reconnect();

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        bool Connect<T>(T connectParam);

        /// <summary>
        /// 断开/释放
        /// </summary>
        void Dispose();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        bool Initialization();

        /// <summary>
        /// 初始化完成
        /// </summary>
        event EventHandler<IDevice> Initialized;

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<IDevice> Connected;

        /// <summary>
        /// 断开事件
        /// </summary>
        event EventHandler<IDevice> Disconnected;

        /// <summary>
        /// 已重连
        /// </summary>
        event EventHandler<IDevice> Reconnected;

        /// <summary>
        /// 异常事件
        /// </summary>
        event EventHandler<Exception> Excepted;
    }

    public enum DeviceStatus {

        /// <summary>
        /// 未初始化
        /// </summary>
        Uninitialized,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 已断开
        /// </summary>
        Disconnected,
    }
}