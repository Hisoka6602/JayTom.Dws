using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.PackageExits;

/// <summary>封装出口仓储查询表达式的应用层目录服务。</summary>
public sealed class PackageExitCatalog : IPackageExitCatalog, IPackageExitAdministration, IPackageExitManagement {
    /// <summary>出口定义持久化边界。</summary>
    private readonly IPackageExitDefinitionRepository _repository;

    /// <summary>创建出口目录服务。</summary>
    /// <param name="repository">出口定义持久化边界。</param>
    public PackageExitCatalog(IPackageExitDefinitionRepository repository) {
        _repository = repository;
    }

    /// <summary>按创建时间读取全部有效标识的出口定义。</summary>
    public async Task<IReadOnlyList<PackageExitDefinitionInfoModel>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.Select(
                model => model.Id > 0,
                model => model.CreateTime,
                cancellationToken)
            .ConfigureAwait(false);

    /// <summary>新增出口定义。</summary>
    public Task<bool> InsertAsync(
        PackageExitDefinitionInfoModel model,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(model);
        return _repository.Insert(model, cancellationToken);
    }

    /// <summary>批量新增出口定义。</summary>
    public Task<bool> InsertRangeAsync(
        IReadOnlyCollection<PackageExitDefinitionInfoModel> models,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(models);
        return models.Count == 0
            ? Task.FromResult(true)
            : _repository.InsertRange(models.ToList(), cancellationToken);
    }

    /// <summary>按标识读取出口定义。</summary>
    public Task<PackageExitDefinitionInfoModel?> FindByIdAsync(
        long id,
        CancellationToken cancellationToken = default) =>
        _repository.FirstOrDefault(model => model.Id == id, cancellationToken);

    /// <summary>按标识删除单个出口定义。</summary>
    public async Task<bool> DeleteByIdAsync(
        long id,
        CancellationToken cancellationToken = default) {
        var model = await FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return model is not null &&
               await _repository.Delete(model, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>按标识批量删除出口定义。</summary>
    public async Task<bool> DeleteByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0) {
            return true;
        }

        var idSet = ids.ToHashSet();
        var models = await _repository.Select(
                model => idSet.Contains(model.Id),
                model => model.Id,
                cancellationToken)
            .ConfigureAwait(false);
        return models.Count == 0 ||
               await _repository.DeleteRange(models, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>删除全部出口定义。</summary>
    public async Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) {
        var models = await _repository.Select(
                model => model.Id > 0,
                model => model.Id,
                cancellationToken)
            .ConfigureAwait(false);
        return models.Count == 0 ||
               await _repository.DeleteRange(models, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>按出口名称集合读取定义。</summary>
    public async Task<IReadOnlyList<PackageExitDefinitionInfoModel>> ListByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Count == 0) {
            return [];
        }

        var nameSet = names.ToHashSet(StringComparer.Ordinal);
        return await _repository.Select(
                model => nameSet.Contains(model.ExitName),
                model => model.Id,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>按修改时间读取全部出口定义。</summary>
    public async Task<IReadOnlyList<PackageExitDefinitionInfoModel>> ListByModifiedTimeAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.Select(
                model => model.Id > 0,
                model => model.ModifyTime,
                cancellationToken)
            .ConfigureAwait(false);

    /// <summary>更新单个出口定义。</summary>
    public Task<bool> UpdateAsync(
        PackageExitDefinitionInfoModel model,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(model);
        return _repository.Update(model, cancellationToken);
    }

    /// <summary>批量更新出口定义。</summary>
    public Task<bool> UpdateRangeAsync(
        IReadOnlyCollection<PackageExitDefinitionInfoModel> models,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(models);
        return models.Count == 0
            ? Task.FromResult(true)
            : _repository.UpdateRange(models.ToList(), cancellationToken);
    }

    /// <summary>使持久化出口定义与给定快照保持同步。</summary>
    public Task<bool> SyncAsync(
        IReadOnlyCollection<PackageExitDefinitionInfoModel> models,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(models);
        return _repository.SyncEntities([.. models], cancellationToken);
    }
}
