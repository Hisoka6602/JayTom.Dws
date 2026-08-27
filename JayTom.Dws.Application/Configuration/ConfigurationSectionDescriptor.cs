namespace JayTom.Dws.Application.Configuration;

/// <summary>描述一个强类型配置节的稳定键、模块和对象类型。</summary>
/// <param name="Key">持久化配置键。</param>
/// <param name="Module">配置所属业务模块。</param>
/// <param name="SettingsType">配置对象类型。</param>
public sealed record ConfigurationSectionDescriptor(
    string Key,
    ConfigurationModule Module,
    Type SettingsType);
