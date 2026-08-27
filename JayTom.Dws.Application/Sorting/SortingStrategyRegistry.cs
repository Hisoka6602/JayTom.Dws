namespace JayTom.Dws.Application.Sorting;

/// <summary>按策略类型维护不可变查找表，避免在分拣热路径中扫描实现集合。</summary>
public sealed class SortingStrategyRegistry
{
    /// <summary>策略类型到实现的预计算索引。</summary>
    private readonly IReadOnlyDictionary<SortingStrategyKind, ISortingStrategy> _strategies;

    /// <summary>创建注册表并拒绝重复的策略实现。</summary>
    public SortingStrategyRegistry(IEnumerable<ISortingStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        var strategyLookup = new Dictionary<SortingStrategyKind, ISortingStrategy>();
        foreach (ISortingStrategy strategy in strategies)
        {
            ArgumentNullException.ThrowIfNull(strategy);
            strategyLookup.Add(strategy.Kind, strategy);
        }
        _strategies = strategyLookup;
    }

    /// <summary>通过常量时间索引解析指定策略。</summary>
    public bool TryResolve(SortingStrategyKind kind, out ISortingStrategy? strategy) =>
        _strategies.TryGetValue(kind, out strategy);
}
