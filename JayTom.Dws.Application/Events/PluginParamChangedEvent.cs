namespace JayTom.Dws.Application.Events;

/// <summary>表示插件参数或插件选择发生变化。</summary>
public sealed class PluginParamChangedEvent
{
    /// <summary>获取插件类型。</summary>
    public PluginType Type { get; init; }

    /// <summary>获取插件名称。</summary>
    public string PluginName { get; init; } = string.Empty;

    /// <summary>获取插件参数内容。</summary>
    public string Content { get; init; } = string.Empty;
}
