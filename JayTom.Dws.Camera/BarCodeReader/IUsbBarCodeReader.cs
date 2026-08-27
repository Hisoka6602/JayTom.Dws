namespace JayTom.Dws.Camera.BarCodeReader;

/// <summary>
/// USB 条码相机会话契约，供上层在不构造具体 SDK 适配器的情况下管理预览生命周期。
/// </summary>
public interface IUsbBarCodeReader : IDisposable
{
    /// <summary>识别到条码时触发。</summary>
    event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    /// <summary>应用条码算法参数。</summary>
    Task<KeyValuePair<bool, string>> ApplyBarcodeReaderSettingsAsync(
        BarcodeReaderSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>应用 USB 相机参数。</summary>
    Task<KeyValuePair<bool, string>> ApplyUsbCameraSettingsAsync(
        UsbCameraSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>绑定指定 USB 相机。</summary>
    Task<bool> BindCamera(UsbCameraInfo info);

    /// <summary>启动预览与识别。</summary>
    Task<KeyValuePair<bool, string>> Start();

    /// <summary>停止预览与识别。</summary>
    Task<KeyValuePair<bool, string>> Stop();
}
