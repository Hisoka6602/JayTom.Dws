namespace JayTom.Dws.Camera.Testing;

/// <summary>在无厂商 SDK 和真实硬件时复现相机契约与生命周期。</summary>
internal sealed class SimulatedCamera : ICamera
{
    /// <summary>记录实例是否已经释放。</summary>
    private int _disposed;

    /// <summary>获取当前模拟相机信息。</summary>
    public CameraInfo? Info { get; private set; }

    /// <summary>获取模拟器 SDK 分类。</summary>
    public SdkType SdkType => SdkType.OtherSdk;

    /// <summary>获取模拟器 SDK 名称。</summary>
    public string SdkName => "DWS Camera Simulator";

    /// <summary>获取或设置是否输出原始图像。</summary>
    public bool IsOriginalImageOut { get; set; }

    /// <summary>获取当前生命周期状态。</summary>
    public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;

    /// <summary>获取或设置相机绑定用途。</summary>
    public CameraBindingType BindingType { get; set; }

    /// <summary>获取实时图像是否已经启用。</summary>
    public bool IsRealtimeImageEnabled { get; private set; }

    /// <summary>获取或设置默认拍照延迟毫秒数。</summary>
    public int TakePhotoDelay { get; set; }

    /// <summary>模拟器发生异常时触发。</summary>
    public event EventHandler<CameraExceptionEventArgs> CameraExceptionOccurred = delegate { };

    /// <summary>模拟器断开连接时触发。</summary>
    public event EventHandler<CameraConnectionEventArgs> CameraDisconnected = delegate { };

    /// <summary>模拟器初始化完成时触发。</summary>
    public event EventHandler<CameraInitializedEventArgs> CameraInitialized = delegate { };

    /// <summary>模拟器启动时触发。</summary>
    public event EventHandler<CameraStartedEventArgs> CameraStarted = delegate { };

    /// <summary>模拟器停止时触发。</summary>
    public event EventHandler<CameraStoppedEventArgs> CameraStopped = delegate { };

    /// <summary>模拟器注销时触发。</summary>
    public event EventHandler<CameraUnregisteredEventArgs> CameraUnregistered = delegate { };

    /// <summary>模拟器输出实时帧时触发。</summary>
    public event EventHandler<RealtimeImageEventArgs> RealtimeImage = delegate { };

    /// <summary>模拟器完成拍照时触发。</summary>
    public event EventHandler<PhotoTakenEventArgs> PhotoTaken = delegate { };

    /// <summary>枚举模拟器提供的确定性设备。</summary>
    public Task<List<CameraInfo>?> EnumerateCameras()
    {
        ThrowIfDisposed();
        List<CameraInfo>? result =
        [
            new CameraInfo
            {
                Name = "Simulator",
                Brand = "DWS",
                SerialNumber = "SIM-001",
                IsAvailable = true,
                SupportedBindingType = CameraBindingType.ScannerCamera |
                                       CameraBindingType.PanoramaCamera
            }
        ];
        return Task.FromResult(result);
    }

    /// <summary>使用指定设备信息初始化模拟器。</summary>
    public Task<KeyValuePair<bool, string>> Initialize(
        CameraInfo camera,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(camera);
        cancellationToken.ThrowIfCancellationRequested();
        Info = camera;
        Status = CameraStatus.Initialized;
        CameraInitialized(this, new CameraInitializedEventArgs { CameraInfo = camera });
        return SuccessAsync();
    }

    /// <summary>启动已经初始化的模拟器。</summary>
    public Task<KeyValuePair<bool, string>> Start(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        if (Status != CameraStatus.Initialized || Info is null)
        {
            return FailureAsync("camera_not_initialized");
        }
        Status = CameraStatus.Running;
        CameraStarted(this, new CameraStartedEventArgs { Camera = this, CameraInfo = Info });
        return SuccessAsync();
    }

    /// <summary>停止正在运行的模拟器。</summary>
    public Task<KeyValuePair<bool, string>> Stop()
    {
        ThrowIfDisposed();
        if (Status != CameraStatus.Running)
        {
            return FailureAsync("camera_not_running");
        }
        IsRealtimeImageEnabled = false;
        Status = CameraStatus.Initialized;
        CameraStopped(this, new CameraStoppedEventArgs { CameraInfo = Info });
        return SuccessAsync();
    }

    /// <summary>应用通用相机运行参数。</summary>
    public Task ApplySettingsAsync(
        CameraRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();
        if (settings.TakePhotoDelayMilliseconds is int delay)
        {
            TakePhotoDelay = delay;
        }
        return Task.CompletedTask;
    }

    /// <summary>开启模拟实时图像。</summary>
    public void StartRealTimeImage()
    {
        ThrowIfDisposed();
        if (Status != CameraStatus.Running)
        {
            throw new InvalidOperationException("Camera must be running.");
        }
        IsRealtimeImageEnabled = true;
        RealtimeImage(this, new RealtimeImageEventArgs());
    }

    /// <summary>停止模拟实时图像。</summary>
    public void StopRealTimeImage()
    {
        ThrowIfDisposed();
        IsRealtimeImageEnabled = false;
    }

    /// <summary>按默认延迟模拟拍照。</summary>
    public Task TakePhotoAsync(
        string barcode,
        long packageTimestampMilliseconds,
        CancellationToken cancellation = default) =>
        TakePhotoAsync(
            barcode,
            packageTimestampMilliseconds,
            TimeSpan.FromMilliseconds(Math.Max(0, TakePhotoDelay)),
            cancellation);

    /// <summary>按指定延迟模拟拍照并发布事件。</summary>
    public async Task TakePhotoAsync(
        string barcode,
        long packageTimestampMilliseconds,
        TimeSpan delay,
        CancellationToken cancellation = default)
    {
        ThrowIfDisposed();
        if (Status != CameraStatus.Running)
        {
            throw new InvalidOperationException("Camera must be running.");
        }
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellation).ConfigureAwait(false);
        }
        cancellation.ThrowIfCancellationRequested();
        PhotoTaken(this, new PhotoTakenEventArgs
        {
            Barcode = barcode,
            PackageTimestampMilliseconds = packageTimestampMilliseconds,
            CameraSerialNumber = Info?.SerialNumber ?? string.Empty,
            PhotoTime = DateTime.Now
        });
    }

    /// <summary>模拟厂商设备断线并发布标准断线事件。</summary>
    public void Disconnect()
    {
        ThrowIfDisposed();
        Status = CameraStatus.Disconnected;
        IsRealtimeImageEnabled = false;
        CameraDisconnected(this, new CameraConnectionEventArgs { CameraInfo = Info });
    }

    /// <summary>模拟厂商异常并映射为标准相机异常事件。</summary>
    public void RaiseFailure(Exception exception)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(exception);
        Status = CameraStatus.Failure;
        CameraExceptionOccurred(this, new CameraExceptionEventArgs { Exception = exception });
    }

    /// <summary>注销模拟器并幂等释放状态。</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        IsRealtimeImageEnabled = false;
        Status = CameraStatus.Uninitialized;
        CameraUnregistered(this, new CameraUnregisteredEventArgs { CameraInfo = Info });
        Info = null;
    }

    /// <summary>在释放后拒绝继续操作。</summary>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    /// <summary>创建成功的兼容结果。</summary>
    private static Task<KeyValuePair<bool, string>> SuccessAsync() =>
        Task.FromResult(new KeyValuePair<bool, string>(true, string.Empty));

    /// <summary>创建失败的兼容结果。</summary>
    private static Task<KeyValuePair<bool, string>> FailureAsync(string error) =>
        Task.FromResult(new KeyValuePair<bool, string>(false, error));
}
