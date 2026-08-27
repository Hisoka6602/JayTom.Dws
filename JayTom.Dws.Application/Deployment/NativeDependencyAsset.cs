namespace JayTom.Dws.Application.Deployment;

/// <summary>描述一个必须随指定 RID 发布的原生依赖入口文件。</summary>
public sealed record NativeDependencyAsset
{
    /// <summary>获取依赖包的稳定标识。</summary>
    public required string Name { get; init; }

    /// <summary>获取相对发布根目录的文件路径。</summary>
    public required string RelativePath { get; init; }

    /// <summary>获取相对适配项目根目录的源资产路径。</summary>
    public required string SourceRelativePath { get; init; }

    /// <summary>获取厂商声明的文件版本。</summary>
    public required string Version { get; init; }

    /// <summary>获取预期文件字节数。</summary>
    public long Length { get; init; }

    /// <summary>获取小写 SHA-256 摘要。</summary>
    public required string Sha256 { get; init; }
}
