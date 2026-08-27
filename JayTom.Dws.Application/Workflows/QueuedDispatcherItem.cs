namespace JayTom.Dws.Application.Workflows;

/// <summary>保存调度工作项及其单调时钟入队时间，用于无锁计算排队耗时。</summary>
/// <typeparam name="T">工作项类型。</typeparam>
internal readonly record struct QueuedDispatcherItem<T>(
    T Item,
    long EnqueuedAtTimestamp);
