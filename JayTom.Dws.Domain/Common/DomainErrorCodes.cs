namespace JayTom.Dws.Domain.Common;

/// <summary>
/// 定义跨进程和跨版本稳定的领域错误码。
/// </summary>
public static class DomainErrorCodes {
    /// <summary>包裹不存在。</summary>
    public const string PackageNotFound = "package.not_found";
    /// <summary>包裹状态转换无效。</summary>
    public const string PackageInvalidTransition = "package.invalid_transition";
    /// <summary>分拣规则不匹配。</summary>
    public const string SortingRuleNotMatched = "sorting.rule_not_matched";
    /// <summary>格口已锁定。</summary>
    public const string ExitLocked = "sorting.exit_locked";
    /// <summary>设备能力不受支持。</summary>
    public const string DeviceCapabilityMissing = "device.capability_missing";
    /// <summary>授权声明无效。</summary>
    public const string LicenseInvalid = "license.invalid";
    /// <summary>操作超过重试预算。</summary>
    public const string RetryBudgetExhausted = "resilience.retry_budget_exhausted";
}
