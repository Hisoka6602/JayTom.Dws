using System.Linq.Expressions;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;

/// <summary>
/// 统一带规则明细的分拣配置持久化能力。
/// </summary>
/// <typeparam name="TConfiguration">分拣配置实体类型。</typeparam>
public interface ISortingDetailRepository<TConfiguration> : IRepository<TConfiguration>
    where TConfiguration : BasePackageSortingConfig {
    /// <summary>读取满足条件的配置及其规则明细。</summary>
    Task<List<TConfiguration>> SelectDetails(
        Expression<Func<TConfiguration, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>新增配置及其规则明细。</summary>
    Task<bool> InsertDetailAsync(
        TConfiguration entity,
        CancellationToken cancellationToken = default);

    /// <summary>批量新增配置及其规则明细。</summary>
    Task<bool> InsertRangeDetailAsync(
        List<TConfiguration> entities,
        CancellationToken cancellationToken = default);

    /// <summary>更新配置及其规则明细。</summary>
    Task<bool> UpdateDetailAsync(
        TConfiguration entity,
        CancellationToken cancellationToken = default);
}
