namespace JayTom.Dws.Application.Resilience;

/// <summary>
/// 计算具有最大上限的指数退避时间，避免持续故障时形成紧密重试循环。
/// </summary>
public sealed class BoundedExponentialBackoff {
    /// <summary>使用初始延迟和最大延迟创建退避策略。</summary>
    public BoundedExponentialBackoff(TimeSpan initialDelay, TimeSpan maximumDelay) {
        if (initialDelay <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(initialDelay));
        }

        if (maximumDelay < initialDelay) {
            throw new ArgumentOutOfRangeException(nameof(maximumDelay));
        }

        InitialDelay = initialDelay;
        MaximumDelay = maximumDelay;
    }

    /// <summary>获取首次失败后的延迟。</summary>
    public TimeSpan InitialDelay { get; }

    /// <summary>获取退避时间上限。</summary>
    public TimeSpan MaximumDelay { get; }

    /// <summary>根据连续失败次数计算下一次重试延迟。</summary>
    public TimeSpan GetDelay(int consecutiveFailureCount) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(consecutiveFailureCount);

        var shift = Math.Min(consecutiveFailureCount - 1, 30);
        var multiplier = 1L << shift;
        var maximumTicks = MaximumDelay.Ticks;
        var delayTicks = InitialDelay.Ticks > maximumTicks / multiplier
            ? maximumTicks
            : InitialDelay.Ticks * multiplier;
        return TimeSpan.FromTicks(Math.Min(delayTicks, maximumTicks));
    }
}
