namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 表示插件清单与当前宿主的兼容性状态。
/// </summary>
public enum PluginCompatibilityStatus {
    /// <summary>
    /// 插件清单有效且与宿主兼容。
    /// </summary>
    Compatible,

    /// <summary>
    /// 插件清单缺少必要信息或包含无效版本。
    /// </summary>
    InvalidManifest,

    /// <summary>
    /// 当前宿主版本低于插件要求。
    /// </summary>
    HostVersionTooLow,

    /// <summary>
    /// 插件使用了不受支持的契约主版本。
    /// </summary>
    ContractVersionMismatch
}
