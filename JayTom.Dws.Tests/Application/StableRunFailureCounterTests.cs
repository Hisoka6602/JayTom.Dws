using JayTom.Dws.Application.Workflows;
using JayTom.Dws.Tests.Testing;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证稳定运行窗口对连续失败退避计数的重置语义。</summary>
public sealed class StableRunFailureCounterTests
{
    /// <summary>短时连续故障必须持续累加。</summary>
    [Fact]
    public void Consecutive_failures_accumulate_before_stable_window()
    {
        ManualTimeProvider time = new();
        StableRunFailureCounter counter = new(TimeSpan.FromMinutes(5), time);

        counter.MarkRunStarted();
        time.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(1, counter.RegisterFailure());
        counter.MarkRunStarted();
        time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(2, counter.RegisterFailure());
    }

    /// <summary>达到稳定运行窗口后必须遗忘此前故障并从一次重新计数。</summary>
    [Fact]
    public void Stable_run_resets_previous_failures()
    {
        ManualTimeProvider time = new();
        StableRunFailureCounter counter = new(TimeSpan.FromMinutes(5), time);

        counter.MarkRunStarted();
        Assert.Equal(1, counter.RegisterFailure());
        counter.MarkRunStarted();
        time.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(1, counter.RegisterFailure());
        Assert.Equal(1, counter.Count);
    }
}
