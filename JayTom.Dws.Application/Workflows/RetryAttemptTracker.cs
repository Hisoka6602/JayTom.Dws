using System.Runtime.CompilerServices;

namespace JayTom.Dws.Application.Workflows;

/// <summary>
/// 以工作项实例为范围记录有限次数的并发安全重试状态。
/// </summary>
public sealed class RetryAttemptTracker {
    /// <summary>保存工作项实例与原子重试计数的弱引用关系。</summary>
    private readonly ConditionalWeakTable<object, RetryCounter> _attempts = new();

    /// <summary>使用允许的最大重试次数创建跟踪器。</summary>
    public RetryAttemptTracker(int maxAttempts) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);
        MaxAttempts = maxAttempts;
    }

    /// <summary>获取单个工作项允许的最大重试次数。</summary>
    public int MaxAttempts { get; }

    /// <summary>登记一次失败，并返回该工作项是否仍允许重试。</summary>
    public bool TryRegisterFailure(object workItem, out int attempt) {
        ArgumentNullException.ThrowIfNull(workItem);

        var counter = _attempts.GetValue(workItem, static _ => new RetryCounter());
        attempt = Interlocked.Increment(ref counter.Value);
        return attempt <= MaxAttempts;
    }

    /// <summary>在工作完成后主动清除重试状态。</summary>
    public void Forget(object workItem) {
        ArgumentNullException.ThrowIfNull(workItem);
        _attempts.Remove(workItem);
    }
}
