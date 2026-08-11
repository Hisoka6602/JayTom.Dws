namespace JayTom.Dws.Abstractions.Devices;

/// <summary>定义平台无关的串口校验位，并保持配置数值兼容。</summary>
public enum SerialParity
{
    /// <summary>无校验。</summary>
    None = 0,
    /// <summary>奇校验。</summary>
    Odd = 1,
    /// <summary>偶校验。</summary>
    Even = 2,
    /// <summary>标记校验。</summary>
    Mark = 3,
    /// <summary>空格校验。</summary>
    Space = 4
}
