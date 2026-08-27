using System.Collections.Concurrent;
using System.Diagnostics;

namespace JayTom.Dws.Application.Workflows;

/// <summary>以单消费者异步循环按入队顺序执行工作，生产者入队时不会等待处理器。</summary>
/// <typeparam name="T">工作项类型。</typeparam>
public sealed class AsyncOrderedDispatcher<T> : IAsyncDisposable
{
    /// <summary>待处理工作队列。</summary>
    private readonly ConcurrentQueue<QueuedDispatcherItem<T>> _queue = new();

    /// <summary>合并连续写入的唤醒信号。</summary>
    private readonly SemaphoreSlim _workAvailable = new(0, 1);

    /// <summary>异步工作处理器。</summary>
    private readonly Func<T, Task> _handler;

    /// <summary>单项处理异常回调。</summary>
    private readonly Action<T, Exception>? _exceptionHandler;

    /// <summary>单消费者任务。</summary>
    private readonly Task _worker;

    /// <summary>尚未完成的工作项数量。</summary>
    private long _pendingCount;

    /// <summary>正在越过停止边界的生产者数量。</summary>
    private int _writersInFlight;

    /// <summary>已唤醒或正在排空时为一。</summary>
    private int _wakeScheduled;

    /// <summary>零表示运行，一表示停止中，二表示已释放。</summary>
    private int _disposeState;

    /// <summary>最大排队耗时，单位微秒。</summary>
    private long _maximumQueueDelayMicroseconds;

    /// <summary>最大处理耗时，单位微秒。</summary>
    private long _maximumHandlerDurationMicroseconds;

    /// <summary>创建异步顺序调度器。</summary>
    /// <param name="handler">异步工作处理器。</param>
    /// <param name="exceptionHandler">可选的异常回调。</param>
    public AsyncOrderedDispatcher(
        Func<T, Task> handler,
        Action<T, Exception>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
        _exceptionHandler = exceptionHandler;
        _worker = Task.Run(ProcessAsync);
    }

    /// <summary>获取尚未执行完成的工作项数量。</summary>
    public long PendingCount => Interlocked.Read(ref _pendingCount);

    /// <summary>读取并清零最大排队耗时。</summary>
    public long TakeMaximumQueueDelayMicroseconds() =>
        Interlocked.Exchange(ref _maximumQueueDelayMicroseconds, 0);

    /// <summary>读取并清零最大处理耗时。</summary>
    public long TakeMaximumHandlerDurationMicroseconds() =>
        Interlocked.Exchange(ref _maximumHandlerDurationMicroseconds, 0);

    /// <summary>尝试非阻塞地加入工作项。</summary>
    /// <param name="item">待处理工作项。</param>
    /// <returns>调度器仍在接收工作时返回真。</returns>
    public bool TryEnqueue(T item)
    {
        if (Volatile.Read(ref _disposeState) != 0)
        {
            return false;
        }

        Interlocked.Increment(ref _writersInFlight);
        if (Volatile.Read(ref _disposeState) != 0)
        {
            Interlocked.Decrement(ref _writersInFlight);
            return false;
        }

        try
        {
            _queue.Enqueue(new QueuedDispatcherItem<T>(item, Stopwatch.GetTimestamp()));
            Interlocked.Increment(ref _pendingCount);
        }
        finally
        {
            Interlocked.Decrement(ref _writersInFlight);
        }
        if (Interlocked.Exchange(ref _wakeScheduled, 1) == 0)
        {
            _workAvailable.Release();
        }

        return true;
    }

    /// <summary>按顺序排空队列，并在停止时处理完已经接收的工作。</summary>
    private async Task ProcessAsync()
    {
        while (true)
        {
            while (_queue.TryDequeue(out QueuedDispatcherItem<T> queuedItem))
            {
                long handlerStartedAt = Stopwatch.GetTimestamp();
                RecordMaximum(
                    ref _maximumQueueDelayMicroseconds,
                    ToMicroseconds(queuedItem.EnqueuedAtTimestamp, handlerStartedAt));
                try
                {
                    await _handler(queuedItem.Item);
                }
                catch (Exception exception)
                {
                    ObserveError(queuedItem.Item, exception);
                }
                finally
                {
                    RecordMaximum(
                        ref _maximumHandlerDurationMicroseconds,
                        ToMicroseconds(handlerStartedAt, Stopwatch.GetTimestamp()));
                    Interlocked.Decrement(ref _pendingCount);
                }
            }

            if (Volatile.Read(ref _disposeState) != 0 &&
                Volatile.Read(ref _writersInFlight) == 0 &&
                _queue.IsEmpty)
            {
                return;
            }

            Volatile.Write(ref _wakeScheduled, 0);
            if (!_queue.IsEmpty && Interlocked.Exchange(ref _wakeScheduled, 1) == 0)
            {
                continue;
            }

            await _workAvailable.WaitAsync();
        }
    }

    /// <summary>安全发布处理异常。</summary>
    private void ObserveError(T item, Exception exception)
    {
        try
        {
            _exceptionHandler?.Invoke(item, exception);
        }
        catch
        {
            // 异常观察器不得终止顺序消费者。
        }
    }

    /// <summary>把单调时钟差值转换为整数微秒。</summary>
    private static long ToMicroseconds(long startedAt, long completedAt) =>
        Math.Max(0L, completedAt - startedAt) * 1_000_000L / Stopwatch.Frequency;

    /// <summary>以无锁方式记录最大值。</summary>
    private static void RecordMaximum(ref long target, long value)
    {
        long current = Volatile.Read(ref target);
        while (value > current)
        {
            long observed = Interlocked.CompareExchange(ref target, value, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    /// <summary>停止接收新工作并异步等待已入队工作完成。</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposeState, 1, 0) == 0 &&
            Interlocked.Exchange(ref _wakeScheduled, 1) == 0)
        {
            _workAvailable.Release();
        }

        while (Volatile.Read(ref _writersInFlight) != 0)
        {
            await Task.Yield();
        }

        await _worker;
        if (Interlocked.Exchange(ref _disposeState, 2) != 2)
        {
            _workAvailable.Dispose();
        }
    }
}
