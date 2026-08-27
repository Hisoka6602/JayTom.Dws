using System.Diagnostics;
using System.Diagnostics.Metrics;
using JayTom.Dws.Abstractions.Observability;

namespace JayTom.Dws.Tests.Observability;

/// <summary>验证关联标识、脱敏、结构化字段与关键路径指标契约。</summary>
public sealed class ObservabilityContractTests
{
    /// <summary>关联标识必须跨异步调用传播并在嵌套作用域释放后恢复。</summary>
    [Fact]
    public async Task Correlation_value_flows_across_await_and_restores_outer_scope()
    {
        using CorrelationScope outer = CorrelationContext.Begin("outer-correlation");
        Assert.Equal("outer-correlation", CorrelationContext.CurrentValueText);

        await Task.Yield();
        Assert.Equal("outer-correlation", CorrelationContext.CurrentValueText);

        using (CorrelationContext.Begin("inner-correlation"))
        {
            Assert.Equal("inner-correlation", CorrelationContext.CurrentValueText);
        }
        Assert.Equal("outer-correlation", CorrelationContext.CurrentValueText);
    }

    /// <summary>凭据字段和消息片段必须统一替换且普通业务文本保持原样。</summary>
    [Fact]
    public void Sensitive_data_is_redacted_by_key_and_message_pattern()
    {
        Assert.Equal(
            SensitiveDataRedactor.RedactedValue,
            SensitiveDataRedactor.Redact("Password", "plain-text"));
        string redacted = SensitiveDataRedactor.RedactMessage(
            "request failed token=secret-token route=/sorting");

        Assert.DoesNotContain("secret-token", redacted, StringComparison.Ordinal);
        Assert.Contains(SensitiveDataRedactor.RedactedValue, redacted, StringComparison.Ordinal);
        Assert.Contains("route=/sorting", redacted, StringComparison.Ordinal);
    }

    /// <summary>指标 API 必须发出成功计数、失败计数和耗时三个稳定指标。</summary>
    [Fact]
    public void Diagnostics_records_operation_metrics()
    {
        var names = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == DwsDiagnostics.SourceName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
            names.Add(instrument.Name));
        listener.Start();

        DwsDiagnostics.RecordOperation("test.success", true, TimeSpan.FromMilliseconds(5));
        DwsDiagnostics.RecordOperation("test.failure", false, TimeSpan.FromMilliseconds(7));

        Assert.Contains("dws.operations.completed", names);
        Assert.Contains("dws.operations.failed", names);
        Assert.Contains("dws.operation.duration", names);
    }

    /// <summary>活动源存在监听器时必须把关联标识写入追踪标签。</summary>
    [Fact]
    public void Activity_contains_correlation_tag()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DwsDiagnostics.SourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using CorrelationScope correlation = CorrelationContext.Begin("trace-correlation");
        using Activity? activity = DwsDiagnostics.StartActivity("test.activity");

        Assert.NotNull(activity);
        Assert.Equal("trace-correlation", activity.GetTagItem("correlation.id"));
    }
}
