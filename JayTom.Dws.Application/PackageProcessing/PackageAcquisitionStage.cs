namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>验证包裹采集记录的稳定标识和时间。</summary>
public sealed class PackageAcquisitionStage : IPackageProcessingStage
{
    /// <summary>采集阶段标识。</summary>
    public PackageProcessingStage Stage => PackageProcessingStage.Acquisition;

    /// <summary>验证采集标识和创建时间。</summary>
    public ValueTask<PackageStageDecision> EvaluateAsync(
        PackageProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        PackageStageDecision decision = request.PackageKey > 0 && request.CreatedAt != default
            ? PackageStageDecision.Pass()
            : PackageStageDecision.Reject("包裹采集标识或时间无效。");
        return ValueTask.FromResult(decision);
    }
}
