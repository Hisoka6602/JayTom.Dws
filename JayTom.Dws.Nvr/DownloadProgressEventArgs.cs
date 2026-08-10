namespace JayTom.Dws.Nvr;

/// <summary>封装录像下载进度事件数据。</summary>
public class DownloadProgressEventArgs : EventArgs {
    /// <summary>获取或设置下载进度百分比。</summary>
    public int Progress { get; set; }
}
