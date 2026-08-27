using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.SortingInstructions;

/// <summary>
/// 使用绑定仓储实现下位机分拣指令绑定用例。
/// </summary>
public sealed class SortingInstructionBindingCatalog : ISortingInstructionBindingCatalog {
    /// <summary>持有分拣指令绑定的持久化边界。</summary>
    private readonly ISortingInstructionBindingRepository _repository;

    /// <summary>持有分拣指令的只读持久化边界。</summary>
    private readonly ISortingInstructionRepository _instructionRepository;

    /// <summary>创建分拣指令绑定目录。</summary>
    public SortingInstructionBindingCatalog(
        ISortingInstructionBindingRepository repository,
        ISortingInstructionRepository instructionRepository) {
        _repository = repository;
        _instructionRepository = instructionRepository;
    }

    /// <summary>读取全部指令绑定及其指令明细。</summary>
    public async Task<IReadOnlyList<SortingInstructionBindingInfoModel>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.InstructionBindings(item => item.Id > 0, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>读取全部分拣指令。</summary>
    public async Task<IReadOnlyList<SortingInstructionInfoModel>> ListInstructionsAsync(
        CancellationToken cancellationToken = default) =>
        await _instructionRepository.Select(
                item => item.Id > 0,
                item => item.CreateTime,
                cancellationToken)
            .ConfigureAwait(false);

    /// <summary>判断指定出口是否允许启用绑定。</summary>
    public async Task<bool> CanActivateAsync(
        long exitId,
        long? excludingId = null,
        CancellationToken cancellationToken = default) =>
        await _repository.FirstOrDefault(
                item => item.ExitId == exitId &&
                        item.IsActive &&
                        (excludingId == null || item.Id != excludingId),
                cancellationToken)
            .ConfigureAwait(false) is null;

    /// <summary>新增绑定及其指令明细。</summary>
    public Task<bool> AddAsync(
        SortingInstructionBindingInfoModel binding,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(binding);
        return _repository.InsertDetailAsync(binding, cancellationToken);
    }

    /// <summary>更新绑定及其指令明细。</summary>
    public Task<bool> UpdateAsync(
        SortingInstructionBindingInfoModel binding,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(binding);
        return _repository.UpdateDetailAsync(binding, cancellationToken);
    }

    /// <summary>设置绑定启用状态。</summary>
    public async Task<bool> SetActiveAsync(
        long id,
        bool isActive,
        CancellationToken cancellationToken = default) {
        var binding = await _repository.FirstOrDefault(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (binding is null) {
            return false;
        }

        binding.IsActive = isActive;
        return await _repository.Update(binding, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>按标识删除绑定。</summary>
    public async Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default) {
        var binding = await _repository.FirstOrDefault(item => item.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return binding is not null &&
               await _repository.Delete(binding, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>按标识集合批量删除绑定。</summary>
    public async Task<bool> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0) {
            return true;
        }

        var bindings = await _repository.Select(item => ids.Contains(item.Id), item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        return bindings.Count == 0 ||
               await _repository.DeleteRange(bindings, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>删除全部绑定。</summary>
    public async Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) {
        var bindings = await _repository.Select(item => item.Id > 0, item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        return bindings.Count == 0 ||
               await _repository.DeleteRange(bindings, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>批量新增绑定及其指令明细。</summary>
    public Task<bool> AddRangeAsync(
        IReadOnlyCollection<SortingInstructionBindingInfoModel> bindings,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bindings);
        return _repository.InsertRangeDetailAsync([.. bindings], cancellationToken);
    }

    /// <summary>使持久化指令绑定与给定快照保持同步。</summary>
    public Task<bool> SyncAsync(
        IReadOnlyCollection<SortingInstructionBindingInfoModel> bindings,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bindings);
        return _repository.SyncEntities([.. bindings], cancellationToken);
    }
}
