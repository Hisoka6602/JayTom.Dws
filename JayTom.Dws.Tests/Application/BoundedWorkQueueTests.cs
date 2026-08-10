using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证应用工作队列的顺序与取消语义。</summary>
public sealed class BoundedWorkQueueTests {
    /// <summary>验证队列按写入顺序读取工作项。</summary>
    [Fact]
    public async Task Queue_preserves_item_order() {
        var queue = new BoundedWorkQueue<int>(2);

        await queue.EnqueueAsync(10);
        await queue.EnqueueAsync(20);

        Assert.Equal(10, await queue.DequeueAsync());
        Assert.Equal(20, await queue.DequeueAsync());
    }

    /// <summary>验证等待读取时能够响应取消。</summary>
    [Fact]
    public async Task Queue_honors_cancellation_while_waiting() {
        var queue = new BoundedWorkQueue<int>(1);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await queue.DequeueAsync(cancellation.Token));
    }

    /// <summary>验证队列已满时同步写入会被原子拒绝。</summary>
    [Fact]
    public void Try_enqueue_rejects_items_after_capacity_is_reached() {
        var queue = new BoundedWorkQueue<int>(1);

        Assert.True(queue.TryEnqueue(10));
        Assert.False(queue.TryEnqueue(20));
        Assert.False(queue.IsEmpty);
        Assert.True(queue.TryDequeue(out var item));
        Assert.Equal(10, item);
        Assert.True(queue.IsEmpty);
    }

    /// <summary>验证清空操作会移除所有已排队工作项。</summary>
    [Fact]
    public void Clear_removes_all_queued_items() {
        var queue = new BoundedWorkQueue<int>(2);
        queue.TryEnqueue(10);
        queue.TryEnqueue(20);

        queue.Clear();

        Assert.True(queue.IsEmpty);
        Assert.False(queue.TryDequeue(out _));
    }
}
