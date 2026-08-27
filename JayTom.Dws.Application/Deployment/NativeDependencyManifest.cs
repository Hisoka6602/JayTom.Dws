namespace JayTom.Dws.Application.Deployment;

/// <summary>描述一个运行时标识对应的原生依赖清单。</summary>
public sealed record NativeDependencyManifest
{
    /// <summary>获取运行时标识。</summary>
    public required string Rid { get; init; }

    /// <summary>获取必须验证的原生依赖。</summary>
    public required IReadOnlyList<NativeDependencyAsset> Assets { get; init; }
}
