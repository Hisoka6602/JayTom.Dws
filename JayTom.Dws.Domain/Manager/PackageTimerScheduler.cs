using System.Diagnostics;

namespace JayTom.Dws.Domain.Manager;

/// <summary>使用单调时钟集中调度全部包裹生命周期截止任务，避免为每个包裹创建多个线程池定时器。</summary>
internal static class PackageTimerScheduler
{
    /// <summary>保护截止时间优先队列。</summary>
    private static readonly object Gate = new();
    /// <summary>在新增或取消截止项时唤醒调度线程。</summary>
    private static readonly AutoResetEvent Changed = new(false);
    /// <summary>按单调时钟截止时间保存全部包裹回调。</summary>
    private static readonly PriorityQueue<(PackageScheduledCallback Registration, long Version), long> Queue = new();
    /// <summary>持有唯一的生命周期调度线程。</summary>
    private static readonly Thread Worker = StartWorker();

    /// <summary>注册一次包裹生命周期截止回调。</summary>
    public static PackageScheduledCallback Schedule(TimeSpan dueTime, Action callback)
    {
        var registration = new PackageScheduledCallback(callback);
        registration.Change(dueTime);
        return registration;
    }

    /// <summary>创建集中调度线程。</summary>
    private static Thread StartWorker()
    {
        var worker = new Thread(Process)
        {
            IsBackground = true,
            Name = "PackageDeadlines",
            Priority = ThreadPriority.AboveNormal
        };
        worker.Start();
        return worker;
    }

    /// <summary>持续等待最近截止项并执行仍然有效的回调。</summary>
    private static void Process()
    {
        while (true)
        {
            (PackageScheduledCallback Registration, long Version) work;
            var waitMilliseconds = Timeout.Infinite;
            lock (Gate)
            {
                while (Queue.TryPeek(out var queued, out var dueTimestamp))
                {
                    if (!queued.Registration.IsCurrent(queued.Version))
                    {
                        Queue.Dequeue();
                        continue;
                    }

                    var remaining = dueTimestamp - Stopwatch.GetTimestamp();
                    if (remaining > 0)
                    {
                        waitMilliseconds = ToCeilingMilliseconds(remaining);
                        work = default;
                        goto Wait;
                    }

                    work = Queue.Dequeue();
                    goto Execute;
                }

                work = default;
            }

        Wait:
            Changed.WaitOne(waitMilliseconds);
            continue;

        Execute:
            if (work.Registration.TryBegin(work.Version))
            {
                try
                {
                    work.Registration.Callback();
                }
                catch
                {
                    // 关键队列负责上报业务回调异常；单个回调不得终止生命周期调度线程。
                }
            }
        }
    }

    /// <summary>把指定版本的注册项放入截止时间优先队列。</summary>
    internal static void Enqueue(PackageScheduledCallback registration, long version, TimeSpan dueTime)
    {
        var dueStopwatchTicks = dueTime <= TimeSpan.Zero
            ? 0L
            : checked(
                dueTime.Ticks / TimeSpan.TicksPerSecond * Stopwatch.Frequency +
                dueTime.Ticks % TimeSpan.TicksPerSecond * Stopwatch.Frequency /
                TimeSpan.TicksPerSecond);
        var dueTimestamp = Stopwatch.GetTimestamp() + dueStopwatchTicks;
        lock (Gate)
        {
            Queue.Enqueue((registration, version), dueTimestamp);
        }
        Changed.Set();
    }

    /// <summary>通知调度线程重新读取最近截止时间。</summary>
    internal static void NotifyChanged() => Changed.Set();

    /// <summary>以整数运算向上换算等待毫秒，避免长截止时间乘法溢出。</summary>
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
}
