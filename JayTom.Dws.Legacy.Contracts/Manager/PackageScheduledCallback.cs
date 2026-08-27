namespace JayTom.Dws.Legacy.Contracts.Packages;

/// <summary>表示可取消、可重新安排的包裹生命周期回调注册。</summary>
internal sealed class PackageScheduledCallback : IDisposable
{
    /// <summary>保存当前安排版本，使旧队列项可以无锁失效。</summary>
    private long _version;
    /// <summary>记录注册是否已经取消。</summary>
    private int _disposed;

    /// <summary>创建指定业务回调的注册。</summary>
    public PackageScheduledCallback(Action callback) => Callback = callback;

    /// <summary>获取截止时需要执行的业务回调。</summary>
    internal Action Callback { get; }

    /// <summary>使旧截止项失效并安排新的截止时间。</summary>
    public void Change(TimeSpan dueTime)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }
        var version = Interlocked.Increment(ref _version);
        PackageTimerScheduler.Enqueue(this, version, dueTime);
    }

    /// <summary>判断队列项是否仍是当前有效版本。</summary>
    internal bool IsCurrent(long version) =>
        Volatile.Read(ref _disposed) == 0 && Volatile.Read(ref _version) == version;

    /// <summary>原子取得指定版本回调的一次执行权。</summary>
    internal bool TryBegin(long version) =>
        IsCurrent(version) && Interlocked.CompareExchange(ref _version, version + 1, version) == version;

    /// <summary>取消尚未执行的全部版本。</summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _disposed, 1);
        Interlocked.Increment(ref _version);
        PackageTimerScheduler.NotifyChanged();
    }
}
