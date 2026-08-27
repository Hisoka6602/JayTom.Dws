using System.Collections.Concurrent;
using JayTom.Dws.Application.Messaging;

namespace JayTom.Dws.Tests.Application;

/// <summary>
/// 验证应用消息总线的异步顺序、故障隔离与订阅生命周期。
/// </summary>
public sealed class EventAggregatorTests
{
    /// <summary>验证同一异步订阅者严格按发布顺序处理事件。</summary>
    [Fact]
    public async Task Async_subscription_preserves_publish_order()
    {
        var processed = new ConcurrentQueue<int>();
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = new SequentialAsyncEventHandler<int>(async value =>
        {
            await Task.Yield();
            processed.Enqueue(value);
            if (value == 3)
            {
                completed.TrySetResult();
            }
        });

        Assert.True(subscription.TryEnqueue(1));
        Assert.True(subscription.TryEnqueue(2));
        Assert.True(subscription.TryEnqueue(3));

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal([1, 2, 3], processed);
    }

    /// <summary>验证一个事件处理失败不会中断同一订阅者的后续事件。</summary>
    [Fact]
    public async Task Async_subscription_isolates_handler_failures()
    {
        var completed = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = new SequentialAsyncEventHandler<int>(value =>
        {
            if (value == 1)
            {
                throw new InvalidOperationException("expected test failure");
            }

            completed.TrySetResult(value);
            return Task.CompletedTask;
        });

        Assert.True(subscription.TryEnqueue(1));
        Assert.True(subscription.TryEnqueue(2));

        Assert.Equal(2, await completed.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    /// <summary>验证释放订阅后不会启动尚未处理的积压事件。</summary>
    [Fact]
    public async Task Disposing_async_subscription_discards_pending_events()
    {
        var firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var processed = new ConcurrentQueue<int>();
        var subscription = new SequentialAsyncEventHandler<int>(async value =>
        {
            firstStarted.TrySetResult();
            await releaseFirst.Task;
            processed.Enqueue(value);
        });

        Assert.True(subscription.TryEnqueue(1));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(subscription.TryEnqueue(2));
        subscription.Dispose();
        releaseFirst.TrySetResult();

        await Task.Delay(100);
        Assert.Equal([1], processed);
    }

    /// <summary>验证达到容量上限后明确拒绝新事件，而不静默扩容。</summary>
    [Fact]
    public async Task Async_subscription_applies_bounded_backpressure()
    {
        var firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = new SequentialAsyncEventHandler<int>(async _ =>
        {
            firstStarted.TrySetResult();
            await releaseFirst.Task;
        }, capacity: 1);

        Assert.True(subscription.TryEnqueue(1));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(subscription.TryEnqueue(2));
        Assert.False(subscription.TryEnqueue(3));
        releaseFirst.TrySetResult();
    }
}
