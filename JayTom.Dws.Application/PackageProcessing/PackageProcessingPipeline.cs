namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>按采集、匹配、完成的固定顺序编排包裹业务判定。</summary>
public sealed class PackageProcessingPipeline
{
    /// <summary>采集阶段。</summary>
    private readonly IPackageProcessingStage _acquisition;

    /// <summary>匹配阶段。</summary>
    private readonly IPackageProcessingStage _matching;

    /// <summary>完成阶段。</summary>
    private readonly IPackageProcessingStage _completion;

    /// <summary>创建并校验唯一的三阶段流水线。</summary>
    public PackageProcessingPipeline(IEnumerable<IPackageProcessingStage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);
        IPackageProcessingStage? acquisition = null;
        IPackageProcessingStage? matching = null;
        IPackageProcessingStage? completion = null;
        foreach (IPackageProcessingStage stage in stages)
        {
            switch (stage.Stage)
            {
                case PackageProcessingStage.Acquisition when acquisition is null:
                    acquisition = stage;
                    break;
                case PackageProcessingStage.Matching when matching is null:
                    matching = stage;
                    break;
                case PackageProcessingStage.Completion when completion is null:
                    completion = stage;
                    break;
                default:
                    throw new InvalidOperationException($"包裹阶段 {stage.Stage} 重复或无效。");
            }
        }

        _acquisition = acquisition ?? throw Missing(PackageProcessingStage.Acquisition);
        _matching = matching ?? throw Missing(PackageProcessingStage.Matching);
        _completion = completion ?? throw Missing(PackageProcessingStage.Completion);
    }

    /// <summary>按固定顺序运行流水线，并在首个拒绝阶段停止。</summary>
    public async ValueTask<PackageProcessingOutcome> ExecuteAsync(
        PackageProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PackageStageDecision decision = await _acquisition
            .EvaluateAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (!decision.Accepted)
        {
            return Rejected(PackageProcessingStage.Acquisition, decision);
        }

        decision = await _matching.EvaluateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!decision.Accepted)
        {
            return Rejected(PackageProcessingStage.Matching, decision);
        }

        decision = await _completion.EvaluateAsync(request, cancellationToken).ConfigureAwait(false);
        return decision.Accepted
            ? new PackageProcessingOutcome(true, PackageProcessingStage.Completion, string.Empty)
            : Rejected(PackageProcessingStage.Completion, decision);
    }

    /// <summary>创建缺失阶段异常。</summary>
    private static InvalidOperationException Missing(PackageProcessingStage stage) =>
        new($"缺少包裹处理阶段 {stage}。");

    /// <summary>创建流水线拒绝结果。</summary>
    private static PackageProcessingOutcome Rejected(
        PackageProcessingStage stage,
        PackageStageDecision decision) => new(false, stage, decision.Reason);
}
