namespace JayTom.Dws.Application.Events;

/// <summary>定义可由远端请求的应用生命周期操作。</summary>
public enum RemoteCommand
{
    /// <summary>无操作。</summary>
    None,
    /// <summary>停止处理。</summary>
    Stop,
    /// <summary>开始处理。</summary>
    Start,
    /// <summary>退出应用。</summary>
    Exit,
    /// <summary>重启应用。</summary>
    Restart
}
