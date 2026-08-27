using System;
using System.Collections.Generic;
using System.Threading;
using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Client.Service.Sorting;

/// <summary>把兼容分拣模式映射为独立策略入口，替代服务中的模式分支。</summary>
internal sealed class LegacySortingStrategyRegistry
{
    /// <summary>按分拣模式索引策略委托。</summary>
    private readonly IReadOnlyDictionary<SortMode, Action<SortingParam, CancellationToken>> _strategies;

    /// <summary>创建策略注册表并拒绝重复模式。</summary>
    public LegacySortingStrategyRegistry(
        IEnumerable<KeyValuePair<SortMode, Action<SortingParam, CancellationToken>>> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        var strategyLookup = new Dictionary<SortMode, Action<SortingParam, CancellationToken>>();
        foreach (KeyValuePair<SortMode, Action<SortingParam, CancellationToken>> strategy in strategies)
        {
            strategyLookup.Add(strategy.Key, strategy.Value);
        }
        _strategies = strategyLookup;
    }

    /// <summary>执行当前模式对应的策略；无分拣模式时明确返回假。</summary>
    public bool TryExecute(
        SortMode mode,
        SortingParam input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_strategies.TryGetValue(mode, out Action<SortingParam, CancellationToken>? strategy))
        {
            return false;
        }
        strategy(input, cancellationToken);
        return true;
    }
}
