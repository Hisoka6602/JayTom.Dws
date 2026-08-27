namespace JayTom.Dws.Application.PackageHistory;

/// <summary>与 EF 实体生命周期无关的历史包裹读模型。</summary>
public sealed record PackageHistoryItem(
    long PackageTimestamped,
    DateTime PackageCreateTime,
    string Other,
    PackageHistoryBarcode? BarCodeInfo,
    PackageHistoryWeight? WeightInfo,
    PackageHistoryVolume? VolumeInfo,
    PackageHistoryUpload? UploadInfo,
    PackageHistoryExit? ExitInfo,
    PackageHistorySorting? SortingInfo,
    PackageHistoryOcr? OcrInfo,
    IReadOnlyList<PackageHistoryImage> ImageInfos,
    bool IsUploadedToCloudVideo);
