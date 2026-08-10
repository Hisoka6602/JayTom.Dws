using JayTom.Dws.Abstractions.Integrations;

namespace JayTom.Dws.Application.Integrations;

/// <summary>
/// 使用延迟工厂按稳定标识创建外部提供商实例。
/// </summary>
/// <typeparam name="TProvider">提供商契约类型。</typeparam>
public sealed class ProviderRegistry<TProvider> : IProviderRegistry<TProvider>
    where TProvider : class {
    /// <summary>按标识保存提供商工厂。</summary>
    private readonly IReadOnlyDictionary<string, Func<TProvider>> _factories;

    /// <summary>
    /// 初始化提供商注册表。
    /// </summary>
    /// <param name="factories">按稳定标识组织的提供商工厂。</param>
    public ProviderRegistry(IReadOnlyDictionary<string, Func<TProvider>> factories) {
        ArgumentNullException.ThrowIfNull(factories);
        _factories = new Dictionary<string, Func<TProvider>>(
            factories,
            StringComparer.OrdinalIgnoreCase);
        ProviderIds = [.. _factories.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>获取全部已注册提供商标识。</summary>
    public IReadOnlyCollection<string> ProviderIds { get; }

    /// <summary>尝试创建指定标识对应的提供商。</summary>
    public bool TryResolve(string providerId, out TProvider? provider) {
        if (!string.IsNullOrWhiteSpace(providerId) &&
            _factories.TryGetValue(providerId, out var factory)) {
            provider = factory();
            return true;
        }

        provider = null;
        return false;
    }
}
