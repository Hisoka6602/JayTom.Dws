using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JayTom.Dws.Abstractions.Observability;

/// <summary>集中定义 DWS 关键路径的追踪与指标。</summary>
public static class DwsDiagnostics
{
    /// <summary>稳定的遥测源名称。</summary>
    public const string SourceName = "JayTom.Dws";

    /// <summary>应用活动追踪源。</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);
    /// <summary>应用指标源。</summary>
    public static readonly Meter Meter = new(SourceName);
    /// <summary>已完成操作计数器。</summary>
    private static readonly Counter<long> CompletedOperations =
        Meter.CreateCounter<long>("dws.operations.completed");
    /// <summary>失败操作计数器。</summary>
    private static readonly Counter<long> FailedOperations =
        Meter.CreateCounter<long>("dws.operations.failed");
    /// <summary>操作耗时直方图。</summary>
    private static readonly Histogram<long> OperationDuration =
        Meter.CreateHistogram<long>("dws.operation.duration", "ms");

    /// <summary>开始带有关联标识的内部活动。</summary>
    public static Activity? StartActivity(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        string correlationValue = CorrelationContext.CurrentValueText;
        Activity? activity = ActivitySource.StartActivity(operationName, ActivityKind.Internal);
        activity?.SetTag("correlation.id", correlationValue);
        return activity;
    }

    /// <summary>记录一次关键路径操作的成功状态与耗时。</summary>
    public static void RecordOperation(string operationName, bool succeeded, TimeSpan elapsed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        var tags = new TagList { { "operation.name", operationName } };
        if (succeeded)
        {
            CompletedOperations.Add(1, tags);
        }
        else
        {
            FailedOperations.Add(1, tags);
        }
        OperationDuration.Record((long)elapsed.TotalMilliseconds, tags);
    }
}
