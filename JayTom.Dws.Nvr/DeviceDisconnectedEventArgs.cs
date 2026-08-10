namespace JayTom.Dws.Nvr;

/// <summary>封装 NVR 设备断开事件数据。</summary>
public class DeviceDisconnectedEventArgs : EventArgs {
    /// <summary>获取或设置厂商 SDK 登录句柄。</summary>
    public IntPtr LoginHandle { get; set; }

    /// <summary>获取或设置断开原因。</summary>
    public string Message { get; set; } = string.Empty;
}
