namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 封装插件生命周期变化信息。
/// </summary>
public sealed class PluginLifecycleEventArgs : EventArgs {
    /// <summary>初始化插件生命周期事件。</summary>
    public PluginLifecycleEventArgs(IPlugin plugin) {
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
    }

    /// <summary>获取发生生命周期变化的插件。</summary>
    public IPlugin Plugin { get; }
}
