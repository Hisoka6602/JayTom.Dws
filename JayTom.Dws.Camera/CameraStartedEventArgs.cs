namespace JayTom.Dws.Camera;

/// <summary>封装相机启动事件数据。</summary>
public class CameraStartedEventArgs : EventArgs {
    /// <summary>获取或设置已启动的相机。</summary>
    public ICamera? Camera { get; set; }

    /// <summary>获取或设置关联的相机信息。</summary>
    public CameraInfo? CameraInfo { get; set; }
}
