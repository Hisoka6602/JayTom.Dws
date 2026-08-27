using JayTom.Dws.Plugin.Contracts;

namespace JayTom.Dws.Plugin.Runtime;

/// <summary>显式拥有一个插件实例及其可回收加载上下文。</summary>
public sealed class PluginHandle : IAsyncDisposable
{
    /// <summary>插件加载上下文。</summary>
    private PluginLoadContext? _loadContext;

    /// <summary>插件实例；释放后置空以解除上下文引用。</summary>
    private IPlugin? _instance;

    /// <summary>创建已加载插件的所有权句柄。</summary>
    internal PluginHandle(
        PluginManifest manifest,
        IPlugin instance,
        PluginLoadContext loadContext)
    {
        Manifest = manifest;
        _instance = instance;
        _loadContext = loadContext;
        LoadContextReference = new WeakReference(loadContext, trackResurrection: false);
    }

    /// <summary>获取经兼容性验证的插件清单。</summary>
    public PluginManifest Manifest { get; }

    /// <summary>获取尚未释放的插件实例。</summary>
    public IPlugin Instance =>
        Volatile.Read(ref _instance)
        ?? throw new ObjectDisposedException(nameof(PluginHandle));

    /// <summary>获取用于验证上下文最终被回收的弱引用。</summary>
    public WeakReference LoadContextReference { get; }

    /// <summary>停止后台插件、释放实例并请求卸载程序集上下文。</summary>
    public async ValueTask DisposeAsync()
    {
        IPlugin? instance = Interlocked.Exchange(ref _instance, null);
        PluginLoadContext? loadContext = Interlocked.Exchange(ref _loadContext, null);
        if (instance is null || loadContext is null)
        {
            return;
        }

        try
        {
            if (instance is IBackgroundPlugin backgroundPlugin)
            {
                await backgroundPlugin.StopAsync().ConfigureAwait(false);
            }

            if (instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        finally
        {
            loadContext.Unload();
        }
    }
}
