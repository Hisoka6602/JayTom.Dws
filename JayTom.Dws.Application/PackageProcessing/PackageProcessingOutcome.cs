namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>包裹流水线的最终处理结果。</summary>
public sealed record PackageProcessingOutcome(
    bool IsReady,
    PackageProcessingStage LastStage,
    string Reason);
