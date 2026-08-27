using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.SortingInstructions;

/// <summary>
/// 提供下位机分拣指令绑定的应用层管理边界。
/// </summary>
public interface ISortingInstructionBindingCatalog {
    /// <summary>读取全部指令绑定及其指令明细。</summary>
    Task<IReadOnlyList<SortingInstructionBindingInfoModel>> ListAsync(
        CancellationToken cancellationToken = default);

    /// <summary>读取全部分拣指令。</summary>
    Task<IReadOnlyList<SortingInstructionInfoModel>> ListInstructionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>判断指定出口是否允许启用绑定。</summary>
    Task<bool> CanActivateAsync(
        long exitId,
        long? excludingId = null,
        CancellationToken cancellationToken = default);

    /// <summary>新增绑定及其指令明细。</summary>
    Task<bool> AddAsync(
        SortingInstructionBindingInfoModel binding,
        CancellationToken cancellationToken = default);

    /// <summary>更新绑定及其指令明细。</summary>
    Task<bool> UpdateAsync(
        SortingInstructionBindingInfoModel binding,
        CancellationToken cancellationToken = default);

    /// <summary>设置绑定启用状态。</summary>
    Task<bool> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>按标识删除绑定。</summary>
    Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>按标识集合批量删除绑定。</summary>
    Task<bool> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);

    /// <summary>删除全部绑定。</summary>
    Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default);

    /// <summary>批量新增绑定及其指令明细。</summary>
    Task<bool> AddRangeAsync(
        IReadOnlyCollection<SortingInstructionBindingInfoModel> bindings,
        CancellationToken cancellationToken = default);

    /// <summary>使持久化指令绑定与给定快照保持同步。</summary>
    Task<bool> SyncAsync(
        IReadOnlyCollection<SortingInstructionBindingInfoModel> bindings,
        CancellationToken cancellationToken = default);
}
