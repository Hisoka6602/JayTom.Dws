namespace JayTom.Dws.Abstractions.Devices;

/// <summary>定义平台无关的串口停止位，并保持配置数值兼容。</summary>
public enum SerialStopBits
{
    /// <summary>不使用停止位。</summary>
    None = 0,
    /// <summary>一个停止位。</summary>
    One = 1,
    /// <summary>两个停止位。</summary>
    Two = 2,
    /// <summary>一个半停止位。</summary>
    OnePointFive = 3
}
