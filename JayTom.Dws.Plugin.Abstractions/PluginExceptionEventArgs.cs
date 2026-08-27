namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 表示不跨程序集传递 Exception 实例的插件故障事件。
/// </summary>
public sealed class PluginExceptionEventArgs : EventArgs {
    /// <summary>创建结构化插件故障事件。</summary>
    public PluginExceptionEventArgs(
        string errorCode,
        string message,
        string? exceptionType,
        DateTimeOffset occurredAt) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
        ExceptionType = exceptionType;
        OccurredAt = occurredAt;
    }

    /// <summary>获取稳定错误码。</summary>
    public string ErrorCode { get; }

    /// <summary>获取已脱敏错误说明。</summary>
    public string Message { get; }

    /// <summary>获取可选异常类型名称，不包含异常对象和堆栈。</summary>
    public string? ExceptionType { get; }

    /// <summary>获取故障发生时间。</summary>
    public DateTimeOffset OccurredAt { get; }
}
