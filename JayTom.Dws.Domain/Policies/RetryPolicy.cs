// DWS-COHESIVE-CONTRACTS: 重试预算和异常处置策略共同构成失败政策。
namespace JayTom.Dws.Domain.Policies;

/// <summary>
/// 定义有上限且可预测的领域重试预算。
/// </summary>
public sealed record RetryPolicy(int MaximumAttempts, TimeSpan InitialDelay, TimeSpan MaximumDelay) {
    /// <summary>创建并校验重试策略。</summary>
    public static RetryPolicy Create(
        int maximumAttempts,
        TimeSpan initialDelay,
        TimeSpan maximumDelay) {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        if (initialDelay < TimeSpan.Zero || maximumDelay < initialDelay) {
            throw new ArgumentOutOfRangeException(nameof(initialDelay));
        }

        return new RetryPolicy(maximumAttempts, initialDelay, maximumDelay);
    }

    /// <summary>计算指定尝试次数的指数退避时间。</summary>
    public TimeSpan DelayForAttempt(int attempt) {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);
        var delay = InitialDelay;
        for (var index = 1; index < attempt && delay < MaximumDelay; index++) {
            var remainingTicks = MaximumDelay.Ticks - delay.Ticks;
            delay = remainingTicks <= delay.Ticks
                ? MaximumDelay
                : TimeSpan.FromTicks(delay.Ticks * 2);
        }

        return delay > MaximumDelay ? MaximumDelay : delay;
    }
}

/// <summary>定义异常包裹的稳定处置动作。</summary>
public enum AbnormalPackageAction {
    /// <summary>进入人工处理。</summary>
    ManualReview,
    /// <summary>路由到备用格口。</summary>
    AlternateExit,
    /// <summary>丢弃当前测量并重试。</summary>
    RetryMeasurement,
    /// <summary>停止流水线。</summary>
    StopPipeline
}

/// <summary>将稳定异常码映射到处置动作。</summary>
public sealed class AbnormalPackagePolicy {
    private readonly IReadOnlyDictionary<string, AbnormalPackageAction> _actions;

    /// <summary>创建异常处置策略。</summary>
    public AbnormalPackagePolicy(
        IReadOnlyDictionary<string, AbnormalPackageAction> actions) {
        _actions = actions;
    }

    /// <summary>解析异常处置动作。</summary>
    public AbnormalPackageAction Resolve(string errorCode) =>
        _actions.TryGetValue(errorCode, out var action)
            ? action
            : AbnormalPackageAction.ManualReview;
}
