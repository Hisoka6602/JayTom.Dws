namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 定义不依赖桌面 UI 或宿主容器的插件元数据与生命周期事件。
/// </summary>
public interface IPlugin {
    /// <summary>获取插件唯一标识。</summary>
    long Id { get; }

    /// <summary>获取插件名称。</summary>
    string Name { get; }

    /// <summary>获取插件版本。</summary>
    Version Version { get; }

    /// <summary>获取插件文件完整路径。</summary>
    string FilePath { get; }

    /// <summary>获取插件显示标题。</summary>
    string Title { get; }

    /// <summary>获取插件详细描述。</summary>
    string Description { get; }

    /// <summary>获取插件作者。</summary>
    string Author { get; }

    /// <summary>获取插件简述。</summary>
    string Summary { get; }

    /// <summary>获取插件发布日期。</summary>
    DateTimeOffset ReleaseDate { get; }

    /// <summary>获取插件要求的最低客户端版本。</summary>
    Version ClientDependencyVersion { get; }

    /// <summary>获取插件类型。</summary>
    PluginType Type { get; }

    /// <summary>插件产生跨模块消息时触发。</summary>
    event EventHandler<PluginMessageEventArgs>? PluginMessageReceived;

    /// <summary>插件完成加载时触发。</summary>
    event EventHandler<PluginLifecycleEventArgs>? PluginLoaded;

    /// <summary>插件退出时触发。</summary>
    event EventHandler<PluginLifecycleEventArgs>? PluginExited;

    /// <summary>宿主文化设置变化时触发。</summary>
    event EventHandler<PluginCultureChangedEventArgs>? CultureChanged;

    /// <summary>插件发生未处理异常时触发。</summary>
    event EventHandler<PluginExceptionEventArgs>? PluginExceptionOccurred;
}
