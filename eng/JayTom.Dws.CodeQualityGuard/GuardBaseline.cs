using System.Text.Json.Serialization;

/// <summary>
/// 保存现有代码违规数量和 EF Core 模型状态，防止后续提交扩大技术债。
/// </summary>
internal sealed class GuardBaseline {
    /// <summary>
    /// 获取写入基线文件的中文用途说明。
    /// </summary>
    [JsonPropertyName("$comment")]
    public string Comment { get; init; } =
        "该文件记录现有代码质量技术债；新增违规会导致编译失败，请勿手工放宽计数。";

    /// <summary>
    /// 获取浮点数违规数量。
    /// </summary>
    public Dictionary<string, int> FloatingPoint { get; init; } = [];

    /// <summary>
    /// 获取缺少中文文档的声明数量。
    /// </summary>
    public Dictionary<string, int> MissingChineseDocumentation { get; init; } = [];

    /// <summary>
    /// 获取类型未独占文件的违规数量。
    /// </summary>
    public Dictionary<string, int> TypeIsolation { get; init; } = [];

    /// <summary>
    /// 获取未使用 long 类型的 ID 数量。
    /// </summary>
    public Dictionary<string, int> InvalidIdType { get; init; } = [];

    /// <summary>
    /// 获取热路径直接数据库或文件 I/O 数量。
    /// </summary>
    public Dictionary<string, int> HotPathIo { get; init; } = [];

    /// <summary>
    /// 获取原始 SQL 调用数量。
    /// </summary>
    public Dictionary<string, int> RawSql { get; init; } = [];

    /// <summary>
    /// 获取数据库工程策略违规数量。
    /// </summary>
    public Dictionary<string, int> DatabasePolicy { get; init; } = [];

    /// <summary>
    /// 获取数据库查询性能违规数量。
    /// </summary>
    public Dictionary<string, int> DatabaseQuery { get; init; } = [];

    /// <summary>
    /// 获取数据库保存图片或文件内容的违规数量。
    /// </summary>
    public Dictionary<string, int> DatabaseBinaryContent { get; init; } = [];

    /// <summary>
    /// 获取通用性能反模式数量。
    /// </summary>
    public Dictionary<string, int> Performance { get; init; } = [];

    /// <summary>
    /// 获取缺少中文说明的配置文件数量。
    /// </summary>
    public Dictionary<string, int> MissingChineseConfigurationDocumentation {
        get;
        init;
    } = [];

    /// <summary>
    /// 获取未使用 .NET 10 的工程数量。
    /// </summary>
    public Dictionary<string, int> LegacyTargetFramework { get; init; } = [];

    /// <summary>
    /// 获取 EF Core 数据模型的稳定签名。
    /// </summary>
    public string EfModelSignature { get; init; } = string.Empty;

    /// <summary>
    /// 获取 Code First 迁移数量。
    /// </summary>
    public int EfMigrationCount { get; init; }
}
