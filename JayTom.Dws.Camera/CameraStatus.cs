using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera;

/// <summary>描述相机生命周期状态。</summary>
public enum CameraStatus {
    /// <summary>尚未初始化。</summary>
    [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
    Uninitialized,

    /// <summary>已经连接。</summary>
    [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
    Connected,

    /// <summary>已经初始化。</summary>
    [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
    Initialized,

    /// <summary>正在运行。</summary>
    [CameraBackgroundColor("#32CD32"), CameraFontIcon("\xe693")]
    Running,

    /// <summary>连接已断开。</summary>
    [CameraBackgroundColor("#A9A9A9"), CameraFontIcon("\xe612")]
    Disconnected,

    /// <summary>发生故障。</summary>
    [CameraBackgroundColor("#FF4500"), CameraFontIcon("\xe612")]
    Failure,

    /// <summary>已经暂停。</summary>
    [CameraBackgroundColor("#FF8C00"), CameraFontIcon("\xea82")]
    Paused
}
