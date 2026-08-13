using System.Collections.Concurrent;

namespace JayTom.Dws.Camera.Concurrency;

/// <summary>
/// 使用独立长驻线程按写入顺序处理相机帧；生产者只执行无等待入队，既不占用线程池配额，也不丢失扫码帧。
/// </summary>
/// <typeparam name="T">相机工作项类型。</typeparam>
internal sealed class LosslessOrderedDispatcher<T> : IDisposable {
    /// <summary>保存待处理相机工作项的无界队列。</summary>
    private readonly ConcurrentQueue<T> _queue = new();
    /// <summary>合并密集帧写入的线程唤醒，避免逐帧阻塞集合同步。</summary>
    private readonly AutoResetEvent _workAvailable = new(false);
    /// <summary>在独立线程上执行的工作项处理器。</summary>
    private readonly Action<T> _handler;
    /// <summary>隔离单项处理异常的回调。</summary>
    private readonly Action<T, Exception>? _exceptionHandler;
    /// <summary>按顺序消费相机工作项的独立后台线程。</summary>
    private readonly Thread _worker;
    /// <summary>零表示接收入队，一表示正在停止，二表示已经释放。</summary>
    private int _disposeState;
    /// <summary>当前尚未处理完成的工作项数量。</summary>
    private long _pendingCount;
    /// <summary>正在越过停止边界的 SDK 回调数量。</summary>
    private int _writersInFlight;
    /// <summary>零表示工作线程可能休眠；一表示已唤醒或正在连续排空帧。</summary>
    private int _wakeScheduled;

    /// <summary>获取当前尚未处理完成的工作项数量。</summary>
    public long PendingCount => Interlocked.Read(ref _pendingCount);

    /// <summary>创建不占用普通线程池工作线程的相机帧调度器。</summary>
    public LosslessOrderedDispatcher(
        Action<T> handler,
        Action<T, Exception>? exceptionHandler = null) {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
        _exceptionHandler = exceptionHandler;
        _worker = new Thread(Process) {
            IsBackground = true,
            Name = $"CameraFrame-{typeof(T).Name}",
            Priority = ThreadPriority.AboveNormal
        };
        _worker.Start();
    }

    /// <summary>立即写入工作项；仅在调度器停止接收后返回 <see langword="false"/>。</summary>
    public bool TryEnqueue(T item) {
        if (Volatile.Read(ref _disposeState) != 0) {
            return false;
        }

        Interlocked.Increment(ref _writersInFlight);
        if (Volatile.Read(ref _disposeState) != 0) {
            Interlocked.Decrement(ref _writersInFlight);
            return false;
        }

        try {
            Interlocked.Increment(ref _pendingCount);
            _queue.Enqueue(item);
        }
        finally {
            Interlocked.Decrement(ref _writersInFlight);
        }

        // 一个活跃周期仅唤醒一次长驻线程，避免逐帧进入内核。
        if (Interlocked.Exchange(ref _wakeScheduled, 1) == 0) {
            _workAvailable.Set();
        }
        return true;
    }

    /// <summary>持续读取并按生产顺序执行相机工作项。</summary>
    private void Process() {
        while (true) {
            while (_queue.TryDequeue(out var item)) {
                try {
                    _handler(item);
                }
                catch (Exception exception) {
                    try {
                        _exceptionHandler?.Invoke(item, exception);
                    }
                    catch {
                        // 错误上报失败不能终止相机帧处理线程。
                    }
                }
                finally {
                    Interlocked.Decrement(ref _pendingCount);
                }
            }

            if (Volatile.Read(ref _disposeState) != 0 && _queue.IsEmpty) {
                return;
            }

            Volatile.Write(ref _wakeScheduled, 0);
            if (!_queue.IsEmpty) {
                Volatile.Write(ref _wakeScheduled, 1);
                continue;
            }

            _workAvailable.WaitOne();
        }
    }

    /// <summary>停止接收新工作项，并等待已经入队的帧全部按顺序处理完成。</summary>
    public void Dispose() {
        if (Interlocked.CompareExchange(ref _disposeState, 1, 0) == 0) {
            var spinWait = new SpinWait();
            while (Volatile.Read(ref _writersInFlight) != 0) {
                spinWait.SpinOnce();
            }

            _workAvailable.Set();
        }

        if (Thread.CurrentThread != _worker) {
            _worker.Join();
        }
        if (Interlocked.Exchange(ref _disposeState, 2) != 2) {
            _workAvailable.Dispose();
        }
    }
}
