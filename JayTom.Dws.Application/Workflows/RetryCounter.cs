namespace JayTom.Dws.Application.Workflows;

/// <summary>保存单个工作项的原子重试计数。</summary>
internal sealed class RetryCounter {
    /// <summary>当前重试次数。</summary>
    internal int Value;
}
