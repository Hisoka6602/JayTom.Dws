using JayTom.Dws.Models.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Application.Communications;

/// <summary>
/// 提供通讯连接配置的应用层管理边界。
/// </summary>
public interface ICommunicationConfigurationCatalog {
    /// <summary>读取全部通讯连接的基础配置。</summary>
    Task<IReadOnlyList<CommunicationConnectionConfigInfoModel>> ListAsync(
        CancellationToken cancellationToken = default);

    /// <summary>读取全部通讯连接及其协议明细。</summary>
    Task<IReadOnlyList<CommunicationConnectionConfigInfoModel>> ListWithDetailsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>读取全部 TCP 协议配置。</summary>
    Task<IReadOnlyList<JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams.TcpConfigInfoModel>>
        ListTcpAsync(CancellationToken cancellationToken = default);

    /// <summary>新增通讯连接及其协议明细。</summary>
    Task<bool> AddAsync(
        CommunicationConnectionConfigInfoModel configuration,
        CancellationToken cancellationToken = default);

    /// <summary>更新通讯连接及其协议明细。</summary>
    Task<bool> UpdateAsync(
        CommunicationConnectionConfigInfoModel configuration,
        CancellationToken cancellationToken = default);

    /// <summary>按标识删除通讯连接。</summary>
    Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>使持久化通讯配置与给定快照保持同步。</summary>
    Task<bool> SyncAsync(
        IReadOnlyCollection<CommunicationConnectionConfigInfoModel> configurations,
        CancellationToken cancellationToken = default);
}
