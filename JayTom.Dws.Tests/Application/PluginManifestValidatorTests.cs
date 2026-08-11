using JayTom.Dws.Plugin.Contracts;

namespace JayTom.Dws.Tests.Application;

/// <summary>
/// 验证插件清单的宿主与契约版本准入规则。
/// </summary>
public sealed class PluginManifestValidatorTests {
    /// <summary>
    /// 验证有效清单可通过兼容性检查。
    /// </summary>
    [Fact]
    public void Validate_CompatibleManifest_ReturnsCompatible() {
        PluginCompatibilityResult result = new PluginManifestValidator().Validate(
            CreateManifest(),
            new Version(2, 1),
            1);

        Assert.True(result.IsCompatible);
        Assert.Equal(PluginCompatibilityStatus.Compatible, result.Status);
    }

    /// <summary>
    /// 验证宿主版本过低时拒绝插件。
    /// </summary>
    [Fact]
    public void Validate_HostVersionTooLow_ReturnsRejectedStatus() {
        PluginCompatibilityResult result = new PluginManifestValidator().Validate(
            CreateManifest(minimumHostVersion: "3.0"),
            new Version(2, 1),
            1);

        Assert.Equal(PluginCompatibilityStatus.HostVersionTooLow, result.Status);
    }

    /// <summary>
    /// 验证契约主版本不匹配时拒绝插件。
    /// </summary>
    [Fact]
    public void Validate_ContractVersionMismatch_ReturnsRejectedStatus() {
        PluginCompatibilityResult result = new PluginManifestValidator().Validate(
            CreateManifest(contractMajorVersion: 2),
            new Version(2, 1),
            1);

        Assert.Equal(PluginCompatibilityStatus.ContractVersionMismatch, result.Status);
    }

    /// <summary>
    /// 创建可按需覆盖版本信息的有效测试清单。
    /// </summary>
    /// <param name="minimumHostVersion">最低宿主版本。</param>
    /// <param name="contractMajorVersion">契约主版本。</param>
    /// <returns>初始化完成的插件清单。</returns>
    private static PluginManifest CreateManifest(
        string minimumHostVersion = "2.0",
        int contractMajorVersion = 1) {
        return new PluginManifest {
            PluginKey = "jaytom.sample",
            Name = "示例插件",
            Version = "1.2.0",
            MinimumHostVersion = minimumHostVersion,
            ContractMajorVersion = contractMajorVersion,
            EntryPoint = "Sample.Plugin, Sample.Plugin"
        };
    }
}
