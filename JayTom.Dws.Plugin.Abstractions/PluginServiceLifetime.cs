namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 描述插件服务的宿主无关生命周期。
/// </summary>
public enum PluginServiceLifetime {
    /// <summary>每次请求创建。</summary>
    Transient,
    /// <summary>在宿主作用域内复用。</summary>
    Scoped,
    /// <summary>在宿主进程内复用。</summary>
    Singleton
}
