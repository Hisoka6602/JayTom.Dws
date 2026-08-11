namespace JayTom.Dws.Tests.Architecture;

/// <summary>表示从配置文件读取的架构约束。</summary>
internal sealed record ArchitecturePolicy(
    Dictionary<string, string[]> ProjectReferences,
    Dictionary<string, string> TargetFrameworks,
    Dictionary<string, string[]> ForbiddenPackages);
