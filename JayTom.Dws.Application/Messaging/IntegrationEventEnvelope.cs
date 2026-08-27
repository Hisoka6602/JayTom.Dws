// DWS-COHESIVE-CONTRACTS: 正常事件与死信信封共享同一版本化协议。
namespace JayTom.Dws.Application.Messaging;

/// <summary>表示具有版本、分区、关联和幂等语义的集成事件。</summary>
public sealed record IntegrationEventEnvelope(
    Guid EventId,
    string EventType,
    int Version,
    string PartitionKey,
    long PartitionSequence,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string Payload);

/// <summary>表示死信及其可重放元数据。</summary>
public sealed record DeadLetterEnvelope(
    IntegrationEventEnvelope Event,
    string ErrorCode,
    string ErrorMessage,
    int DeliveryAttempts,
    DateTimeOffset FailedAt,
    DateTimeOffset? ReplayAfter);
