namespace JayTom.Dws.Client.Service.Device;

/// <summary>表示设备服务完整的运行生命周期状态。</summary>
public enum DeviceRuntimeState {
    /// <summary>尚未初始化或已经停止。</summary>
    Stopped = 0,

    /// <summary>正在初始化设备。</summary>
    Initializing = 1,

    /// <summary>设备服务正在运行。</summary>
    Running = 2,

    /// <summary>正在停止并释放设备。</summary>
    Stopping = 3,

    /// <summary>初始化或运行发生故障。</summary>
    Faulted = 4
}
