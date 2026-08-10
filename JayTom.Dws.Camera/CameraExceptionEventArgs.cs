namespace JayTom.Dws.Camera;

/// <summary>封装相机异常事件数据。</summary>
public class CameraExceptionEventArgs : EventArgs {
    /// <summary>获取或设置相机异常。</summary>
    public Exception? Exception { get; set; }
}
