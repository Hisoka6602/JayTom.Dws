namespace JayTom.Dws.Abstractions.Results;

/// <summary>
/// 用于分层边界且与传输协议无关的错误描述。
/// </summary>
public sealed record Error(string Code, string Message) {
    /// <summary>表示没有错误。</summary>
    public static readonly Error None = new(string.Empty, string.Empty);
    /// <summary>表示操作已取消。</summary>
    public static readonly Error Cancelled = new("operation.cancelled", "The operation was cancelled.");

    /// <summary>创建参数验证错误。</summary>
    public static Error Validation(string message) => new("validation.failed", message);

    /// <summary>创建未预期的操作错误。</summary>
    public static Error Unexpected(string message) => new("operation.failed", message);
}
