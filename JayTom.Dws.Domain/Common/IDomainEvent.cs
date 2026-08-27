namespace JayTom.Dws.Domain.Common;

/// <summary>
/// 标记不依赖持久化模型的领域事件。
/// </summary>
public interface IDomainEvent {
    /// <summary>获取事件发生的本地业务时间。</summary>
    DateTimeOffset OccurredAt { get; }
}
