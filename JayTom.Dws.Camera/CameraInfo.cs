namespace JayTom.Dws.Camera;

/// <summary>
/// 描述可枚举和连接的相机硬件。
/// </summary>
public class CameraInfo {
    /// <summary>获取或设置相机标识。</summary>
    public long Id { get; set; }

    /// <summary>获取或设置相机名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>获取或设置相机品牌。</summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>获取或设置相机序列号。</summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>获取或设置相机 IP 地址。</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>获取或设置相机版本号。</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>获取或设置相机型号。</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>获取或设置服务端口。</summary>
    public int Port { get; set; }

    /// <summary>获取或设置相机是否已激活并可用。</summary>
    public bool IsAvailable { get; set; }

    /// <summary>获取或设置相机是否支持 OCR。</summary>
    public bool IsOcrSupported { get; set; }

    /// <summary>获取或设置相机类型。</summary>
    public CameraType Type { get; set; }

    /// <summary>获取或设置连接类型。</summary>
    public CameraConnectionType ConnectionType { get; set; }

    /// <summary>获取或设置用户定义名称。</summary>
    public string CustomName { get; set; } = string.Empty;

    /// <summary>获取或设置支持的绑定用途。</summary>
    public CameraBindingType SupportedBindingType { get; set; }

    /// <summary>获取或设置关联的 NVR 信息。</summary>
    public CameraNvrInfo? CameraNvrInfo { get; set; }

    /// <summary>按非空序列号判断两个相机是否相同。</summary>
}
