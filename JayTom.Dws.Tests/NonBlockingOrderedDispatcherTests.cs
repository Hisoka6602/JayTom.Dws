using System.Diagnostics;
using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests;

/// <summary>验证关键设备工作队列的非阻塞和顺序语义。</summary>
public sealed class NonBlockingOrderedDispatcherTests
{
    /// <summary>消费者被阻塞时，生产者仍应立即完成高密度入队。</summary>
    [Fact]
    public async Task TryEnqueue_DoesNotWaitForBlockedConsumer()
    {
        using var releaseConsumer = new ManualResetEvent(false);
        await using var dispatcher = new NonBlockingOrderedDispatcher<int>(item =>
        {
            if (item == 0)
            {
                releaseConsumer.WaitOne(TimeSpan.FromSeconds(5));
            }
        });

        Assert.True(dispatcher.TryEnqueue(0));
        var stopwatch = Stopwatch.StartNew();
        for (var index = 1; index <= 100_000; index++)
        {
            Assert.True(dispatcher.TryEnqueue(index));
        }
        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(2),
            $"生产者入队耗时过长:{stopwatch.Elapsed}");
        releaseConsumer.Set();
    }

    /// <summary>多个工作项必须严格按照写入顺序执行。</summary>
    [Fact]
    public async Task Consumer_PreservesEnqueueOrder()
    {
        const int itemCount = 10_000;
        var processed = new List<int>(itemCount);
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using var dispatcher = new NonBlockingOrderedDispatcher<int>(item =>
        {
            processed.Add(item);
            if (processed.Count == itemCount)
            {
                completed.SetResult();
            }
        });

        for (var index = 0; index < itemCount; index++)
        {
            Assert.True(dispatcher.TryEnqueue(index));
        }
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(Enumerable.Range(0, itemCount), processed);
    }

    /// <summary>并发生产结束后，释放必须排空全部工作且待处理计数归零。</summary>
    [Fact]
    public async Task DisposeAsync_DrainsConcurrentProducersAndResetsPendingCount()
    {
        const int producerCount = 8;
        const int itemsPerProducer = 5_000;
        var processedCount = 0;
        var dispatcher = new NonBlockingOrderedDispatcher<int>(_ =>
            Interlocked.Increment(ref processedCount));

        await Task.WhenAll(Enumerable.Range(0, producerCount).Select(producer =>
            Task.Factory.StartNew(
                () =>
                {
                    for (var index = 0; index < itemsPerProducer; index++)
                    {
                        Assert.True(dispatcher.TryEnqueue(producer * itemsPerProducer + index));
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)));
        await dispatcher.DisposeAsync();

        Assert.Equal(producerCount * itemsPerProducer, processedCount);
        Assert.Equal(0, dispatcher.PendingCount);
        Assert.False(dispatcher.TryEnqueue(-1));
    }
}
