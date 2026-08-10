using JayTom.Dws.Abstractions.Time;

namespace JayTom.Dws.Application.Time;

/// <summary>
/// 使用系统时钟提供当前本地时间。
/// </summary>
public sealed class SystemClock : IClock {
    /// <summary>获取当前本地系统时间。</summary>
    public DateTimeOffset Now => DateTimeOffset.Now;
}
