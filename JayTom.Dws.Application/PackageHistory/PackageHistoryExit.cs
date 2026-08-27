namespace JayTom.Dws.Application.PackageHistory;

/// <summary>历史格口读模型。</summary>
public sealed record PackageHistoryExit(
    string TheoreticalExit,
    string PhysicalExit,
    long PhysicalExitId);
