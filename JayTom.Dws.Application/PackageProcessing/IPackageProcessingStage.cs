namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>定义一个可独立测试的包裹业务处理阶段。</summary>
public interface IPackageProcessingStage
{
    /// <summary>当前阶段标识。</summary>
    PackageProcessingStage Stage { get; }

    /// <summary>对不可变输入快照执行阶段判定。</summary>
    ValueTask<PackageStageDecision> EvaluateAsync(
        PackageProcessingRequest request,
        CancellationToken cancellationToken = default);
}
