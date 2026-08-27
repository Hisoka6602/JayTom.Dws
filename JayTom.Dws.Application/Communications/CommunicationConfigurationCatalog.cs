using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Application.Communications;

/// <summary>
/// 使用持久化仓储实现通讯连接配置用例。
/// </summary>
public sealed class CommunicationConfigurationCatalog : ICommunicationConfigurationCatalog {
    /// <summary>持有通讯连接配置的持久化边界。</summary>
    private readonly ICommunicationConnectionConfigRepository _repository;

    /// <summary>持有 TCP 协议配置的只读持久化边界。</summary>
    private readonly JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams.ITcpConfigRepository
        _tcpRepository;

    /// <summary>创建通讯连接配置目录。</summary>
    public CommunicationConfigurationCatalog(
        ICommunicationConnectionConfigRepository repository,
        JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams.ITcpConfigRepository
            tcpRepository) {
        _repository = repository;
        _tcpRepository = tcpRepository;
    }

    /// <summary>读取全部通讯连接的基础配置。</summary>
    public async Task<IReadOnlyList<CommunicationConnectionConfigInfoModel>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.Select(item => item.Id > 0, item => item.Id, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>读取全部通讯连接及其协议明细。</summary>
    public async Task<IReadOnlyList<CommunicationConnectionConfigInfoModel>> ListWithDetailsAsync(
        CancellationToken cancellationToken = default) =>
        await _repository.CommunicationConnectionConfigItems(
                item => item.Id > 0,
                cancellationToken)
            .ConfigureAwait(false);

    /// <summary>读取全部 TCP 协议配置。</summary>
    public async Task<IReadOnlyList<JayTom.Dws.Models.LocalConf.PackageSortingConfig.ConnectionParams.TcpConfigInfoModel>>
        ListTcpAsync(CancellationToken cancellationToken = default) =>
        await _tcpRepository.Select(item => item.Id > 0, item => item.Id, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>新增通讯连接及其协议明细。</summary>
    public Task<bool> AddAsync(
        CommunicationConnectionConfigInfoModel configuration,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configuration);
        return _repository.InsertDetailAsync(configuration, cancellationToken);
    }

    /// <summary>更新通讯连接及其协议明细。</summary>
    public Task<bool> UpdateAsync(
        CommunicationConnectionConfigInfoModel configuration,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configuration);
        return _repository.UpdateDetailAsync(configuration, cancellationToken);
    }

    /// <summary>按标识删除通讯连接。</summary>
    public async Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default) {
        var configuration = await _repository.FirstOrDefault(
                item => item.Id == id,
                cancellationToken)
            .ConfigureAwait(false);
        return configuration is not null &&
               await _repository.Delete(configuration, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>使持久化通讯配置与给定快照保持同步。</summary>
    public Task<bool> SyncAsync(
        IReadOnlyCollection<CommunicationConnectionConfigInfoModel> configurations,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configurations);
        return _repository.SyncEntities([.. configurations], cancellationToken);
    }
}
