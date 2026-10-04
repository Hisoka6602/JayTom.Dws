using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证关键业务队列在突发流量下不会阻塞生产者或丢弃数据。</summary>
public sealed class LosslessWorkQueueTests
{
    /// <summary>确认超过旧容量阈值时所有工作仍能按顺序出队。</summary>
    [Fact]
    public void BurstBeyondLegacyCapacity_RemainsLossless()
    {
        var queue = new LosslessWorkQueue<int>();
        const int itemCount = 12_000;
        for (var value = 0; value < itemCount; value++)
        {
            Assert.True(queue.TryEnqueue(value));
        }
        Assert.Equal(itemCount, queue.Count);

        for (var expected = 0; expected < itemCount; expected++)
        {
            Assert.True(queue.TryDequeue(out var actual));
            Assert.Equal(expected, actual);
        }
        Assert.True(queue.IsEmpty);
        Assert.Equal(0, queue.Count);
    }

    /// <summary>并发生产者和单个异步消费者既不丢失工作，也不遗留错误积压读数。</summary>
    [Fact]
    public async Task Concurrent_producers_and_async_reads_keep_an_exact_backlog_count()
    {
        var queue = new LosslessWorkQueue<int>();
        const int producers = 4;
        const int perProducer = 5000;
        var seen = new HashSet<int>();
        var consumer = Task.Run(async () =>
        {
            for (var i = 0; i < producers * perProducer; i++)
            {
                Assert.True(seen.Add(await queue.DequeueAsync()));
                Assert.True(queue.Count >= 0);
            }
        });
        await Task.WhenAll(Enumerable.Range(0, producers).Select(producer => Task.Run(async () =>
        {
            for (var i = 0; i < perProducer; i++) await queue.EnqueueAsync(producer * perProducer + i);
        })));
        await consumer.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(producers * perProducer, seen.Count);
        Assert.Equal(0, queue.Count);
        Assert.False(queue.TryDequeue(out _));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await queue.EnqueueAsync(-1, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await queue.DequeueAsync(cancellation.Token));
        Assert.Equal(0, queue.Count);
        queue.TryEnqueue(1);
        queue.TryEnqueue(2);
        queue.Clear();
        Assert.Equal(0, queue.Count);
        Assert.True(queue.IsEmpty);
    }
}
