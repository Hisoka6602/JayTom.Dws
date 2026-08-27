using System.Diagnostics;

namespace JayTom.Dws.Abstractions.Observability;

/// <summary>在同步和异步调用链中保存统一关联标识。</summary>
public static class CorrelationContext
{
    /// <summary>当前异步调用上下文中的显式关联标识。</summary>
    private static readonly AsyncLocal<string?> CurrentValue = new();

    /// <summary>获取当前关联标识；存在活动追踪时优先使用 TraceId。</summary>
    public static string CurrentValueText =>
        Activity.Current is { } activity && activity.TraceId != default
            ? activity.TraceId.ToString()
            : CurrentValue.Value ?? string.Empty;

    /// <summary>开始一个可嵌套、可跨 await 传播的关联作用域。</summary>
    public static CorrelationScope Begin(string? correlationValue = null)
    {
        string previous = CurrentValue.Value ?? string.Empty;
        string current = string.IsNullOrWhiteSpace(correlationValue)
            ? Guid.NewGuid().ToString("N")
            : correlationValue.Trim();
        CurrentValue.Value = current;
        return new CorrelationScope(previous);
    }

    /// <summary>恢复外层关联标识。</summary>
    internal static void Restore(string previous) =>
        CurrentValue.Value = string.IsNullOrEmpty(previous) ? null : previous;
}
