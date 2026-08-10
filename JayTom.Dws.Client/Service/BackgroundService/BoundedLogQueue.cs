using JayTom.Dws.Application.Workflows;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace JayTom.Dws.Client.Service.BackgroundService;

/// <summary>为同步事件发布路径提供非阻塞、有界的日志缓冲。</summary>
internal sealed class BoundedLogQueue<T> {
    /// <summary>实际保存日志项的通用有界队列。</summary>
    private readonly BoundedWorkQueue<T> _queue;
    /// <summary>自上次读取后因队列已满而丢弃的日志数量。</summary>
    private long _droppedCount;

    /// <summary>获取当前队列是否没有待处理日志。</summary>
    public bool IsEmpty => _queue.IsEmpty;

    /// <summary>使用指定容量创建日志缓冲。</summary>
    public BoundedLogQueue(int capacity) {
        _queue = new BoundedWorkQueue<T>(capacity);
    }

    /// <summary>非阻塞加入日志；队列满时记录丢弃数量。</summary>
    public void Enqueue(T item) {
        if (!_queue.TryEnqueue(item)) {
            Interlocked.Increment(ref _droppedCount);
        }
    }

    /// <summary>立即尝试读取一个日志项。</summary>
    public bool TryDequeue([MaybeNullWhen(false)] out T item) => _queue.TryDequeue(out item);

    /// <summary>读取并清零累计丢弃数量。</summary>
    public long ConsumeDroppedCount() => Interlocked.Exchange(ref _droppedCount, 0);
}
