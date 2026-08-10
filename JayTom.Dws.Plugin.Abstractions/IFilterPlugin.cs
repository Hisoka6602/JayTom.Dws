using JayTom.Dws.Abstractions.Results;

namespace JayTom.Dws.PluginInterface;

/// <summary>
/// 定义条码数据过滤与拦截插件能力。
/// </summary>
public interface IFilterPlugin : IPlugin {
    /// <summary>对完整包裹测量数据执行过滤。</summary>
    Task<Result<BarCodeResult>> ExecuteAsync(
        string barcode,
        decimal weight,
        decimal length = default,
        decimal width = default,
        decimal height = default,
        decimal volume = default,
        PluginImage? image = default,
        PluginImage? panoramaImage = default,
        CancellationToken cancellationToken = default);

    /// <summary>对条码与重量执行过滤。</summary>
    Task<Result<BarCodeResult>> ExecuteAsync(
        string barcode,
        decimal weight,
        CancellationToken cancellationToken = default);

    /// <summary>仅对条码执行过滤。</summary>
    Task<Result<BarCodeResult>> ExecuteAsync(
        string barcode,
        CancellationToken cancellationToken = default);
}
