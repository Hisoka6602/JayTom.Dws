namespace JayTom.Dws.Abstractions.Devices;

/// <summary>
/// 与具体驱动无关且可序列化的键盘类扫码设备描述。
/// </summary>
public sealed class KeyboardDevice {
    /// <summary>获取或设置厂商标识。</summary>
    public int VendorId { get; set; }
    /// <summary>获取或设置产品标识。</summary>
    public int ProductId { get; set; }
    /// <summary>获取或设置设备名称。</summary>
    public string? DeviceName { get; set; }
    /// <summary>获取或设置系统设备路径。</summary>
    public string? DevicePath { get; set; }
    /// <summary>获取或设置厂商名称。</summary>
    public string? ManufacturerName { get; set; }
    /// <summary>获取或设置设备是否在线。</summary>
    public bool IsConnected { get; set; }
}
