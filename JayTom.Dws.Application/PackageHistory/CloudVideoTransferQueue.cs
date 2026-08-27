using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 使用包裹和云视频上传仓储实现稳定的云视频任务队列语义。
/// </summary>
public sealed class CloudVideoTransferQueue : ICloudVideoTransferQueue
{
    /// <summary>包裹查询持久化边界。</summary>
    private readonly IPackageRepository _packages;

    /// <summary>云视频上传回执持久化边界。</summary>
    private readonly ICloudVideoUploadRepository _receipts;

    /// <summary>创建云视频上传任务队列。</summary>
    public CloudVideoTransferQueue(
        IPackageRepository packages,
        ICloudVideoUploadRepository receipts)
    {
        _packages = packages;
        _receipts = receipts;
    }

    /// <summary>按扫描时间领取尚未成功上传云视频的包裹。</summary>
    public async Task<IReadOnlyList<PackageInfoModel>> ListPendingAsync(
        DateTime scannedAfter,
        DateTime scannedBefore,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        var result = await _packages.SelectPackage(
                package => package.BarCodeInfo != null &&
                           package.BarCodeInfo.ScanTime > scannedAfter &&
                           package.BarCodeInfo.ScanTime <= scannedBefore &&
                           (package.CloudVideoUploadInfo == null ||
                            package.CloudVideoUploadInfo.UploadTime == null),
                package => package.PackageCreateTime,
                0,
                limit,
                cancellationToken)
            .ConfigureAwait(false);
        return result.Key ? result.Value : Array.Empty<PackageInfoModel>();
    }

    /// <summary>新增或更新指定包裹的云视频上传回执。</summary>
    public async Task<bool> SaveReceiptAsync(
        long packageId,
        CloudVideoUploadReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var model = await _receipts.FirstOrDefault(
                item => item.PackageId == packageId,
                cancellationToken)
            .ConfigureAwait(false);
        model ??= new CloudVideoUploadInfoModel { PackageId = packageId };
        model.ResponseContent = receipt.ResponseContent;
        model.TargetAddress = receipt.TargetAddress;
        model.UploadTime = receipt.UploadTime;
        model.UploadContent = receipt.UploadContent;
        model.UploadDuration = receipt.UploadDuration;
        model.ScanImageCount = receipt.ScanImageCount;
        model.PanoramaImageCount = receipt.PanoramaImageCount;
        return model.Id > 0
            ? await _receipts.Update(model, cancellationToken).ConfigureAwait(false)
            : await _receipts.Insert(model, cancellationToken).ConfigureAwait(false);
    }
}
