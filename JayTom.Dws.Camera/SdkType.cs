namespace JayTom.Dws.Camera;

/// <summary>描述相机厂商 SDK 的能力类别。</summary>
public enum SdkType {
    /// <summary>智能相机 SDK。</summary>
    SmartCameraSdk,
    /// <summary>工业相机 SDK。</summary>
    IndustrialCameraSdk,
    /// <summary>体积相机 SDK。</summary>
    VolumeCameraSdk,
    /// <summary>视频相机 SDK。</summary>
    VideoCameraSdk,
    /// <summary>安防相机 SDK。</summary>
    SecurityCamera,
    /// <summary>其他 SDK。</summary>
    OtherSdk
}
