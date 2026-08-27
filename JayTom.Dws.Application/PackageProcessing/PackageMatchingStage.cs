namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>验证包裹已匹配到有效条码身份。</summary>
public sealed class PackageMatchingStage : IPackageProcessingStage
{
    /// <summary>匹配阶段标识。</summary>
    public PackageProcessingStage Stage => PackageProcessingStage.Matching;

    /// <summary>验证条码身份是否已匹配。</summary>
    public ValueTask<PackageStageDecision> EvaluateAsync(
        PackageProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        PackageStageDecision decision = !string.IsNullOrWhiteSpace(request.Barcode)
            ? PackageStageDecision.Pass()
            : PackageStageDecision.Reject("包裹尚未匹配到条码。");
        return ValueTask.FromResult(decision);
    }
}
