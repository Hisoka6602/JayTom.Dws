namespace JayTom.Dws.Application.Configuration;

/// <summary>通过预计算类型索引集中分派所有配置校验器。</summary>
public sealed class ConfigurationValidationRegistry
{
    /// <summary>配置类型到校验器的不可变索引。</summary>
    private readonly IReadOnlyDictionary<Type, IConfigurationValidator> _validators;

    /// <summary>创建校验器注册表并拒绝重复类型。</summary>
    public ConfigurationValidationRegistry(IEnumerable<IConfigurationValidator> validators)
    {
        ArgumentNullException.ThrowIfNull(validators);
        var validatorLookup = new Dictionary<Type, IConfigurationValidator>();
        foreach (IConfigurationValidator validator in validators)
        {
            ArgumentNullException.ThrowIfNull(validator);
            validatorLookup.Add(validator.SettingsType, validator);
        }
        _validators = validatorLookup;
    }

    /// <summary>校验已知配置类型；未登记类型保持向后兼容并视为有效。</summary>
    public IReadOnlyList<string> Validate(object settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _validators.TryGetValue(settings.GetType(), out IConfigurationValidator? validator)
            ? validator.Validate(settings)
            : Array.Empty<string>();
    }
}
