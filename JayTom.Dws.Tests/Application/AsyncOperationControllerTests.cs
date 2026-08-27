using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证异步命令状态与取消控制。</summary>
public sealed class AsyncOperationControllerTests
{
    /// <summary>运行期间拒绝重入，并在完成后恢复空闲状态。</summary>
    [Fact]
    public async Task TryRunAsync_rejects_reentry_and_restores_idle_state()
    {
        using AsyncOperationController controller = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<bool> states = [];
        controller.StateChanged += (_, _) => states.Add(controller.IsBusy);

        Task<bool> first = controller.TryRunAsync(async _ =>
        {
            entered.SetResult();
            await release.Task;
        });

        await entered.Task;
        bool second = await controller.TryRunAsync(_ => Task.CompletedTask);
        release.SetResult();

        Assert.True(await first);
        Assert.False(second);
        Assert.False(controller.IsBusy);
        Assert.Equal([true, false], states);
    }

    /// <summary>取消请求会传递到当前操作并安全结束。</summary>
    [Fact]
    public async Task Cancel_propagates_to_running_operation()
    {
        using AsyncOperationController controller = new();
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool> running = controller.TryRunAsync(async cancellationToken =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });

        await entered.Task;
        controller.Cancel();

        Assert.True(await running);
        Assert.False(controller.IsBusy);
    }
}
