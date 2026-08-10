namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 封装宿主文化设置变化信息。
/// </summary>
public sealed class PluginCultureChangedEventArgs : EventArgs {
    /// <summary>初始化文化设置变化事件。</summary>
    public PluginCultureChangedEventArgs(string cultureName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        CultureName = cultureName;
    }

    /// <summary>获取标准文化名称。</summary>
    public string CultureName { get; }
}
