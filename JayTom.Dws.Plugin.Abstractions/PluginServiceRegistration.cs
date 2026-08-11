namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 描述插件向宿主声明的一项服务映射。
/// </summary>
public sealed record PluginServiceRegistration {
    /// <summary>获取服务契约类型。</summary>
    public required Type ServiceType { get; init; }

    /// <summary>获取服务实现类型。</summary>
    public required Type ImplementationType { get; init; }

    /// <summary>获取服务生命周期。</summary>
    public required PluginServiceLifetime Lifetime { get; init; }
}
