namespace JayTom.Dws.Application.Events;

/// <summary>表示应用处理状态发生变化。</summary>
public sealed class ApplicationStatusChanged
{
    /// <summary>获取新的应用状态。</summary>
    public ApplicationStatus Status { get; init; }
}
