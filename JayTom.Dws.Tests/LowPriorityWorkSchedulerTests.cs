using JayTom.Dws.Abstractions.Threading;

namespace JayTom.Dws.Tests;

/// <summary>验证历史查询独立线程、资源上限、取消和异常恢复。</summary>
public sealed class LowPriorityWorkSchedulerTests
{
    /// <summary>调用方不会执行查询，异步等待后仍回到独立的查询线程。</summary>
    [Fact]
    public async Task Work_and_continuations_run_on_a_separate_cooperative_thread()
    {
        var scheduler = new LowPriorityWorkScheduler("QueryThreadTest");
        var caller = Environment.CurrentManagedThreadId;
        var result = await scheduler.RunAsync(async () =>
        {
            var before = Environment.CurrentManagedThreadId;
            await Task.Delay(15);
            return (before, after: Environment.CurrentManagedThreadId, Thread.CurrentThread.Priority);
        });
        Assert.NotEqual(caller, result.before);
        Assert.Equal(result.before, result.after);
        Assert.Equal(ThreadPriority.Normal, result.Priority);
    }

    /// <summary>慢操作只占一个后台槽位，超过上限的请求安全失败且可恢复。</summary>
    [Fact]
    public async Task Capacity_is_bounded_and_faults_do_not_stop_the_worker()
    {
        var scheduler = new LowPriorityWorkScheduler("QueryCapacityTest", 1);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = scheduler.RunAsync(() => release.Task);
        Assert.Equal(1, scheduler.PendingCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.RunAsync(() => 2));
        release.SetResult(1);
        Assert.Equal(1, await first);
        await Assert.ThrowsAsync<IOException>(() => scheduler.RunAsync(() => Task.FromException<int>(new IOException("test"))));
        Assert.Equal(3, await scheduler.RunAsync(() => 3));
        Assert.Equal(0, scheduler.PendingCount);
    }

    /// <summary>一千次替代查询立即释放排队名额，不依赖线程池处理取消的速度。</summary>
    [Fact]
    public async Task Replaced_queued_queries_release_capacity_immediately()
    {
        var scheduler = new LowPriorityWorkScheduler("QueryCancellationTest", 2);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = scheduler.RunAsync(async () => { started.SetResult(); return await release.Task; });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        for (var i = 0; i < 1000; i++)
        {
            using var cancellation = new CancellationTokenSource();
            var pending = scheduler.RunAsync(new Func<int>(() => throw new IOException("cancelled work must not run")), cancellation.Token);
            cancellation.Cancel();
            Assert.Equal(1, scheduler.PendingCount);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
        release.SetResult(1);
        await first;
        Assert.Equal(9, await scheduler.RunAsync(() => 9));
    }

    /// <summary>预先取消的请求完全不会占用后台资源。</summary>
    [Fact]
    public async Task Pre_cancelled_work_is_never_executed()
    {
        var scheduler = new LowPriorityWorkScheduler("PreCancelledQueryTest");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var executed = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scheduler.RunAsync(() => executed = true, cancellation.Token));
        Assert.False(executed);
        Assert.Equal(0, scheduler.PendingCount);
    }

    /// <summary>业务写入积压时历史请求不执行；取消和服务停止均会释放请求。</summary>
    [Fact]
    public async Task Persistence_backlog_pauses_reads_without_blocking_or_losing_admission()
    {
        long pending = 200;
        using var pressure = RealtimeWorkPressure.Register(() => Volatile.Read(ref pending));
        var scheduler = new LowPriorityWorkScheduler("SortingFirstQueryTest", 1);
        using var cancellation = new CancellationTokenSource();
        var executed = false;
        var read = scheduler.RunAsync(() => executed = true, cancellation.Token);
        await Task.Delay(60);
        Assert.False(read.IsCompleted);
        Assert.False(executed);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read.WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.Equal(0, scheduler.PendingCount);
        var next = scheduler.RunAsync(() => 42);
        await Task.Delay(60);
        Assert.False(next.IsCompleted);
        Volatile.Write(ref pending, 0);
        Assert.Equal(42, await next.WaitAsync(TimeSpan.FromSeconds(3)));

        Volatile.Write(ref pending, 200);
        var afterStop = scheduler.RunAsync(() => 43);
        await Task.Delay(60);
        Assert.False(afterStop.IsCompleted);
        pressure.Dispose();
        Assert.Equal(43, await afterStop.WaitAsync(TimeSpan.FromSeconds(3)));
    }
}
