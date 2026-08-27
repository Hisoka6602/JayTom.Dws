namespace JayTom.Dws.Client.Events;

/// <summary>表示仅由桌面表现层消费的窗口操作。</summary>
public sealed class WindowsAction
{
    /// <summary>获取目标窗口键。</summary>
    public string WindowKey { get; init; } = "shell";

    /// <summary>获取窗口操作类型。</summary>
    public WindowsActionType Type { get; init; }
}
