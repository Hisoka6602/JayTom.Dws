namespace JayTom.Dws.Tests.Testing;

/// <summary>为单调时间相关测试提供可手动推进的时间戳。</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    /// <summary>当前以 TimeSpan tick 表示的单调时间戳。</summary>
    private long _timestamp;

    /// <summary>声明时间戳每秒频率与 TimeSpan tick 一致。</summary>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <summary>获取当前单调时间戳。</summary>
    public override long GetTimestamp() => Volatile.Read(ref _timestamp);

    /// <summary>按指定正向时长推进单调时间。</summary>
    public void Advance(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        Interlocked.Add(ref _timestamp, duration.Ticks);
    }
}
