using JayTom.Dws.Abstractions.Devices;
using JayTom.Dws.Legacy.Contracts.Dto;

namespace JayTom.Dws.Application.Configuration;

/// <summary>集中校验称重配置的范围和通讯参数。</summary>
public sealed class WeightSettingsValidator : IConfigurationValidator
{
    /// <summary>获取称重配置类型。</summary>
    public Type SettingsType => typeof(WeightSettingsDto);

    /// <summary>校验重量范围、采样和串口基础参数。</summary>
    public IReadOnlyList<string> Validate(object settings)
    {
        var weight = (WeightSettingsDto)settings;
        var errors = new List<string>();
        if (weight.CommonWeight.MinWeight < 0)
        {
            errors.Add("最小重量不能小于零。");
        }
        if (weight.CommonWeight.MaxWeight <= weight.CommonWeight.MinWeight)
        {
            errors.Add("最大重量必须大于最小重量。");
        }
        if (weight.Mode != WeightMode.None &&
            weight.ScaleCommunicationMode == ScaleCommunicationMode.SerialPort &&
            weight.Connection.BaudRate <= 0)
        {
            errors.Add("串口波特率必须大于零。");
        }
        if (weight.StaticWeight.DataInterval <= TimeSpan.Zero)
        {
            errors.Add("称重采样间隔必须大于零。");
        }
        return errors.AsReadOnly();
    }
}
