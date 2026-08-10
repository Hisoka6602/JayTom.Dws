using JayTom.Dws.Application.Integrations;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证外部提供商注册表的解析行为。</summary>
public sealed class ProviderRegistryTests {
    /// <summary>验证提供商标识按忽略大小写方式解析。</summary>
    [Fact]
    public void Resolve_is_case_insensitive_and_uses_the_registered_factory() {
        var registry = new ProviderRegistry<object>(
            new Dictionary<string, Func<object>> {
                ["primary"] = static () => new object()
            });

        Assert.True(registry.TryResolve("PRIMARY", out var first));
        Assert.True(registry.TryResolve("primary", out var second));
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    /// <summary>验证未知或空标识不会返回提供商实例。</summary>
    [Theory]
    [InlineData("")]
    [InlineData("missing")]
    public void Resolve_rejects_unknown_provider_ids(string providerId) {
        var registry = new ProviderRegistry<object>(
            new Dictionary<string, Func<object>>());

        Assert.False(registry.TryResolve(providerId, out var provider));
        Assert.Null(provider);
    }

    /// <summary>验证公开的标识列表使用稳定的忽略大小写排序。</summary>
    [Fact]
    public void Provider_ids_are_exposed_in_stable_order() {
        var registry = new ProviderRegistry<object>(
            new Dictionary<string, Func<object>> {
                ["zeta"] = static () => new object(),
                ["Alpha"] = static () => new object()
            });

        Assert.Equal(["Alpha", "zeta"], registry.ProviderIds);
    }
}
