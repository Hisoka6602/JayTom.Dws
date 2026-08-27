using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Dto.PackageExitLockDto;

namespace JayTom.Dws.Application.Configuration;

/// <summary>按业务模块集中登记配置节，替代散落的无类型字符串分类。</summary>
public static class ConfigurationSectionCatalog
{
    /// <summary>全部配置节的只读模块快照。</summary>
    private static readonly IReadOnlyList<ConfigurationSectionDescriptor> Sections =
        Array.AsReadOnly<ConfigurationSectionDescriptor>(
        [
            new("WeightSettings", ConfigurationModule.Device, typeof(WeightSettingsDto)),
            new("VolumeSettings", ConfigurationModule.Device, typeof(VolumeSettingsDto)),
            new("OcrSettings", ConfigurationModule.Device, typeof(OcrSettingsDto)),
            new("CreatePackageSettings", ConfigurationModule.Device, typeof(CreatePackageSettingsDto)),
            new("SortingMethodSettings", ConfigurationModule.Sorting, typeof(SortingMethodDto)),
            new("PackageExitLockSettings", ConfigurationModule.Sorting, typeof(PackageExitLockSettingsDto)),
            new("StackedPackageDetectionSettings", ConfigurationModule.Sorting, typeof(StackedPackageDetectionSettingsDto)),
            new("SaveImageSettings", ConfigurationModule.Output, typeof(ImageSettingsDto)),
            new("ResultOutputSettings", ConfigurationModule.Output, typeof(ResultOutputSettingsDto)),
            new("ApiSettings", ConfigurationModule.Integration, typeof(ApiSettingsDto)),
            new("CacheClearSettings", ConfigurationModule.Maintenance, typeof(CacheClearSettingsDto))
        ]);

    /// <summary>获取全部配置节描述。</summary>
    public static IReadOnlyList<ConfigurationSectionDescriptor> All => Sections;

    /// <summary>获取指定模块拥有的配置节。</summary>
    public static IReadOnlyList<ConfigurationSectionDescriptor> ForModule(ConfigurationModule module) =>
        Array.AsReadOnly(Sections.Where(section => section.Module == module).ToArray());
}
