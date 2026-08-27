namespace JayTom.Dws.Application.Events;

/// <summary>表示外部系统请求执行的应用生命周期命令。</summary>
public sealed class RemoteAction
{
    /// <summary>获取命令附带的消息。</summary>
    public object? Message { get; init; }

    /// <summary>获取远程命令。</summary>
    public RemoteCommand Command { get; init; }
}
