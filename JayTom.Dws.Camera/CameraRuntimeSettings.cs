namespace JayTom.Dws.Camera;

/// <summary>
/// 汇总相机运行期可选配置，替代以字符串键和值对象传递厂商参数。
/// </summary>
public sealed class CameraRuntimeSettings
{
    /// <summary>获取或初始化读码器参数。</summary>
    public BarCodeReader.BarcodeReaderSettings? BarcodeReader { get; init; }

    /// <summary>获取或初始化 USB 相机画面参数。</summary>
    public UsbCameraSettings? UsbCamera { get; init; }

    /// <summary>获取或初始化拍照延迟毫秒数。</summary>
    public int? TakePhotoDelayMilliseconds { get; init; }

    /// <summary>获取或初始化体积测量触发模式。</summary>
    public MeasurementTriggerMode? MeasurementTriggerMode { get; init; }
}
