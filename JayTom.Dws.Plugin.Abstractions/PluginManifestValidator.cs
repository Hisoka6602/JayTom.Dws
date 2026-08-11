namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 提供确定性的插件清单及版本兼容性校验。
/// </summary>
public sealed class PluginManifestValidator : IPluginManifestValidator {
    /// <summary>
    /// 根据宿主版本和契约主版本检查插件是否可以加载。
    /// </summary>
    /// <param name="manifest">待检查的插件清单。</param>
    /// <param name="hostVersion">当前宿主版本。</param>
    /// <param name="supportedContractMajorVersion">宿主支持的契约主版本。</param>
    /// <returns>包含兼容性状态与诊断说明的结果。</returns>
    public PluginCompatibilityResult Validate(
        PluginManifest manifest,
        Version hostVersion,
        int supportedContractMajorVersion) {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(hostVersion);

        if (string.IsNullOrWhiteSpace(manifest.PluginKey)
            || string.IsNullOrWhiteSpace(manifest.Name)
            || string.IsNullOrWhiteSpace(manifest.EntryPoint)
            || !Version.TryParse(manifest.Version, out _)
            || !Version.TryParse(manifest.MinimumHostVersion, out Version? minimumHostVersion)) {
            return CreateResult(
                PluginCompatibilityStatus.InvalidManifest,
                "插件清单缺少必要信息或版本格式无效。");
        }

        if (manifest.ContractMajorVersion != supportedContractMajorVersion) {
            return CreateResult(
                PluginCompatibilityStatus.ContractVersionMismatch,
                $"插件契约主版本 {manifest.ContractMajorVersion} 与宿主版本 {supportedContractMajorVersion} 不兼容。");
        }

        if (hostVersion < minimumHostVersion) {
            return CreateResult(
                PluginCompatibilityStatus.HostVersionTooLow,
                $"插件至少需要宿主版本 {minimumHostVersion}，当前版本为 {hostVersion}。");
        }

        return CreateResult(PluginCompatibilityStatus.Compatible, "插件与当前宿主兼容。");
    }

    /// <summary>
    /// 创建统一格式的兼容性检查结果。
    /// </summary>
    /// <param name="status">兼容性状态。</param>
    /// <param name="message">检查说明。</param>
    /// <returns>初始化完成的兼容性结果。</returns>
    private static PluginCompatibilityResult CreateResult(
        PluginCompatibilityStatus status,
        string message) {
        return new PluginCompatibilityResult {
            Status = status,
            Message = message
        };
    }
}
