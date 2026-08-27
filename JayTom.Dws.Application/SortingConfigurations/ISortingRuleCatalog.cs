using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.SortingConfigurations;

/// <summary>
/// 提供分拣规则与正则配置的只读应用边界。
/// </summary>
/// <typeparam name="TRule">规则实体类型。</typeparam>
public interface ISortingRuleCatalog<TRule>
    where TRule : BasePackageSortingConfig
{
    /// <summary>按稳定顺序读取全部规则。</summary>
    Task<IReadOnlyList<TRule>> ListAsync(
        CancellationToken cancellationToken = default);
}
