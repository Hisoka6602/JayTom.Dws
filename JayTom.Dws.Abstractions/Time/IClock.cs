namespace JayTom.Dws.Abstractions.Time;

/// <summary>
/// 为应用工作流提供可测试的本地时间。
/// </summary>
public interface IClock {
    /// <summary>
    /// 获取当前本地时间。
    /// </summary>
    DateTimeOffset Now { get; }
}
