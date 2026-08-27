// DWS-COHESIVE-CONTRACTS: 分拣事实和组合规则构成封闭规则代数。
namespace JayTom.Dws.Domain.Sorting;

/// <summary>
/// 定义可组合、无需 JSON 解释器的分拣规则。
/// </summary>
public abstract record SortingRule {
    /// <summary>判断给定事实是否匹配。</summary>
    public abstract bool IsMatch(SortingFacts facts);
}

/// <summary>按条码前缀匹配的分拣规则。</summary>
public sealed record BarcodePrefixRule(string Prefix) : SortingRule {
    /// <inheritdoc />
    public override bool IsMatch(SortingFacts facts) =>
        facts.Barcode.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
}

/// <summary>按重量闭区间匹配的分拣规则。</summary>
public sealed record WeightRangeRule(decimal Minimum, decimal Maximum) : SortingRule {
    /// <inheritdoc />
    public override bool IsMatch(SortingFacts facts) =>
        facts.Weight is { } weight && weight >= Minimum && weight <= Maximum;
}

/// <summary>组合全部子规则的分拣规则。</summary>
public sealed record AllOfSortingRule(IReadOnlyList<SortingRule> Rules) : SortingRule {
    /// <inheritdoc />
    public override bool IsMatch(SortingFacts facts) => Rules.All(rule => rule.IsMatch(facts));
}

/// <summary>表示执行分拣规则所需的不可变事实。</summary>
public sealed record SortingFacts(string Barcode, decimal? Weight, decimal? Volume);
