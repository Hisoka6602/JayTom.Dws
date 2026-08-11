using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 定义通用包裹数据处理插件能力。
/// </summary>
public interface IProcessPlugin : IPlugin {
    /// <summary>执行处理逻辑并返回插件自定义结果。</summary>
    Task<Result<object>> ExecuteAsync(
        string barcode,
        decimal weight,
        decimal length = default,
        decimal width = default,
        decimal height = default,
        decimal volume = default,
        PluginImage? image = default,
        PluginImage? panoramaImage = default,
        CancellationToken cancellationToken = default);
}
