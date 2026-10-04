namespace JayTom.Dws.Legacy.Contracts.Packages;

/// <summary>条码未能绑定到运行包裹的具体原因。</summary>
public enum BarcodeAssignmentRejectionReason
{
    NoUnassignedPackage,
    InvalidAssignmentWindow,
    ObservedBeforePackageCreation,
    BelowMinimumInterval,
    AboveMaximumInterval,
    EmptyPackageExpiredAtObservation,
    EmptyPackageExpiredWhileQueued,
    /// <summary>结果所属的原小车已经移除或赋值，禁止转交下一辆车。</summary>
    OriginalPackageUnavailable
}
