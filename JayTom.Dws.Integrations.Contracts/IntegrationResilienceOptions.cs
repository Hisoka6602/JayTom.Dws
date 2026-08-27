namespace JayTom.Dws.Integrations.Contracts;

/// <summary>定义外部 HTTP 集成统一使用的超时、重试与熔断参数。</summary>
public sealed record IntegrationResilienceOptions(
    TimeSpan RequestTimeout,
    int RetryAttempts,
    TimeSpan RetryDelay,
    int CircuitFailureThreshold,
    TimeSpan CircuitBreakDuration)
{
    /// <summary>获取适用于桌面长连接场景的默认策略。</summary>
    public static IntegrationResilienceOptions Default { get; } = new(
        TimeSpan.FromMinutes(2),
        2,
        TimeSpan.FromMilliseconds(100),
        5,
        TimeSpan.FromSeconds(30));

    /// <summary>验证所有策略参数均处于可执行范围。</summary>
    /// <exception cref="ArgumentOutOfRangeException">参数超出允许范围时抛出。</exception>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(RequestTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(RetryAttempts);
        if (RetryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryDelay));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(CircuitFailureThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(CircuitBreakDuration, TimeSpan.Zero);
    }
}
