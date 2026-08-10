using JayTom.Dws.Application.Workflows;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证工作项重试跟踪器的边界和并发语义。</summary>
public sealed class RetryAttemptTrackerTests {
    /// <summary>验证达到最大次数后拒绝继续重试。</summary>
    [Fact]
    public void Registration_stops_after_the_configured_attempt_limit() {
        var tracker = new RetryAttemptTracker(2);
        var workItem = new object();

        Assert.True(tracker.TryRegisterFailure(workItem, out var firstAttempt));
        Assert.True(tracker.TryRegisterFailure(workItem, out var secondAttempt));
        Assert.False(tracker.TryRegisterFailure(workItem, out var rejectedAttempt));
        Assert.Equal(1, firstAttempt);
        Assert.Equal(2, secondAttempt);
        Assert.Equal(3, rejectedAttempt);
    }

    /// <summary>验证清理后同一实例可以作为新工作重新计数。</summary>
    [Fact]
    public void Forget_resets_the_attempt_counter() {
        var tracker = new RetryAttemptTracker(1);
        var workItem = new object();
        tracker.TryRegisterFailure(workItem, out _);

        tracker.Forget(workItem);

        Assert.True(tracker.TryRegisterFailure(workItem, out var attempt));
        Assert.Equal(1, attempt);
    }

    /// <summary>验证并发登记不会丢失计数。</summary>
    [Fact]
    public void Concurrent_registration_uses_atomic_attempt_numbers() {
        const int operationCount = 64;
        var tracker = new RetryAttemptTracker(operationCount);
        var workItem = new object();
        var attempts = new int[operationCount];

        Parallel.For(0, operationCount, index => {
            Assert.True(tracker.TryRegisterFailure(workItem, out attempts[index]));
        });

        Assert.Equal(Enumerable.Range(1, operationCount), attempts.Order());
    }
}
