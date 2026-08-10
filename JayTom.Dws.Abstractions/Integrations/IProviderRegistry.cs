namespace JayTom.Dws.Abstractions.Integrations;

/// <summary>
/// 按稳定标识解析外部提供商，避免向业务代码暴露依赖注入容器。
/// </summary>
public interface IProviderRegistry<TProvider> where TProvider : class {
    /// <summary>获取全部已注册提供商标识。</summary>
    IReadOnlyCollection<string> ProviderIds { get; }

    /// <summary>尝试解析指定提供商。</summary>
    bool TryResolve(string providerId, out TProvider? provider);
}
