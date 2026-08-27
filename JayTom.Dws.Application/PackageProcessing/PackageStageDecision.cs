namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>单个包裹处理阶段的业务判定。</summary>
public sealed record PackageStageDecision(bool Accepted, string Reason)
{
    /// <summary>创建通过判定。</summary>
    public static PackageStageDecision Pass() => new(true, string.Empty);

    /// <summary>创建拒绝判定。</summary>
    public static PackageStageDecision Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new PackageStageDecision(false, reason);
    }
}
