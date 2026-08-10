using JayTom.Dws.Application.Resilience;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证无人值守工作流使用的指数退避边界。</summary>
public sealed class BoundedExponentialBackoffTests {
    /// <summary>验证连续失败会按二次幂增加延迟。</summary>
    [Fact]
    public void Delay_grows_exponentially() {
        var backoff = new BoundedExponentialBackoff(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30));

        Assert.Equal(TimeSpan.FromSeconds(1), backoff.GetDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(2), backoff.GetDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(4), backoff.GetDelay(3));
    }

    /// <summary>验证高失败次数不会超过最大退避时间或发生溢出。</summary>
    [Fact]
    public void Delay_is_capped_without_overflow() {
        var backoff = new BoundedExponentialBackoff(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMinutes(5));

        Assert.Equal(TimeSpan.FromMinutes(5), backoff.GetDelay(10));
        Assert.Equal(TimeSpan.FromMinutes(5), backoff.GetDelay(int.MaxValue));
    }

    /// <summary>验证失败次数必须为正数。</summary>
    [Fact]
    public void Delay_rejects_invalid_failure_counts() {
        var backoff = new BoundedExponentialBackoff(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2));

        Assert.Throws<ArgumentOutOfRangeException>(() => backoff.GetDelay(0));
    }
}
