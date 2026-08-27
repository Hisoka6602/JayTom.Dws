using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.PackageExits;

/// <summary>定义包裹出口配置的应用层写入用例。</summary>
public interface IPackageExitAdministration {
    /// <summary>新增出口定义。</summary>
    Task<bool> InsertAsync(
        PackageExitDefinitionInfoModel model,
        CancellationToken cancellationToken = default);

    /// <summary>批量新增出口定义。</summary>
    Task<bool> InsertRangeAsync(
        IReadOnlyCollection<PackageExitDefinitionInfoModel> models,
        CancellationToken cancellationToken = default);

    /// <summary>按标识读取出口定义。</summary>
    Task<PackageExitDefinitionInfoModel?> FindByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>按标识删除单个出口定义。</summary>
    Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按标识批量删除出口定义。</summary>
    Task<bool> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);

    /// <summary>删除全部出口定义。</summary>
    Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>按出口名称集合读取定义。</summary>
    Task<IReadOnlyList<PackageExitDefinitionInfoModel>> ListByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default);

    /// <summary>按修改时间读取全部出口定义。</summary>
    Task<IReadOnlyList<PackageExitDefinitionInfoModel>> ListByModifiedTimeAsync(
        CancellationToken cancellationToken = default);

    /// <summary>更新单个出口定义。</summary>
    Task<bool> UpdateAsync(
        PackageExitDefinitionInfoModel model,
        CancellationToken cancellationToken = default);

    /// <summary>批量更新出口定义。</summary>
    Task<bool> UpdateRangeAsync(
        IReadOnlyCollection<PackageExitDefinitionInfoModel> models,
        CancellationToken cancellationToken = default);

    /// <summary>使持久化出口定义与给定快照保持同步。</summary>
    Task<bool> SyncAsync(
        IReadOnlyCollection<PackageExitDefinitionInfoModel> models,
        CancellationToken cancellationToken = default);
}
