namespace JayTom.Dws.Application.Events;

/// <summary>表示某个应用配置节已经发生变化。</summary>
public sealed class SettingsChangedEvent
{
    /// <summary>获取配置节名称。</summary>
    public string SettingsName { get; init; } = string.Empty;

    /// <summary>获取本次变化是否已保存到本地。</summary>
    public bool IsLocallySaved { get; init; }
}
