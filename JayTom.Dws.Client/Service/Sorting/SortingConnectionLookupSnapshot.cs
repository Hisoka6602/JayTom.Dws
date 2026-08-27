using System;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.Sorting;

/// <summary>保存同一配置版本中的格口连接映射和确定性默认连接顺序。</summary>
internal sealed class SortingConnectionLookupSnapshot
{
    /// <summary>表示尚未加载任何连接配置的空快照。</summary>
    internal static readonly SortingConnectionLookupSnapshot Empty = new(
        new Dictionary<long, CommunicationConnectionConfigInfoModel>(),
        Array.Empty<string>());

    /// <summary>初始化一个完整且只读使用的连接查找快照。</summary>
    /// <param name="exitConnections">格口到物理连接配置的映射。</param>
    /// <param name="orderedConnectionNames">按配置编号稳定排序的连接名称。</param>
    internal SortingConnectionLookupSnapshot(
        IReadOnlyDictionary<long, CommunicationConnectionConfigInfoModel> exitConnections,
        IReadOnlyList<string> orderedConnectionNames)
    {
        ExitConnections = exitConnections;
        OrderedConnectionNames = orderedConnectionNames;
    }

    /// <summary>获取格口到物理连接配置的查找表。</summary>
    internal IReadOnlyDictionary<long, CommunicationConnectionConfigInfoModel> ExitConnections { get; }

    /// <summary>获取用于无格口命令的确定性连接候选顺序。</summary>
    internal IReadOnlyList<string> OrderedConnectionNames { get; }
}
