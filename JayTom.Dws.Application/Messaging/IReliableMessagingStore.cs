// DWS-COHESIVE-CONTRACTS: Outbox、Inbox 和死信端口共同定义可靠投递协议。
namespace JayTom.Dws.Application.Messaging;

/// <summary>提供事务 Outbox 事件写入和领取边界。</summary>
public interface IOutboxStore {
    /// <summary>在业务事务中追加事件。</summary>
    Task AppendAsync(
        IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken);

    /// <summary>按分区和序号领取待投递事件。</summary>
    Task<IReadOnlyList<IntegrationEventEnvelope>> ClaimPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken);

    /// <summary>标记事件投递成功。</summary>
    Task MarkPublishedAsync(Guid eventId, CancellationToken cancellationToken);
}

/// <summary>提供幂等 Inbox 收件边界。</summary>
public interface IInboxStore {
    /// <summary>原子登记事件；已处理时返回 false。</summary>
    Task<bool> TryBeginAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>标记事件处理成功。</summary>
    Task MarkCompletedAsync(Guid eventId, CancellationToken cancellationToken);
}

/// <summary>提供死信写入、查询和显式重放边界。</summary>
public interface IDeadLetterStore {
    /// <summary>保存失败事件。</summary>
    Task AddAsync(DeadLetterEnvelope envelope, CancellationToken cancellationToken);

    /// <summary>领取到达重放时间的失败事件。</summary>
    Task<IReadOnlyList<DeadLetterEnvelope>> ClaimReplayableAsync(
        int maximumCount,
        CancellationToken cancellationToken);
}
