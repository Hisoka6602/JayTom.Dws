using JayTom.Dws.Abstractions.Observability;
using NLog;
using System;

namespace JayTom.Dws.Client.Observability;

/// <summary>为 NLog 提供统一关联字段和敏感信息脱敏。</summary>
internal static class SafeLoggerExtensions
{
    /// <summary>记录结构化且已脱敏的异常信息。</summary>
    internal static void ErrorSanitized(
        this Logger logger,
        Exception exception,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(exception);
        logger.Error(
            "操作 {Operation} 失败；关联标识 {CorrelationId}；异常 {ExceptionType}；消息 {ErrorMessage}",
            operation,
            CorrelationContext.CurrentValueText,
            exception.GetType().FullName,
            SensitiveDataRedactor.RedactMessage(exception.Message));
    }

    /// <summary>记录带统一操作名与关联标识的结构化信息。</summary>
    internal static void InfoOperation(this Logger logger, string operation, string state)
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.Info(
            "操作 {Operation} 状态 {State}；关联标识 {CorrelationId}",
            operation,
            state,
            CorrelationContext.CurrentValueText);
    }
}
