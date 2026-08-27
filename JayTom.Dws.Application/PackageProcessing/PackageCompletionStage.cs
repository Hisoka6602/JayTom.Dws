namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>验证称重和体积数据满足完成输出条件。</summary>
public sealed class PackageCompletionStage : IPackageProcessingStage
{
    /// <summary>完成阶段标识。</summary>
    public PackageProcessingStage Stage => PackageProcessingStage.Completion;

    /// <summary>验证重量和体积数据是否齐全。</summary>
    public ValueTask<PackageStageDecision> EvaluateAsync(
        PackageProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        bool complete = request.Weight is >= 0 &&
                        request.Length is >= 0 &&
                        request.Width is >= 0 &&
                        request.Height is >= 0 &&
                        request.Volume is >= 0;
        return ValueTask.FromResult(complete
            ? PackageStageDecision.Pass()
            : PackageStageDecision.Reject("包裹称重或体积数据尚未完成。"));
    }
}
