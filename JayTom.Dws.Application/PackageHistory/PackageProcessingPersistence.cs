using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 协调包裹主记录及其上传、图片、分拣和格口关联记录的持久化。
/// </summary>
public sealed class PackageProcessingPersistence : IPackageProcessingPersistence
{
    /// <summary>包裹主记录持久化边界。</summary>
    private readonly IPackageRepository _packages;

    /// <summary>接口上传记录持久化边界。</summary>
    private readonly IUploadRepository _uploads;

    /// <summary>图片元数据持久化边界。</summary>
    private readonly IImageRepository _images;

    /// <summary>分拣结果持久化边界。</summary>
    private readonly ISortingRepository _sorting;

    /// <summary>格口结果持久化边界。</summary>
    private readonly IExitInfoRepository _exits;

    /// <summary>创建实时包裹持久化协调器。</summary>
    public PackageProcessingPersistence(
        IPackageRepository packages,
        IUploadRepository uploads,
        IImageRepository images,
        ISortingRepository sorting,
        IExitInfoRepository exits)
    {
        _packages = packages;
        _uploads = uploads;
        _images = images;
        _sorting = sorting;
        _exits = exits;
    }

    /// <summary>批量保存包裹主记录，并同步运行时缓存。</summary>
    public Task<bool> AddPackagesAsync(
        IReadOnlyCollection<PackageInfoModel> packages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packages);
        return packages.Count == 0
            ? Task.FromResult(true)
            : _packages.InsertPackageRange([.. packages], cancellationToken);
    }

    /// <summary>按包裹时间戳读取运行时缓存中的完整包裹。</summary>
    public Task<PackageInfoModel?> FindCachedPackageAsync(
        long timestamp,
        CancellationToken cancellationToken = default) =>
        _packages.GetMemoryCachePackageInfo(timestamp, cancellationToken);

    /// <summary>用已持久化的关联信息刷新运行时包裹缓存。</summary>
    public void RefreshCachedPackage(
        PackageInfoModel package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        _packages.UpDateMemoryCachePackageInfo(package, cancellationToken);
    }

    /// <summary>保存一次接口上传结果。</summary>
    public Task<bool> AddUploadAttemptAsync(
        UploadInfoModel upload,
        CancellationToken cancellationToken = default) =>
        _uploads.Insert(upload, cancellationToken);

    /// <summary>保存一条图片元数据。</summary>
    public Task<bool> AddImageMetadataAsync(
        ImageInfoModel image,
        CancellationToken cancellationToken = default) =>
        _images.Insert(image, cancellationToken);

    /// <summary>新增包裹分拣结果。</summary>
    public Task<bool> AddSortingAsync(
        SortingInfoModel sorting,
        CancellationToken cancellationToken = default) =>
        _sorting.Insert(sorting, cancellationToken);

    /// <summary>更新包裹分拣结果。</summary>
    public Task<bool> UpdateSortingAsync(
        SortingInfoModel sorting,
        CancellationToken cancellationToken = default) =>
        _sorting.Update(sorting, cancellationToken);

    /// <summary>新增包裹格口结果。</summary>
    public Task<bool> AddExitAsync(
        ExitInfoModel exit,
        CancellationToken cancellationToken = default) =>
        _exits.Insert(exit, cancellationToken);

    /// <summary>更新包裹格口结果。</summary>
    public Task<bool> UpdateExitAsync(
        ExitInfoModel exit,
        CancellationToken cancellationToken = default) =>
        _exits.Update(exit, cancellationToken);
}
