namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 封装插件之间传递的不可变消息。
/// </summary>
public sealed class PluginMessageEventArgs : EventArgs {
    /// <summary>获取消息来源插件标识。</summary>
    public required long PluginId { get; init; }

    /// <summary>获取消息来源插件类型。</summary>
    public required PluginType PluginType { get; init; }

    /// <summary>获取消息来源插件名称。</summary>
    public required string PluginName { get; init; }

    /// <summary>获取目标插件类型。</summary>
    public required PluginType TargetType { get; init; }

    /// <summary>获取消息内容。</summary>
    public object? Content { get; init; }

    /// <summary>获取发送时间。</summary>
    public required DateTimeOffset SentAt { get; init; }

    /// <summary>获取消息描述。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>获取请求动作。</summary>
    public required PluginActionType Action { get; init; }
}
