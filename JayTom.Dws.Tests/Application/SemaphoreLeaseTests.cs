using JayTom.Dws.Abstractions.Threading;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证信号量占用句柄在取消和重复释放下保持计数正确。</summary>
public sealed class SemaphoreLeaseTests
{
    /// <summary>验证正常进入后重复释放只归还一个许可。</summary>
    [Fact]
    public async Task Lease_releases_exactly_once()
    {
        using var semaphore = new SemaphoreSlim(1, 1);
        SemaphoreLease lease = await SemaphoreLease.EnterAsync(semaphore);

        lease.Dispose();
        lease.Dispose();

        Assert.Equal(1, semaphore.CurrentCount);
    }

    /// <summary>验证等待取消不会创建占用，也不会增加信号量计数。</summary>
    [Fact]
    public async Task Cancelled_wait_does_not_release_unowned_permit()
    {
        using var semaphore = new SemaphoreSlim(0, 1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await SemaphoreLease.EnterAsync(semaphore, cancellation.Token));

        Assert.Equal(0, semaphore.CurrentCount);
    }
}
