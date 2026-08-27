namespace JayTom.Dws.Tests.TestDoubles;

/// <summary>
/// 为时间相关单元测试提供固定且可预测的当前时刻。
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    /// <summary>固定的协调世界时。</summary>
    private readonly DateTimeOffset _utcNow;

    /// <summary>创建固定时钟。</summary>
    /// <param name="fixedTime">固定时刻。</param>
    public FixedTimeProvider(DateTimeOffset fixedTime) => _utcNow = fixedTime.ToOffset(TimeSpan.Zero);

    /// <summary>读取固定的协调世界时。</summary>
    public override DateTimeOffset GetUtcNow() => _utcNow;
}
