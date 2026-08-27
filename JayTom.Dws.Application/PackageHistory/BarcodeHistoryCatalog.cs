using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 使用条码仓储实现历史条码读取用例。
/// </summary>
public sealed class BarcodeHistoryCatalog : IBarcodeHistoryCatalog {
    /// <summary>持有条码记录的持久化边界。</summary>
    private readonly IBarCodeRepository _repository;

    /// <summary>创建历史条码目录。</summary>
    public BarcodeHistoryCatalog(IBarCodeRepository repository) {
        _repository = repository;
    }

    /// <summary>按条码集合读取记录，并按扫描时间倒序排列。</summary>
    public async Task<IReadOnlyList<BarCodeInfoModel>> ListByCodesAsync(
        IReadOnlyCollection<string> barcodes,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(barcodes);
        if (barcodes.Count == 0) {
            return Array.Empty<BarCodeInfoModel>();
        }

        return await _repository.SelectOrderByDescending(
                item => barcodes.Contains(item.Barcode),
                item => item.ScanTime,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
