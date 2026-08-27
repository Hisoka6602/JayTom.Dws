using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories;

namespace JayTom.Dws.Application.SortingConfigurations;

/// <summary>
/// 使用规则仓储实现分拣规则读取用例。
/// </summary>
/// <typeparam name="TRule">规则实体类型。</typeparam>
public sealed class SortingRuleCatalog<TRule> : ISortingRuleCatalog<TRule>
    where TRule : BasePackageSortingConfig
{
    /// <summary>持有规则只读持久化边界。</summary>
    private readonly IRepository<TRule> _repository;

    /// <summary>创建分拣规则目录。</summary>
    public SortingRuleCatalog(IRepository<TRule> repository) => _repository = repository;

    /// <summary>按稳定顺序读取全部规则。</summary>
    public async Task<IReadOnlyList<TRule>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.Select(
                item => item.Id > 0,
                item => item.CreateTime,
                cancellationToken)
            .ConfigureAwait(false);
}
