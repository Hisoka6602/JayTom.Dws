namespace JayTom.Dws.Abstractions.Threading;

/// <summary>表示一次可幂等释放的信号量占用，取消等待时不会错误增加计数。</summary>
public sealed class SemaphoreLease : IDisposable
{
    /// <summary>当前持有的信号量；释放后原子置空。</summary>
    private SemaphoreSlim? _semaphore;

    /// <summary>保存已经成功进入的信号量。</summary>
    private SemaphoreLease(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    /// <summary>异步等待并取得信号量占用。</summary>
    /// <param name="semaphore">需要进入的信号量。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>成功取得且必须释放的占用句柄。</returns>
    public static async ValueTask<SemaphoreLease> EnterAsync(
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new SemaphoreLease(semaphore);
    }

    /// <summary>释放当前持有的信号量；重复调用不会重复增加计数。</summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}
