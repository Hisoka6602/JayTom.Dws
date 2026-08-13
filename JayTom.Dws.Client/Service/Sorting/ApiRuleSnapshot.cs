using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Dto;

namespace JayTom.Dws.Client.Service.Sorting
{
    /// <summary>保存已解析的 API 分拣规则及其持久化关联。</summary>
    internal sealed record ApiRuleSnapshot(
        ApiRuleInfoModel Rule,
        ApiRuleJsonDto? Definition);
}
