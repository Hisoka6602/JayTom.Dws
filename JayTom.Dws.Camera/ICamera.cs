namespace JayTom.Dws.Camera;

/// <summary>
/// 定义相机生命周期、实时预览和拍照能力。
/// </summary>
public interface ICamera : IDisposable {
    /// <summary>获取当前相机信息。</summary>
    CameraInfo? Info { get; }

    /// <summary>获取相机 SDK 类型。</summary>
    SdkType SdkType { get; }

    /// <summary>获取相机 SDK 名称。</summary>
    string SdkName { get; }

    /// <summary>获取或设置是否输出原始图像。</summary>
    bool IsOriginalImageOut { get; set; }

    /// <summary>获取相机状态。</summary>
    CameraStatus Status { get; }

    /// <summary>获取或设置相机绑定用途。</summary>
    CameraBindingType BindingType { get; set; }

    /// <summary>枚举当前 SDK 可发现的相机。</summary>
    Task<List<CameraInfo>?> EnumerateCameras();

    /// <summary>相机发生异常时触发。</summary>
    event EventHandler<CameraExceptionEventArgs> CameraExceptionOccurred;

    /// <summary>相机断开连接时触发。</summary>
    event EventHandler<CameraConnectionEventArgs> CameraDisconnected;

    /// <summary>相机初始化完成时触发。</summary>
    event EventHandler<CameraInitializedEventArgs> CameraInitialized;

    /// <summary>相机启动时触发。</summary>
    event EventHandler<CameraStartedEventArgs> CameraStarted;

    /// <summary>相机停止时触发。</summary>
    event EventHandler<CameraStoppedEventArgs> CameraStopped;

    /// <summary>相机注销时触发。</summary>
    event EventHandler<CameraUnregisteredEventArgs> CameraUnregistered;

    /// <summary>收到实时图像时触发。</summary>
    event EventHandler<RealtimeImageEventArgs> RealtimeImage;

    /// <summary>使用已枚举的中立设备信息初始化相机。</summary>
    Task<KeyValuePair<bool, string>> Initialize(
        CameraInfo camera,
        CancellationToken cancellationToken = default);

    /// <summary>启动已经初始化的相机。</summary>
    Task<KeyValuePair<bool, string>> Start(CancellationToken cancellationToken = default);

    /// <summary>停止相机。</summary>
    Task<KeyValuePair<bool, string>> Stop();

    /// <summary>异步应用强类型运行参数。</summary>
    Task ApplySettingsAsync(
        CameraRuntimeSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>获取是否已启用实时图像。</summary>
    bool IsRealtimeImageEnabled { get; }

    /// <summary>开启实时图像。</summary>
    void StartRealTimeImage();

    /// <summary>停止实时图像。</summary>
    void StopRealTimeImage();

    /// <summary>拍照完成时触发。</summary>
    event EventHandler<PhotoTakenEventArgs> PhotoTaken;

    /// <summary>按条码与时间戳拍照。</summary>
    Task TakePhotoAsync(
        string barcode,
        long packageTimestampMilliseconds,
        CancellationToken cancellation = default);

    /// <summary>延迟指定时长后按条码与时间戳拍照。</summary>
    Task TakePhotoAsync(
        string barcode,
        long packageTimestampMilliseconds,
        TimeSpan delay,
        CancellationToken cancellation = default);

    /// <summary>获取或设置拍照延迟毫秒数。</summary>
    int TakePhotoDelay { get; set; }
}
