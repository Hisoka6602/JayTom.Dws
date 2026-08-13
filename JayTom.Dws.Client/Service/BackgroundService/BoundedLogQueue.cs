using JayTom.Dws.Application.Workflows;
using System;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Client.Service.BackgroundService;

/// <summary>
/// 为同步事件发布路径提供非阻塞、无损的日志缓冲。保留原类型名以避免扩大调用方改动；
/// 日志写入慢时只增加后台积压，绝不反向阻塞指令线程，也不丢弃诊断证据。
/// </summary>
internal sealed class BoundedLogQueue<T> {
    /// <summary>实际保存日志项的无损队列。</summary>
    private readonly LosslessWorkQueue<T> _queue = new();

    /// <summary>获取当前队列是否没有待处理日志。</summary>
    public bool IsEmpty => _queue.IsEmpty;

    /// <summary>创建日志缓冲；容量参数仅为源兼容保留。</summary>
    public BoundedLogQueue(int capacity) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
    }

    /// <summary>非阻塞加入日志。</summary>
    public void Enqueue(T item) {
        _queue.TryEnqueue(item);
    }

    /// <summary>立即尝试读取一个日志项。</summary>
    public bool TryDequeue([MaybeNullWhen(false)] out T item) => _queue.TryDequeue(out item);

    /// <summary>无损队列始终不会产生容量丢弃。</summary>
    public long ConsumeDroppedCount() => 0;
}
