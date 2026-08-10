using System.ComponentModel;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera;

/// <summary>描述相机硬件类别。</summary>
public enum CameraType {
    /// <summary>工业相机。</summary>
    [Description("工业相机"), CameraFontIcon("\xe9f5")]
    IndustrialCamera = 0,

    /// <summary>智能相机。</summary>
    [Description("智能相机"), CameraFontIcon("\xe6ef")]
    SmartCamera = 1,

    /// <summary>三维或体积相机。</summary>
    [Description("3D相机/体积相机"), CameraFontIcon("\xea1a")]
    ThreeDCamera = 2,

    /// <summary>网络视频相机。</summary>
    [Description("IPC相机"), CameraFontIcon("\xea0b")]
    VideoCamera = 3,

    /// <summary>USB 相机。</summary>
    [Description("Usb相机"), CameraFontIcon("\xe9f5")]
    UsbCamera = 4,

    /// <summary>NVR 设备。</summary>
    [Description("NVR设备"), CameraFontIcon("\xe9ef")]
    NvrDevice = 5
}
