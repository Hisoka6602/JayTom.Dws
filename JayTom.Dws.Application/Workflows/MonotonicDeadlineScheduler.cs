using System.Diagnostics;

namespace JayTom.Dws.Application.Workflows;

/// <summary>使用单一高优先级线程按单调时钟执行可取消截止任务。</summary>
public sealed class MonotonicDeadlineScheduler : IDisposable
{
    /// <summary>保护截止时间优先队列。</summary>
    private readonly object _gate = new();
    /// <summary>在队首变化或服务停止时唤醒工作线程。</summary>
    private readonly AutoResetEvent _changed = new(false);
    /// <summary>按单调时钟截止时间保存任务。</summary>
    private readonly PriorityQueue<MonotonicScheduledItem, long> _queue = new();
    /// <summary>执行截止任务的独立线程。</summary>
    private readonly Thread _worker;
    /// <summary>零表示运行，一表示已经停止。</summary>
    private int _disposeState;

    /// <summary>创建独立于线程池的截止时间调度器。</summary>
    public MonotonicDeadlineScheduler(
        string workerName,
        ThreadPriority workerPriority = ThreadPriority.AboveNormal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerName);
        _worker = new Thread(Process)
        {
            IsBackground = true,
            Name = workerName,
            Priority = workerPriority
        };
        _worker.Start();
    }

    /// <summary>按相对时间安排一次任务；取消不会阻塞调用线程。</summary>
    public IDisposable Schedule(TimeSpan dueTime, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeState) != 0,
            this);
        var item = new MonotonicScheduledItem(callback);
        var dueTimestamp = AddDuration(Stopwatch.GetTimestamp(), dueTime);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposeState != 0, this);
            _queue.Enqueue(item, dueTimestamp);
        }
        _changed.Set();
        return item;
    }

    /// <summary>持续等待并执行已经到期且尚未取消的任务。</summary>
    private void Process()
    {
        while (Volatile.Read(ref _disposeState) == 0)
        {
            MonotonicScheduledItem? dueItem = null;
            var waitMilliseconds = Timeout.Infinite;
            lock (_gate)
            {
                while (_queue.TryPeek(out var item, out var dueTimestamp))
                {
                    if (item.IsCancelled)
                    {
                        _queue.Dequeue();
                        continue;
                    }

                    var remaining = dueTimestamp - Stopwatch.GetTimestamp();
                    if (remaining <= 0)
                    {
                        dueItem = _queue.Dequeue();
                    }
                    else
                    {
                        waitMilliseconds = ToCeilingMilliseconds(remaining);
                    }
                    break;
                }
            }

            if (dueItem is null)
            {
                _changed.WaitOne(waitMilliseconds);
                continue;
            }

            try
            {
                dueItem.TryInvoke();
            }
            catch
            {
                // 调度线程必须继续服务后续截止任务；业务回调自行报告异常。
            }
        }
    }

    /// <summary>把相对时间安全转换成单调时钟截止时间。</summary>
    private static long AddDuration(long timestamp, TimeSpan dueTime)
    {
        if (dueTime <= TimeSpan.Zero)
        {
            return timestamp;
        }
        var seconds = dueTime.Ticks / TimeSpan.TicksPerSecond;
        var remainder = dueTime.Ticks % TimeSpan.TicksPerSecond;
        var stopwatchTicks = checked(
            seconds * Stopwatch.Frequency +
            remainder * Stopwatch.Frequency / TimeSpan.TicksPerSecond);
        return checked(timestamp + stopwatchTicks);
    }

    /// <summary>把剩余单调时钟刻度向上换算为等待毫秒数。</summary>
    private static int ToCeilingMilliseconds(long stopwatchTicks)
    {
        var wholeSeconds = stopwatchTicks / Stopwatch.Frequency;
        if (wholeSeconds >= int.MaxValue / 1000L)
        {
            return int.MaxValue;
        }
        var remainder = stopwatchTicks % Stopwatch.Frequency;
        var partialMilliseconds =
            (remainder * 1000L + Stopwatch.Frequency - 1L) /
            Stopwatch.Frequency;
        return Math.Max(1, checked((int)(wholeSeconds * 1000L + partialMilliseconds)));
    }

    /// <summary>停止接收新任务并终止工作线程。</summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0)
        {
            return;
        }
        _changed.Set();
        if (Thread.CurrentThread != _worker)
        {
            _worker.Join();
        }
        lock (_gate)
        {
            while (_queue.TryDequeue(out var item, out _))
            {
                item.Dispose();
            }
        }
        _changed.Dispose();
    }

}
