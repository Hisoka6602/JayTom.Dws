using System.Diagnostics;

namespace JayTom.Dws.Legacy.Contracts.Packages;

/// <summary>
/// 使用单调时钟集中调度包裹生命周期截止任务。截止判断和业务回调由不同线程执行，
/// 慢业务回调不会推迟后续截止时间的判定。
/// </summary>
internal static class PackageTimerScheduler
{
    /// <summary>保护截止时间优先队列。</summary>
    private static readonly object Gate = new();
    /// <summary>截止队首变化时唤醒判定线程。</summary>
    private static readonly AutoResetEvent Changed = new(false);
    /// <summary>按单调时钟截止时间保存生命周期注册项。</summary>
    private static readonly PriorityQueue<
        (PackageScheduledCallback Registration, long Version),
        long> Queue = new();

    /// <summary>保护已经到期、等待执行的业务回调。</summary>
    private static readonly object CallbackGate = new();
    /// <summary>保持到期业务回调的执行顺序。</summary>
    private static readonly Queue<Action> CallbackQueue = new();
    /// <summary>有到期业务回调时唤醒执行线程。</summary>
    private static readonly AutoResetEvent CallbackAvailable = new(false);

    /// <summary>唯一的截止时间判定线程。</summary>
    private static readonly Thread Worker = StartWorker();
    /// <summary>唯一的生命周期业务回调线程。</summary>
    private static readonly Thread CallbackWorker = StartCallbackWorker();

    /// <summary>注册一次可取消的包裹生命周期回调。</summary>
    public static PackageScheduledCallback Schedule(TimeSpan dueTime, Action callback)
    {
        var registration = new PackageScheduledCallback(callback);
        registration.Change(dueTime);
        return registration;
    }

    /// <summary>启动不执行用户代码的截止时间判定线程。</summary>
    private static Thread StartWorker()
    {
        var worker = new Thread(ProcessDeadlines)
        {
            IsBackground = true,
            Name = "PackageDeadlines",
            Priority = ThreadPriority.AboveNormal
        };
        worker.Start();
        return worker;
    }

    /// <summary>启动按到期顺序执行生命周期业务回调的线程。</summary>
    private static Thread StartCallbackWorker()
    {
        var worker = new Thread(ProcessCallbacks)
        {
            IsBackground = true,
            Name = "PackageDeadlineCallbacks",
            Priority = ThreadPriority.AboveNormal
        };
        worker.Start();
        return worker;
    }

    /// <summary>持续取出到期注册项并转交给回调线程。</summary>
    private static void ProcessDeadlines()
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
                    goto Dispatch;
                }

                work = default;
            }

        Wait:
            Changed.WaitOne(waitMilliseconds);
            continue;

        Dispatch:
            if (work.Registration.TryBegin(work.Version))
            {
                lock (CallbackGate)
                {
                    CallbackQueue.Enqueue(work.Registration.Callback);
                }
                CallbackAvailable.Set();
            }
        }
    }

    /// <summary>持续执行已到期的业务回调并隔离单项异常。</summary>
    private static void ProcessCallbacks()
    {
        while (true)
        {
            Action? callback = null;
            lock (CallbackGate)
            {
                if (CallbackQueue.Count > 0)
                {
                    callback = CallbackQueue.Dequeue();
                }
            }

            if (callback is null)
            {
                CallbackAvailable.WaitOne();
                continue;
            }

            try
            {
                callback();
            }
            catch
            {
                // 单个业务回调不得终止生命周期回调线程。
            }
        }
    }

    /// <summary>按相对截止时间把指定版本的注册项加入优先队列。</summary>
    internal static void Enqueue(
        PackageScheduledCallback registration,
        long version,
        TimeSpan dueTime)
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

    /// <summary>通知判定线程重新读取最近截止项。</summary>
    internal static void NotifyChanged() => Changed.Set();

    /// <summary>把单调时钟刻度向上换算成等待毫秒数。</summary>
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
        return Math.Max(
            1,
            checked((int)(wholeSeconds * 1000L + partialMilliseconds)));
    }
}
