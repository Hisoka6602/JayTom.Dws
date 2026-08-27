using System.Collections.Concurrent;
using System.Diagnostics;

namespace JayTom.Dws.Application.Workflows;

/// <summary>
/// 将生产线程与按顺序执行的工作完全隔离。写入只使用无界内存队列，
/// 不会等待消费者或执行用户代码；消费者固定在独立后台线程上，不与 API 共用线程池配额。
/// </summary>
/// <typeparam name="T">工作项类型。</typeparam>
public sealed class NonBlockingOrderedDispatcher<T> : IAsyncDisposable
{
    /// <summary>保存待处理工作项的无界内存队列。</summary>
    private readonly ConcurrentQueue<QueuedDispatcherItem<T>> _queue = new();
    /// <summary>保存必须抢在普通工作项之前执行的高优先级队列。</summary>
    private readonly ConcurrentQueue<QueuedDispatcherItem<T>> _priorityQueue = new();
    /// <summary>合并密集写入的唤醒信号，避免每个工作项都经过阻塞集合计数。</summary>
    private readonly AutoResetEvent _workAvailable = new(false);
    /// <summary>按顺序执行工作项的处理器。</summary>
    private readonly Action<T> _handler;
    /// <summary>隔离单项处理异常的可选回调。</summary>
    private readonly Action<T, Exception>? _exceptionHandler;
    /// <summary>运行在独立后台线程上的单消费者任务。</summary>
    private readonly Task _worker;
    /// <summary>尚未执行完成的工作项计数。</summary>
    private long _pendingCount;
    /// <summary>正在越过停机边界的生产者数量。</summary>
    private int _writersInFlight;
    /// <summary>零表示消费者可能休眠；一表示已唤醒或正在连续排空队列。</summary>
    private int _wakeScheduled;
    /// <summary>零表示接收工作，一表示停止中，二表示已经释放。</summary>
    private int _disposeState;
    /// <summary>自上次读取以来观察到的最大排队耗时，单位微秒。</summary>
    private long _maximumQueueDelayMicroseconds;
    /// <summary>自上次读取以来观察到的最大单项执行耗时，单位微秒。</summary>
    private long _maximumHandlerDurationMicroseconds;

    /// <summary>尚未执行完成的工作项数量。</summary>
    public long PendingCount => Interlocked.Read(ref _pendingCount);

    /// <summary>读取并清零自上次采样以来的最大排队耗时，单位微秒。</summary>
    public long TakeMaximumQueueDelayMicroseconds() =>
        Interlocked.Exchange(ref _maximumQueueDelayMicroseconds, 0);

    /// <summary>读取并清零自上次采样以来的最大单项执行耗时，单位微秒。</summary>
    public long TakeMaximumHandlerDurationMicroseconds() =>
        Interlocked.Exchange(ref _maximumHandlerDurationMicroseconds, 0);

    /// <summary>创建单消费者、严格保持写入顺序的非阻塞调度器。</summary>
    public NonBlockingOrderedDispatcher(
        Action<T> handler,
        Action<T, Exception>? exceptionHandler = null,
        string? workerName = null,
        ThreadPriority workerPriority = ThreadPriority.Normal)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
        _exceptionHandler = exceptionHandler;
        _worker = Task.Factory.StartNew(
            () =>
            {
                if (!string.IsNullOrWhiteSpace(workerName) &&
                    Thread.CurrentThread.Name is null)
                {
                    Thread.CurrentThread.Name = workerName;
                }
                Thread.CurrentThread.Priority = workerPriority;
                Process();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    /// <summary>
    /// 立即写入工作项。该方法不等待消费者；仅当调度器已经停止时返回
    /// <see langword="false"/>。
    /// </summary>
    public bool TryEnqueue(T item)
    {
        return TryEnqueueCore(item, false);
    }

    /// <summary>
    /// 立即写入高优先级工作项；消费者会先排空高优先级队列，同时保持各优先级内部的写入顺序。
    /// </summary>
    public bool TryEnqueuePriority(T item)
    {
        return TryEnqueueCore(item, true);
    }

    /// <summary>把工作项写入指定优先级队列且从不等待消费者。</summary>
    private bool TryEnqueueCore(T item, bool isPriority)
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
            Interlocked.Increment(ref _pendingCount);
            var queuedItem = new QueuedDispatcherItem<T>(
                item,
                Stopwatch.GetTimestamp());
            if (isPriority)
            {
                _priorityQueue.Enqueue(queuedItem);
            }
            else
            {
                _queue.Enqueue(queuedItem);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _writersInFlight);
        }

        // 一个活跃周期仅执行一次内核唤醒；消费者随后连续排空密集写入。
        if (Interlocked.Exchange(ref _wakeScheduled, 1) == 0)
        {
            _workAvailable.Set();
        }
        return true;
    }

    /// <summary>持续读取并按写入顺序执行工作项。</summary>
    private void Process()
    {
        while (true)
        {
            while (TryDequeue(out var queuedItem))
            {
                var handlerStartedAt = Stopwatch.GetTimestamp();
                RecordMaximum(
                    ref _maximumQueueDelayMicroseconds,
                    ToMicroseconds(queuedItem.EnqueuedAtTimestamp, handlerStartedAt));
                try
                {
                    _handler(queuedItem.Item);
                }
                catch (Exception exception)
                {
                    try
                    {
                        _exceptionHandler?.Invoke(queuedItem.Item, exception);
                    }
                    catch
                    {
                        // 错误上报不能终止关键工作队列。
                    }
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
                QueuesAreEmpty())
            {
                return;
            }

            Volatile.Write(ref _wakeScheduled, 0);
            if (!QueuesAreEmpty())
            {
                Volatile.Write(ref _wakeScheduled, 1);
                continue;
            }

            _workAvailable.WaitOne();
        }
    }

    /// <summary>先读取高优先级队列，再读取普通队列。</summary>
    private bool TryDequeue(out QueuedDispatcherItem<T> item)
    {
        return _priorityQueue.TryDequeue(out item) || _queue.TryDequeue(out item);
    }

    /// <summary>判断两个优先级队列是否均为空。</summary>
    private bool QueuesAreEmpty() => _priorityQueue.IsEmpty && _queue.IsEmpty;

    /// <summary>把两次单调时间戳的差值转换为整数微秒。</summary>
    private static long ToMicroseconds(long startedAt, long completedAt)
    {
        var elapsedTimestamp = Math.Max(0L, completedAt - startedAt);
        return elapsedTimestamp * 1_000_000L / Stopwatch.Frequency;
    }

    /// <summary>以无锁方式记录更大的耗时水位。</summary>
    private static void RecordMaximum(ref long target, long value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var observed = Interlocked.CompareExchange(ref target, value, current);
            if (observed == current)
            {
                return;
            }
            current = observed;
        }
    }

    /// <summary>停止接收新工作，并等待已经入队的工作按顺序执行完成。</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposeState, 1, 0) == 0)
        {
            var spinWait = new SpinWait();
            while (Volatile.Read(ref _writersInFlight) != 0)
            {
                spinWait.SpinOnce();
            }

            _workAvailable.Set();
        }

        await _worker.ConfigureAwait(false);
        if (Interlocked.Exchange(ref _disposeState, 2) != 2)
        {
            _workAvailable.Dispose();
        }
    }
}
