using System.ComponentModel;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera;

/// <summary>描述相机连接方式。</summary>
public enum CameraConnectionType {
    /// <summary>USB 连接。</summary>
    [CameraFontIcon("\xe7c5"), Description("USB连接")]
    Usb = 0,

    /// <summary>以太网连接。</summary>
    [CameraFontIcon("\xe631"), Description("网口连接")]
    Ethernet = 1,

    /// <summary>串口连接。</summary>
    [CameraFontIcon("\xe62c"), Description("串口连接")]
    SerialPort = 2,

    /// <summary>蓝牙连接。</summary>
    [CameraFontIcon("\xec4a"), Description("蓝牙连接")]
    Bluetooth = 3,

    /// <summary>TCP 连接。</summary>
    [CameraFontIcon("\xe62f"), Description("Tcp连接")]
    Tcp = 4,

    /// <summary>未知连接方式。</summary>
    [CameraFontIcon("\xe71f"), Description("未知连接")]
    Unknown = 5
}
