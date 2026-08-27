namespace JayTom.Dws.Application.Configuration;

/// <summary>定义由应用层拥有的强类型配置校验器。</summary>
public interface IConfigurationValidator
{
    /// <summary>获取该校验器支持的配置对象类型。</summary>
    Type SettingsType { get; }

    /// <summary>返回稳定、可展示的配置错误集合。</summary>
    IReadOnlyList<string> Validate(object settings);
}
