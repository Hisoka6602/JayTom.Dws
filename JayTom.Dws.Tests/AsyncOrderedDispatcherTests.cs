using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests;

/// <summary>验证异步顺序调度器的顺序、排空与故障隔离语义。</summary>
public sealed class AsyncOrderedDispatcherTests
{
    /// <summary>释放会等待已接收工作按顺序完成。</summary>
    [Fact]
    public async Task DisposeAsync_drains_items_in_order()
    {
        List<int> handled = [];
        AsyncOrderedDispatcher<int> dispatcher = new(async item =>
        {
            await Task.Yield();
            handled.Add(item);
        });

        Assert.True(dispatcher.TryEnqueue(1));
        Assert.True(dispatcher.TryEnqueue(2));
        Assert.True(dispatcher.TryEnqueue(3));
        await dispatcher.DisposeAsync();

        Assert.Equal([1, 2, 3], handled);
        Assert.Equal(0, dispatcher.PendingCount);
        Assert.False(dispatcher.TryEnqueue(4));
    }

    /// <summary>单项失败会被观察且不阻断后续工作。</summary>
    [Fact]
    public async Task Handler_failure_is_observed_and_next_item_runs()
    {
        List<int> handled = [];
        List<Exception> errors = [];
        AsyncOrderedDispatcher<int> dispatcher = new(
            item => item == 1
                ? Task.FromException(new InvalidOperationException("failed"))
                : Task.Run(() => handled.Add(item)),
            (_, exception) => errors.Add(exception));

        dispatcher.TryEnqueue(1);
        dispatcher.TryEnqueue(2);
        await dispatcher.DisposeAsync();

        Assert.True(errors.Count == 1);
        Assert.Equal([2], handled);
    }
}
