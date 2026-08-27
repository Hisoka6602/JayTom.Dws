using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.Sorting;

/// <summary>
/// 保存一次构建完成的格口配置及全部派生索引，保证读线程只会观察到同一版本的数据。
/// </summary>
internal sealed class SortingExitSnapshot
{
    /// <summary>初始化完整的格口配置快照。</summary>
    public SortingExitSnapshot(
        PackageExitDefinitionInfoModel[] items,
        IReadOnlyDictionary<long, PackageExitDefinitionInfoModel> exitLookup,
        IReadOnlyDictionary<long, PackageExitDefinitionInfoModel> alternateExitByParent,
        PackageExitDefinitionInfoModel? activeAbnormalExit)
    {
        Items = items;
        ExitLookup = exitLookup;
        AlternateExitByParent = alternateExitByParent;
        ActiveAbnormalExit = activeAbnormalExit;
    }

    /// <summary>格口配置的有序只读数组。</summary>
    public PackageExitDefinitionInfoModel[] Items { get; }

    /// <summary>按格口编号查询格口配置的索引。</summary>
    public IReadOnlyDictionary<long, PackageExitDefinitionInfoModel> ExitLookup { get; }

    /// <summary>按主格口编号查询首个可用备用格口的索引。</summary>
    public IReadOnlyDictionary<long, PackageExitDefinitionInfoModel> AlternateExitByParent { get; }

    /// <summary>当前启用的异常格口。</summary>
    public PackageExitDefinitionInfoModel? ActiveAbnormalExit { get; }

    /// <summary>创建没有任何格口配置的初始快照。</summary>
    public static SortingExitSnapshot Empty { get; } = new(
        [],
        new Dictionary<long, PackageExitDefinitionInfoModel>(),
        new Dictionary<long, PackageExitDefinitionInfoModel>(),
        null);
}
