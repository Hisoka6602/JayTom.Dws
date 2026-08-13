using System.Collections.Concurrent;

namespace JayTom.Dws.Application.Workflows;

/// <summary>
/// 将生产线程与按顺序执行的工作完全隔离。写入只使用无界内存队列，
/// 不会等待消费者或执行用户代码；消费者固定在独立后台线程上，不与 API 共用线程池配额。
/// </summary>
/// <typeparam name="T">工作项类型。</typeparam>
public sealed class NonBlockingOrderedDispatcher<T> : IAsyncDisposable
{
    /// <summary>保存待处理工作项的无界内存队列。</summary>
    private readonly ConcurrentQueue<T> _queue = new();
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

    /// <summary>尚未执行完成的工作项数量。</summary>
    public long PendingCount => Interlocked.Read(ref _pendingCount);

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
            _queue.Enqueue(item);
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
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    _handler(item);
                }
                catch (Exception exception)
                {
                    try
                    {
                        _exceptionHandler?.Invoke(item, exception);
                    }
                    catch
                    {
                        // 错误上报不能终止关键工作队列。
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingCount);
                }
            }

            if (Volatile.Read(ref _disposeState) != 0 && _queue.IsEmpty)
            {
                return;
            }

            Volatile.Write(ref _wakeScheduled, 0);
            if (!_queue.IsEmpty)
            {
                Volatile.Write(ref _wakeScheduled, 1);
                continue;
            }

            _workAvailable.WaitOne();
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
