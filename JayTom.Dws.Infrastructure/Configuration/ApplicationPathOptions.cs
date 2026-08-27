namespace JayTom.Dws.Infrastructure.Configuration;

/// <summary>定义全部可写运行时路径，禁止从当前工作目录隐式推导。</summary>
public sealed record ApplicationPathOptions {
    /// <summary>获取应用数据根目录。</summary>
    public required string DataDirectory { get; init; }

    /// <summary>获取配置目录。</summary>
    public required string ConfigurationDirectory { get; init; }

    /// <summary>获取日志目录。</summary>
    public required string LogDirectory { get; init; }

    /// <summary>获取模型目录。</summary>
    public required string ModelDirectory { get; init; }

    /// <summary>获取可选适配器包目录。</summary>
    public required string AdapterPackDirectory { get; init; }
}
