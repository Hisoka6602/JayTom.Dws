using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Legacy.Contracts.Dto;

namespace JayTom.Dws.Tests.Application;

/// <summary>验证模块化配置目录和应用层集中校验。</summary>
public sealed class ConfigurationValidationTests
{
    /// <summary>配置节必须拥有唯一键并分布到各业务模块。</summary>
    [Fact]
    public void Section_catalog_groups_unique_typed_settings_by_module()
    {
        IReadOnlyList<ConfigurationSectionDescriptor> sections = ConfigurationSectionCatalog.All;

        Assert.True(sections.Count == sections.Select(section => section.Key).Distinct().Count());
        foreach (ConfigurationModule module in Enum.GetValues<ConfigurationModule>())
        {
            Assert.NotEmpty(ConfigurationSectionCatalog.ForModule(module));
        }
        Assert.Contains(sections, section => section.SettingsType == typeof(WeightSettingsDto));
        Assert.Contains(sections, section => section.SettingsType == typeof(ImageSettingsDto));
    }

    /// <summary>无效称重范围必须在进入配置存储之前被拒绝。</summary>
    [Fact]
    public async Task Settings_access_rejects_invalid_weight_configuration()
    {
        InMemorySettingsStore store = new(new Dictionary<string, string>());
        var registry = new ConfigurationValidationRegistry([new WeightSettingsValidator()]);
        SettingsAccess access = new(store, registry);

        var result = await access.SaveAsync(
            "WeightSettings",
            new WeightSettingsDto
            {
                CommonWeight = new CommonWeightParams { MinWeight = 10, MaxWeight = 5 }
            });

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration.validation_failed", result.Error.Code);
        Assert.False(await store.AnyAsync());
    }

    /// <summary>有效称重配置通过同一校验注册表并正常持久化。</summary>
    [Fact]
    public async Task Settings_access_accepts_valid_weight_configuration()
    {
        InMemorySettingsStore store = new(new Dictionary<string, string>());
        var registry = new ConfigurationValidationRegistry([new WeightSettingsValidator()]);
        SettingsAccess access = new(store, registry);

        var result = await access.SaveAsync(
            "WeightSettings",
            new WeightSettingsDto
            {
                CommonWeight = new CommonWeightParams { MinWeight = 0, MaxWeight = 50 }
            });

        Assert.True(result.IsSuccess);
        Assert.True(await store.AnyAsync());
    }
}
