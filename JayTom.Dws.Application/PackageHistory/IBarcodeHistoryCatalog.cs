using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 提供历史条码的批量读取用例。
/// </summary>
public interface IBarcodeHistoryCatalog {
    /// <summary>按条码集合读取记录，并按扫描时间倒序排列。</summary>
    Task<IReadOnlyList<BarCodeInfoModel>> ListByCodesAsync(
        IReadOnlyCollection<string> barcodes,
        CancellationToken cancellationToken = default);
}

