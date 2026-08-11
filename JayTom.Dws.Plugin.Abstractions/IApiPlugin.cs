using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 定义数据上传插件能力。
/// </summary>
public interface IApiPlugin : IPlugin {
    /// <summary>上传一条业务数据。</summary>
    Task<Result<UploadResponse>> UploadDataAsync(
        string barcode,
        decimal weight,
        decimal length = default,
        decimal width = default,
        decimal height = default,
        decimal volume = default,
        PluginImage? image = default,
        PluginImage? panoramaImage = default,
        CancellationToken cancellationToken = default);

    /// <summary>应用插件参数。</summary>
    Task<Result> SetParametersAsync(CancellationToken cancellationToken = default);
}
