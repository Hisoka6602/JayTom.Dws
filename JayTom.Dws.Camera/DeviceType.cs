using System.ComponentModel;

namespace JayTom.Dws.Camera;

/// <summary>描述安防设备类别。</summary>
public enum DeviceType {
    /// <summary>IPC 相机。</summary>
    [Description("IPC相机")]
    IPC,
    /// <summary>NVR 视频设备。</summary>
    [Description("NVR视频设备")]
    NVR
}
