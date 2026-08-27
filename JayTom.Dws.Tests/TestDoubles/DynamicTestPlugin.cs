using JayTom.Dws.Plugin.Contracts;

namespace JayTom.Dws.Tests.TestDoubles;

/// <summary>供可回收加载上下文契约测试动态实例化的最小插件。</summary>
public sealed class DynamicTestPlugin : IPlugin
{
    /// <summary>获取固定测试标识。</summary>
    public long Id => 1;

    /// <summary>获取测试插件名称。</summary>
    public string Name => nameof(DynamicTestPlugin);

    /// <summary>获取测试插件版本。</summary>
    public Version Version => new(1, 0, 0);

    /// <summary>获取动态加载后的程序集位置。</summary>
    public string FilePath => GetType().Assembly.Location;

    /// <summary>获取测试标题。</summary>
    public string Title => "动态测试插件";

    /// <summary>获取测试说明。</summary>
    public string Description => "验证插件隔离加载与卸载。";

    /// <summary>获取测试作者。</summary>
    public string Author => "Tests";

    /// <summary>获取测试摘要。</summary>
    public string Summary => "Plugin runtime contract fixture";

    /// <summary>获取固定发布时间。</summary>
    public DateTimeOffset ReleaseDate => new(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);

    /// <summary>获取最低宿主版本。</summary>
    public Version ClientDependencyVersion => new(1, 0, 0);

    /// <summary>获取插件能力类型。</summary>
    public PluginType Type => PluginType.ExtensionPackage;

    /// <summary>测试插件不主动发布跨模块消息。</summary>
    public event EventHandler<PluginMessageEventArgs>? PluginMessageReceived
    {
        add { }
        remove { }
    }

    /// <summary>测试插件不主动发布加载事件。</summary>
    public event EventHandler<PluginLifecycleEventArgs>? PluginLoaded
    {
        add { }
        remove { }
    }

    /// <summary>测试插件不主动发布退出事件。</summary>
    public event EventHandler<PluginLifecycleEventArgs>? PluginExited
    {
        add { }
        remove { }
    }

    /// <summary>测试插件不主动发布文化变更事件。</summary>
    public event EventHandler<PluginCultureChangedEventArgs>? CultureChanged
    {
        add { }
        remove { }
    }

    /// <summary>测试插件不主动发布异常事件。</summary>
    public event EventHandler<PluginExceptionEventArgs>? PluginExceptionOccurred
    {
        add { }
        remove { }
    }
}
