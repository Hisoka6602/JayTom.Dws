namespace JayTom.Dws.Application.Workflows;

/// <summary>按稳定运行窗口维护连续失败次数，防止历史故障永久放大退避时间。</summary>
public sealed class StableRunFailureCounter
{
    /// <summary>判定一次运行已经稳定的最短持续时间。</summary>
    private readonly TimeSpan _stableRunDuration;

    /// <summary>提供可测试的单调时间戳。</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>当前运行开始时的单调时间戳。</summary>
    private long _runStartedTimestamp;

    /// <summary>指示当前是否已经登记运行开始时间，避免把合法的零时间戳当成空值。</summary>
    private int _hasRunStarted;

    /// <summary>当前连续失败次数。</summary>
    private int _consecutiveFailures;

    /// <summary>创建稳定运行失败计数器。</summary>
    public StableRunFailureCounter(
        TimeSpan stableRunDuration,
        TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            stableRunDuration,
            TimeSpan.Zero);
        _stableRunDuration = stableRunDuration;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>获取当前连续失败次数。</summary>
    public int Count => Volatile.Read(ref _consecutiveFailures);

    /// <summary>记录新一轮运行已经开始。</summary>
    public void MarkRunStarted()
    {
        Interlocked.Exchange(ref _runStartedTimestamp, _timeProvider.GetTimestamp());
        Volatile.Write(ref _hasRunStarted, 1);
    }

    /// <summary>登记运行失败；稳定窗口已满足时先清零历史失败，再计入本次故障。</summary>
    public int RegisterFailure()
    {
        bool hasRunStarted = Interlocked.Exchange(ref _hasRunStarted, 0) != 0;
        long started = Volatile.Read(ref _runStartedTimestamp);
        if (hasRunStarted && _timeProvider.GetElapsedTime(started) >= _stableRunDuration)
        {
            Interlocked.Exchange(ref _consecutiveFailures, 0);
        }

        return Interlocked.Increment(ref _consecutiveFailures);
    }
}
