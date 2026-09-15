using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.CompilerServices;
using CamSDK;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Concurrency;
using JayTom.Dws.Camera.FilterContainer;
using JayTom.Dws.Camera.Cameras.SmartCamera.Irayple;
using JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Wayzim;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Camera.Cameras.VolumeCamera.Dimension;
using JayTom.Dws.Camera.BarCodeReader;
using ThridLibray;

namespace JayTom.Dws.Tests;

/// <summary>复现不同厂商适配器的误报、重复订阅和暂停恢复问题。</summary>
public sealed class VendorCameraRegressionTests {
    /// <summary>被规则过滤掉的有效条码不应变成未读码事件。</summary>
    [Fact]
    public void Irayple_filtered_barcode_is_not_noread() {
        var camera = new DaHuaSmartCamera();
        SetField(camera, "_barCodeFilterContainer", new BarCodeFilterContainer {
            BarCodeFilterMode = BarCodeFilterMode.BasicFilter,
            Pattern = "^JT[0-9]+$",
            FilterOutContent = string.Empty
        });
        var noReads = 0;
        camera.NotBarcodeHitEvent += (_, args) => { noReads++; Release(args); };
        Invoke(camera, "GrabResultDecode", RawFrame("BarCodeNum Value:1", "Code0_CodeData Value:SF123"), DateTime.Now, 1L);
        Assert.Equal(0, noReads);
    }

    /// <summary>只有图片而没有解码结果时，不能据此断言硬件触发读码失败。</summary>
    [Fact]
    public void Irayple_image_without_decode_metadata_is_not_noread() {
        var camera = new DaHuaSmartCamera();
        var noReads = 0;
        camera.NotBarcodeHitEvent += (_, args) => { noReads++; Release(args); };
        Invoke(camera, "GrabResultDecode", RawFrame("Exposure Value:1000"), DateTime.Now, 1L);
        Assert.Equal(0, noReads);
    }

    /// <summary>不同量方相机的单次和连续测量设置互不覆盖。</summary>
    [Fact]
    public async Task Dimension_trigger_settings_are_per_camera_and_stop_before_init_is_safe() {
        var modes = Enum.GetValues<MeasurementTriggerMode>();
        var first = new DimensionVolumeCamera { MeasurementTriggerMode = modes[0] };
        var second = new DimensionVolumeCamera { MeasurementTriggerMode = modes[1] };
        Assert.Equal(modes[0], first.MeasurementTriggerMode);
        Assert.Equal(modes[1], second.MeasurementTriggerMode);
        Assert.True((await first.Stop()).Key);
    }

    /// <summary>安防相机重复初始化不会改写绑定类型或再订阅读码器。</summary>
    [Fact]
    public async Task Security_reinitialization_preserves_binding_and_stopped_results_are_disposed() {
        // 跳过会加载原生安防库的构造函数，只验证生命周期边界与托管结果处理。
        var camera = (DaHuatechSecurityCamera)RuntimeHelpers.GetUninitializedObject(typeof(DaHuatechSecurityCamera));
        SetProperty(camera, "Status", CameraStatus.Initialized);
        camera.BindingType = CameraBindingType.PanoramaCamera;
        Assert.False((await camera.Initialize(new CameraInfo())).Key);
        Assert.Equal(CameraBindingType.PanoramaCamera, camera.BindingType);
        SetProperty(camera, "Status", CameraStatus.Paused);
        var image = new Bitmap(2, 2);
        Invoke(camera, "HandleBarcodeResult", new BarcodeResult { Image = image });
        Assert.Throws<ArgumentException>(() => image.GetPixel(0, 0));
    }

    /// <summary>重复条码被去重后不产生额外未读码事件。</summary>
    [Fact]
    public void Irayple_duplicate_barcode_is_not_noread() {
        var camera = new DaHuaSmartCamera { IsShowBarcodeBorder = false };
        SetField(camera, "_barCodeFilterContainer", new BarCodeFilterContainer {
            BarCodeFilterMode = BarCodeFilterMode.None,
            MaxSize = 0,
            ExpirationTime = TimeSpan.FromSeconds(2)
        });
        var noReads = 0;
        var barcodes = 0;
        camera.NotBarcodeHitEvent += (_, args) => { noReads++; Release(args); };
        camera.BarcodeReadTriggered += (_, args) => { barcodes++; Release(args); };
        var frame = RawFrame("BarCodeNum Value:1", $"Code0_CodeData Value:REPLAY-{Guid.NewGuid():N}");
        var time = DateTime.Now;
        Invoke(camera, "GrabResultDecode", frame, time, 1L);
        Invoke(camera, "GrabResultDecode", frame, time.AddMilliseconds(55), 2L);
        Assert.Equal(1, barcodes);
        Assert.Equal(0, noReads);
    }

    /// <summary>所有空帧输出开关在两种智能相机中具有相同的行为。</summary>
    [Theory]
    [InlineData(true, false, TriggerMode.Hardware, 1)]
    [InlineData(true, true, TriggerMode.Hardware, 0)]
    [InlineData(false, false, TriggerMode.Hardware, 0)]
    [InlineData(true, false, TriggerMode.Software, 0)]
    public void Smart_camera_empty_frames_respect_trigger_and_hide_settings(bool enabled, bool hidden, TriggerMode mode, int expected) {
        var irayple = new DaHuaSmartCamera { IsUseTriggerMode = enabled, IsHideNoRead = hidden, TriggerMode = mode };
        var wayzim = new WayzimSmartCamera { IsUseTriggerMode = enabled, IsHideNoRead = hidden, TriggerMode = mode };
        var iraypleCount = 0;
        var wayzimCount = 0;
        irayple.NotBarcodeHitEvent += (_, args) => { iraypleCount++; Release(args); };
        wayzim.NotBarcodeHitEvent += (_, args) => { wayzimCount++; Release(args); };
        Invoke(irayple, "GrabResultDecode", RawFrame("BarCodeNum Value:0"), DateTime.Now, 1L);
        using var image = new Bitmap(2, 2);
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Jpeg);
        var data = stream.ToArray();
        using var buffer = PooledFrameBuffer.CopyFrom(data, data.Length);
        var wayzimSession = GetField<CameraCaptureSession>(wayzim, "_captureSession");
        var generation = wayzimSession.Start();
        Invoke(wayzim, "EnsureNoReadGate");
        var frame = new WayzimCapturedFrame(buffer, new CodeInfoStruct(), DateTime.Now, 0, 1, generation);
        Invoke(wayzim, "ProcessCapturedFrame", frame);
        GetField<TriggerBurstNoReadGate?>(wayzim, "_noReadGate")?.Complete();
        GetField<TriggerBurstNoReadGate?>(wayzim, "_noReadGate")?.Dispose();
        wayzimSession.Stop();
        Assert.Equal(expected, iraypleCount);
        Assert.Equal(expected, wayzimCount);
    }

    /// <summary>快仓同一连续触发帧组先无码后有码时只发布条码，不发布先到的 NoRead。</summary>
    [Fact]
    public void Wayzim_barcode_in_same_trigger_burst_cancels_noread() {
        var camera = new WayzimSmartCamera {
            IsUseTriggerMode = true,
            IsHideNoRead = false,
            TriggerMode = TriggerMode.Hardware,
            IsShowBarcodeBorder = false
        };
        SetField(camera, "_barCodeFilterContainer", new BarCodeFilterContainer {
            BarCodeFilterMode = BarCodeFilterMode.None,
            MaxSize = 0
        });
        var noReads = 0;
        var barcodes = 0;
        camera.NotBarcodeHitEvent += (_, args) => { noReads++; Release(args); };
        camera.BarcodeReadTriggered += (_, args) => { barcodes++; Release(args); };

        var session = GetField<CameraCaptureSession>(camera, "_captureSession");
        var generation = session.Start();
        Invoke(camera, "EnsureNoReadGate");
        Invoke(camera, "ProcessCapturedFrame", WayzimFrame(new CodeInfoStruct(), generation, 10));
        var code = new CodeInfo(
            "JT-BURST",
            0,
            0,
            0,
            [0, 0, 1, 0, 1, 1, 0, 1],
            CodeTypes.Barcode,
            "JT-BURST");
        Invoke(camera, "ProcessCapturedFrame", WayzimFrame(new CodeInfoStruct {
            CodeInfos = [code],
            ResolutionX = "2",
            ResolutionY = "2"
        }, generation, 11));

        var gate = GetField<TriggerBurstNoReadGate>(camera, "_noReadGate");
        gate.Complete();
        gate.Dispose();
        session.Stop();
        Assert.Equal(1, barcodes);
        Assert.Equal(0, noReads);
    }

    /// <summary>华睿电平触发连续帧先无码后有码时只发布条码，不发布先到的 NoRead。</summary>
    [Fact]
    public void Irayple_barcode_in_same_level_trigger_burst_cancels_noread() {
        var camera = new DaHuaSmartCamera {
            IsUseTriggerMode = true,
            IsHideNoRead = false,
            TriggerMode = TriggerMode.Hardware,
            IsShowBarcodeBorder = false
        };
        SetField(camera, "_isLevelTrigger", true);
        var noReads = 0;
        var barcodes = 0;
        camera.NotBarcodeHitEvent += (_, args) => { noReads++; Release(args); };
        camera.BarcodeReadTriggered += (_, args) => { barcodes++; Release(args); };

        var session = GetField<CameraCaptureSession>(camera, "_captureSession");
        var generation = session.Start();
        Invoke(camera, "EnsureNoReadGate");
        Invoke(camera, "GrabResultDecodeCore", RawFrame("BarCodeNum Value:0"), DateTime.Now, 1L, generation);
        Invoke(
            camera,
            "GrabResultDecodeCore",
            RawFrame("BarCodeNum Value:1", "Code0_CodeData Value:JT-LEVEL"),
            DateTime.Now.AddMilliseconds(55),
            2L,
            generation);

        var gate = GetField<TriggerBurstNoReadGate>(camera, "_noReadGate");
        gate.Complete();
        gate.Dispose();
        session.Stop();
        Assert.Equal(1, barcodes);
        Assert.Equal(0, noReads);
    }

    /// <summary>相机打开时应按软件配置关闭触发或选择对应触发源。</summary>
    [Theory]
    [InlineData(false, TriggerMode.Hardware, null)]
    [InlineData(true, TriggerMode.Software, "Software")]
    [InlineData(true, TriggerMode.Hardware, "Line1")]
    public void Irayple_configures_the_requested_trigger_mode(bool enabled, TriggerMode mode, string? expectedSource) {
        string? source = null;
        var closed = false;
        var trigger = CameraSdkTestProxy.Create<ITrigger>((method, args) => {
            if (method.Name == "Open") { source = (string)args[0]!; return true; }
            if (method.Name == "Close") { closed = true; return true; }
            throw new InvalidOperationException(method.Name);
        });
        var camera = new DaHuaSmartCamera { IsUseTriggerMode = enabled, TriggerMode = mode };
        Invoke(camera, "ConfigureTrigger", trigger);
        Assert.Equal(expectedSource, source);
        Assert.Equal(!enabled, closed);
    }

    /// <summary>停止后重启保留设备，每次只有一个码流订阅，失败启动可重试。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Irayple_restart_keeps_one_callback_and_rejects_old_callbacks(bool failFirstStart) {
        var subscribers = new List<EventHandler<GrabbedEventArgs>>();
        var stream = CameraSdkTestProxy.Create<IStream>((method, args) => {
            if (method.Name == "add_ImageGrabbed") { subscribers.Add((EventHandler<GrabbedEventArgs>)args[0]!); return null; }
            if (method.Name == "remove_ImageGrabbed") { subscribers.Remove((EventHandler<GrabbedEventArgs>)args[0]!); return null; }
            throw new InvalidOperationException(method.Name);
        });
        var opened = false;
        var grabbing = false;
        var attempts = 0;
        var device = CameraSdkTestProxy.Create<IDevice>((method, _) => {
            switch (method.Name) {
                case "get_IsOpen": return opened;
                case "get_IsGrabbing": return grabbing;
                case "get_StreamGrabber": return stream;
                case "Open": opened = true; return true;
                case "Close": opened = false; return true;
                case "ShutdownGrab": grabbing = false; return true;
                case "GrabUsingGrabLoopThread":
                    Assert.Collection(subscribers, callback => Assert.NotNull(callback));
                    attempts++;
                    grabbing = !failFirstStart || attempts > 1;
                    return grabbing;
                default: throw new InvalidOperationException(method.Name);
            }
        });
        var camera = new DaHuaSmartCamera();
        SetField(camera, "_device", device);
        if (failFirstStart) {
            Assert.False((await camera.Start()).Key);
            Assert.Empty(subscribers);
        }
        Assert.True((await camera.Start()).Key);
        var previous = subscribers[0];
        Assert.True((await camera.Start()).Key);
        Assert.Collection(subscribers, callback => Assert.Same(previous, callback));
        Assert.True((await camera.Stop()).Key);
        Assert.Equal(CameraStatus.Paused, camera.Status);
        Assert.Empty(subscribers);
        Assert.True((await camera.Start()).Key);
        Assert.Collection(subscribers, callback => Assert.NotSame(previous, callback));
        // 使用空参数证明旧回调被会话检查拦截，不再访问 SDK 结果。
        previous(null, null!);
        Assert.True((await camera.Stop()).Key);
    }

    /// <summary>同一会话的旧帧在停止以及再次启动后始终无效。</summary>
    [Fact]
    public void Capture_session_rejects_previous_generation() {
        var session = new CameraCaptureSession();
        var first = session.Start();
        Assert.True(session.IsCurrent(first));
        session.Stop();
        Assert.False(session.IsCurrent(first));
        var second = session.Start();
        Assert.False(session.IsCurrent(first));
        Assert.True(session.IsCurrent(second));
    }

    /// <summary>快仓暂停恢复调用原生启动一次，重复启动不再重复调用。</summary>
    [Fact]
    public async Task Wayzim_industrial_camera_can_restart_from_paused() {
        var original = ICAMAPI.ICAM_StartCamera;
        var calls = 0;
        try {
            ICAMAPI.ICAM_StartCamera = _ => { calls++; return 0; };
            var camera = new WayzimIndustrialCamera { BindingType = CameraBindingType.PanoramaCamera };
            SetProperty(camera, "Info", new CameraInfo { Id = 3 });
            SetProperty(camera, "Status", CameraStatus.Paused);
            Assert.True((await camera.Start()).Key);
            Assert.True((await camera.Start()).Key);
            Assert.Equal(1, calls);
            Assert.Equal(CameraStatus.Running, camera.Status);
        }
        finally { ICAMAPI.ICAM_StartCamera = original; }
    }

    /// <summary>未初始化的海康工业相机不能通过空句柄伪装启动成功。</summary>
    [Fact]
    public async Task Hikvision_industrial_camera_rejects_start_without_initialization() {
        var camera = new HikvisionIndustrialCamera();
        Assert.False((await camera.Start()).Key);
        Assert.Equal(CameraStatus.Uninitialized, camera.Status);
    }

    /// <summary>快仓运行状态下再次启动不接触 SDK 或重新绑定回调。</summary>
    [Fact]
    public async Task Wayzim_smart_camera_repeated_start_is_idempotent() {
        var camera = new WayzimSmartCamera();
        SetProperty(camera, "Status", CameraStatus.Running);
        Assert.True((await camera.Start()).Key);
        Assert.Equal(CameraStatus.Running, camera.Status);
    }

    /// <summary>构造带有指定条码元数据的厂商帧接口。</summary>
    private static IGrabbedRawData RawFrame(params string[] metadata) {
        var chunks = CameraSdkTestProxy.Create<IChunkData>((method, args) => {
            if (method.Name == "get_ChunkCount") { return 1u; }
            if (method.Name == "GetChunkDataByIndex") { args[1] = 0u; args[2] = new List<string>(metadata); return true; }
            throw new InvalidOperationException(method.Name);
        });
        return CameraSdkTestProxy.Create<IGrabbedRawData>((method, _) => method.Name switch {
            "get_ChunkData" => chunks,
            "ToBitmap" => new Bitmap(2, 2),
            _ => throw new InvalidOperationException(method.Name)
        });
    }

    /// <summary>创建供快仓适配器真实处理入口使用的独立 JPEG 帧。</summary>
    private static WayzimCapturedFrame WayzimFrame(CodeInfoStruct codeInfo, int generation, uint sdkFrameNo) {
        using var image = new Bitmap(2, 2);
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Jpeg);
        var data = stream.GetBuffer();
        var length = checked((int)stream.Length);
        return new WayzimCapturedFrame(
            PooledFrameBuffer.CopyFrom(data, length),
            codeInfo,
            DateTime.Now,
            DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            sdkFrameNo,
            generation,
            sdkFrameNo);
    }

    /// <summary>调用适配器的真实处理入口，避免连接物理设备。</summary>
    private static object? Invoke(object camera, string name, params object?[] args) =>
        camera.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(camera, args);

    /// <summary>设置无需原生句柄的测试状态。</summary>
    private static void SetField(object camera, string name, object value) =>
        camera.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(camera, value);

    /// <summary>读取测试对象的私有字段。</summary>
    private static T GetField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    /// <summary>通过私有设置器准备相机生命周期状态。</summary>
    private static void SetProperty(object camera, string name, object value) =>
        camera.GetType().GetProperty(name)!.SetValue(camera, value);

    /// <summary>模拟业务订阅者释放事件携带的图像。</summary>
    private static void Release(BarcodeReadEventArgs args) {
        args.Image?.Dispose();
        args.ThumbImage?.Dispose();
    }
}
