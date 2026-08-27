using JayTom.Dws.Abstractions.Threading;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证同步释放入口中的非阻塞异步任务观察。</summary>
public sealed class TaskCleanupTests
{
    /// <summary>观察未完成任务不会阻塞调用线程，并会消费后续异常。</summary>
    [Fact]
    public async Task Observe_returns_immediately_and_reports_fault()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Exception> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        TaskCleanup.Observe(completion.Task, observed.SetResult);
        Assert.False(completion.Task.IsCompleted);

        InvalidOperationException expected = new("cleanup failed");
        completion.SetException(expected);

        Assert.Same(expected, await observed.Task);
    }
}
