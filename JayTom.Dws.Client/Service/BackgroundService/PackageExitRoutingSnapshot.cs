using System;
using System.Collections.Generic;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.DownstreamProtocols;

namespace JayTom.Dws.Client.Service.BackgroundService;

/// <summary>保存回调指令到连接协议、绑定和格口的同版本只读路由索引。</summary>
internal sealed class PackageExitRoutingSnapshot
{
    /// <summary>表示尚未加载路由配置的空快照。</summary>
    internal static readonly PackageExitRoutingSnapshot Empty = new(
        new Dictionary<string, IDeviceCommunicationProtocol?>(StringComparer.Ordinal),
        new Dictionary<string, long>(StringComparer.Ordinal),
        new Dictionary<long, long>(),
        new Dictionary<long, PackageExitDefinitionInfoModel>(),
        null);

    /// <summary>初始化一个完整且只读使用的回调路由快照。</summary>
    internal PackageExitRoutingSnapshot(
        IReadOnlyDictionary<string, IDeviceCommunicationProtocol?> protocolsByConnection,
        IReadOnlyDictionary<string, long> bindingIdsByInstruction,
        IReadOnlyDictionary<long, long> exitIdsByBinding,
        IReadOnlyDictionary<long, PackageExitDefinitionInfoModel> exitsByIdentifier,
        PackageExitDefinitionInfoModel? activeAbnormalExit)
    {
        ProtocolsByConnection = protocolsByConnection;
        BindingIdsByInstruction = bindingIdsByInstruction;
        ExitIdsByBinding = exitIdsByBinding;
        ExitsByIdentifier = exitsByIdentifier;
        ActiveAbnormalExit = activeAbnormalExit;
    }

    /// <summary>获取按连接名称缓存的协议实例。</summary>
    internal IReadOnlyDictionary<string, IDeviceCommunicationProtocol?> ProtocolsByConnection { get; }

    /// <summary>获取按指令内容索引的绑定编号。</summary>
    internal IReadOnlyDictionary<string, long> BindingIdsByInstruction { get; }

    /// <summary>获取按绑定编号索引的格口编号。</summary>
    internal IReadOnlyDictionary<long, long> ExitIdsByBinding { get; }

    /// <summary>获取按格口编号索引的格口定义。</summary>
    internal IReadOnlyDictionary<long, PackageExitDefinitionInfoModel> ExitsByIdentifier { get; }

    /// <summary>获取当前启用的异常格口。</summary>
    internal PackageExitDefinitionInfoModel? ActiveAbnormalExit { get; }
}
