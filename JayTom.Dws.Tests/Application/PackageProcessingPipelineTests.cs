using JayTom.Dws.Application.PackageProcessing;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证包裹处理三阶段流水线的顺序和短路规则。</summary>
public sealed class PackageProcessingPipelineTests
{
    /// <summary>完整包裹通过全部阶段。</summary>
    [Fact]
    public async Task Complete_snapshot_is_ready()
    {
        PackageProcessingOutcome outcome = await CreatePipeline().ExecuteAsync(CreateRequest());

        Assert.True(outcome.IsReady);
        Assert.Equal(PackageProcessingStage.Completion, outcome.LastStage);
    }

    /// <summary>无条码包裹在匹配阶段停止。</summary>
    [Fact]
    public async Task Missing_barcode_stops_at_matching()
    {
        PackageProcessingRequest request = CreateRequest() with { Barcode = null };

        PackageProcessingOutcome outcome = await CreatePipeline().ExecuteAsync(request);

        Assert.False(outcome.IsReady);
        Assert.Equal(PackageProcessingStage.Matching, outcome.LastStage);
    }

    /// <summary>无重量包裹在完成阶段停止。</summary>
    [Fact]
    public async Task Missing_measurement_stops_at_completion()
    {
        PackageProcessingRequest request = CreateRequest() with { Weight = null };

        PackageProcessingOutcome outcome = await CreatePipeline().ExecuteAsync(request);

        Assert.False(outcome.IsReady);
        Assert.Equal(PackageProcessingStage.Completion, outcome.LastStage);
    }

    /// <summary>无效采集记录在第一阶段停止。</summary>
    [Fact]
    public async Task Invalid_identity_stops_at_acquisition()
    {
        PackageProcessingRequest request = CreateRequest() with { PackageKey = 0 };

        PackageProcessingOutcome outcome = await CreatePipeline().ExecuteAsync(request);

        Assert.False(outcome.IsReady);
        Assert.Equal(PackageProcessingStage.Acquisition, outcome.LastStage);
    }

    /// <summary>创建包含唯一固定阶段的流水线。</summary>
    private static PackageProcessingPipeline CreatePipeline() => new(
        [new PackageAcquisitionStage(), new PackageMatchingStage(), new PackageCompletionStage()]);

    /// <summary>创建有效的包裹输入快照。</summary>
    private static PackageProcessingRequest CreateRequest() => new(
        1,
        DateTime.Now,
        "PKG-1",
        DateTime.Now,
        "scanner-1",
        1m,
        2m,
        3m,
        4m,
        24m);
}
