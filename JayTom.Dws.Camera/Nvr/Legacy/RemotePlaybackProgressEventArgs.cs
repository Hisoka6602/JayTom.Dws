namespace JayTom.Dws.Camera.Nvr.Legacy;

/// <summary>封装远程回放进度事件数据。</summary>
public class RemotePlaybackProgressEventArgs : EventArgs {
    /// <summary>获取或设置回放进度百分比。</summary>
    public int Progress { get; set; }
}
