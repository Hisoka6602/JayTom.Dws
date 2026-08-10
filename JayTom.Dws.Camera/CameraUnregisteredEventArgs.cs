namespace JayTom.Dws.Camera;

/// <summary>封装相机注销事件数据。</summary>
public class CameraUnregisteredEventArgs : EventArgs {
    /// <summary>获取或设置关联的相机信息。</summary>
    public CameraInfo? CameraInfo { get; set; }
}
