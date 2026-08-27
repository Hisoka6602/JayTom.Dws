using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 定义云视频上传任务的领取和上传回执保存边界。
/// </summary>
public interface ICloudVideoTransferQueue
{
    /// <summary>按扫描时间领取尚未成功上传云视频的包裹。</summary>
    Task<IReadOnlyList<PackageInfoModel>> ListPendingAsync(
        DateTime scannedAfter,
        DateTime scannedBefore,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>新增或更新指定包裹的云视频上传回执。</summary>
    Task<bool> SaveReceiptAsync(
        long packageId,
        CloudVideoUploadReceipt receipt,
        CancellationToken cancellationToken = default);
}
