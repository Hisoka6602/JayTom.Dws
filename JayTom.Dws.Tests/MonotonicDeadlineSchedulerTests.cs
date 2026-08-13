using System.Diagnostics;
using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests;

/// <summary>验证精确截止调度不会回退到粗粒度轮询或执行已取消任务。</summary>
public sealed class MonotonicDeadlineSchedulerTests
{
    /// <summary>确认较早截止任务能够抢占已经进入等待状态的较晚任务。</summary>
    [Fact]
    public async Task EarlierDeadline_InterruptsCurrentWait()
    {
        using var scheduler = new MonotonicDeadlineScheduler("DeadlineTest");
        using var completed = new SemaphoreSlim(0, 2);
        var executionOrder = new List<int>();
        var gate = new object();
        scheduler.Schedule(TimeSpan.FromMilliseconds(500), () =>
        {
            lock (gate)
            {
                executionOrder.Add(2);
            }
            completed.Release();
        });
        await Task.Delay(20);
        var stopwatch = Stopwatch.StartNew();
        scheduler.Schedule(TimeSpan.FromMilliseconds(30), () =>
        {
            lock (gate)
            {
                executionOrder.Add(1);
            }
            completed.Release();
        });

        Assert.True(await completed.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(250));
        lock (gate)
        {
            Assert.Equal(1, executionOrder[0]);
        }
    }

    /// <summary>确认取消截止任务后不会运行其业务回调。</summary>
    [Fact]
    public async Task CancelledDeadline_DoesNotExecute()
    {
        using var scheduler = new MonotonicDeadlineScheduler("DeadlineCancelTest");
        var invoked = 0;
        var registration = scheduler.Schedule(
            TimeSpan.FromMilliseconds(30),
            () => Interlocked.Increment(ref invoked));
        registration.Dispose();

        await Task.Delay(150);

        Assert.Equal(0, Volatile.Read(ref invoked));
    }
}
