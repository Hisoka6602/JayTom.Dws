namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 定义宿主加载插件前执行的清单兼容性校验。
/// </summary>
public interface IPluginManifestValidator {
    /// <summary>
    /// 根据当前宿主与契约版本校验插件清单。
    /// </summary>
    /// <param name="manifest">待校验的插件清单。</param>
    /// <param name="hostVersion">当前宿主版本。</param>
    /// <param name="supportedContractMajorVersion">宿主支持的契约主版本。</param>
    /// <returns>包含状态和原因的兼容性结果。</returns>
    PluginCompatibilityResult Validate(
        PluginManifest manifest,
        Version hostVersion,
        int supportedContractMajorVersion);
}
