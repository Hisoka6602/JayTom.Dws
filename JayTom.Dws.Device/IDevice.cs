namespace JayTom.Dws.Device {

    public interface IDevice {

        /// <summary>
        /// 设备编码
        /// </summary>
        string DeviceCode { get; }

        /// <summary>
        /// 连接状态
        /// </summary>
        DeviceStatus Status { get; }

        /// <summary>
        /// 设备类型
        /// </summary>
        DeviceType Type { get; }

        /// <summary>
        /// 重连
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Reconnect();

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Connect<T>(T connectParam);

        /// <summary>
        /// 断开/释放
        /// </summary>
        void Dispose();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Initialization();

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

        /// <summary>
        /// 已初始化
        /// </summary>
        Initialized
    }

    public enum DeviceType {

        /// <summary>
        /// 相机
        /// </summary>
        Camera,

        /// <summary>
        /// 串口
        /// </summary>
        SerialPort,

        /// <summary>
        /// 磅秤
        /// </summary>
        Scale,

        /// <summary>
        /// 电脑
        /// </summary>
        Computer,

        /// <summary>
        /// 下位机
        /// </summary>
        Controller,

        /// <summary>
        /// 传感器
        /// </summary>
        Sensor,

        /// <summary>
        /// 信号灯
        /// </summary>
        SignalLight,

        /// <summary>
        /// 喇叭
        /// </summary>
        Speaker,

        /// <summary>
        /// 蜂鸣器
        /// </summary>
        Buzzer,

        /// <summary>
        /// 流水线
        /// </summary>
        Conveyor,

        /// <summary>
        /// 转向机
        /// </summary>
        Steering,

        /// <summary>
        /// 电机
        /// </summary>
        Motor
    }
}