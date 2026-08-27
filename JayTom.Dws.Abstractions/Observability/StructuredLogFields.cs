namespace JayTom.Dws.Abstractions.Observability;

/// <summary>跨模块共享的结构化日志字段名称。</summary>
public static class StructuredLogFields
{
    /// <summary>调用链关联标识。</summary>
    public const string CorrelationKey = "CorrelationId";
    /// <summary>业务或技术操作名称。</summary>
    public const string Operation = "Operation";
    /// <summary>稳定错误代码。</summary>
    public const string ErrorCode = "ErrorCode";
    /// <summary>异常类型。</summary>
    public const string ExceptionType = "ExceptionType";
    /// <summary>已脱敏错误信息。</summary>
    public const string ErrorMessage = "ErrorMessage";
}
