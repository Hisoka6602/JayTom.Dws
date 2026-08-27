using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 定义实时包裹处理流水线所需的持久化动作，避免编排层直接操作多张数据表。
/// </summary>
public interface IPackageProcessingPersistence
{
    /// <summary>批量保存包裹主记录，并同步运行时缓存。</summary>
    Task<bool> AddPackagesAsync(
        IReadOnlyCollection<PackageInfoModel> packages,
        CancellationToken cancellationToken = default);

    /// <summary>按包裹时间戳读取运行时缓存中的完整包裹。</summary>
    Task<PackageInfoModel?> FindCachedPackageAsync(
        long timestamp,
        CancellationToken cancellationToken = default);

    /// <summary>用已持久化的关联信息刷新运行时包裹缓存。</summary>
    void RefreshCachedPackage(
        PackageInfoModel package,
        CancellationToken cancellationToken = default);

    /// <summary>保存一次接口上传结果。</summary>
    Task<bool> AddUploadAttemptAsync(
        UploadInfoModel upload,
        CancellationToken cancellationToken = default);

    /// <summary>保存一条图片元数据。</summary>
    Task<bool> AddImageMetadataAsync(
        ImageInfoModel image,
        CancellationToken cancellationToken = default);

    /// <summary>新增包裹分拣结果。</summary>
    Task<bool> AddSortingAsync(
        SortingInfoModel sorting,
        CancellationToken cancellationToken = default);

    /// <summary>更新包裹分拣结果。</summary>
    Task<bool> UpdateSortingAsync(
        SortingInfoModel sorting,
        CancellationToken cancellationToken = default);

    /// <summary>新增包裹格口结果。</summary>
    Task<bool> AddExitAsync(
        ExitInfoModel exit,
        CancellationToken cancellationToken = default);

    /// <summary>更新包裹格口结果。</summary>
    Task<bool> UpdateExitAsync(
        ExitInfoModel exit,
        CancellationToken cancellationToken = default);
}
