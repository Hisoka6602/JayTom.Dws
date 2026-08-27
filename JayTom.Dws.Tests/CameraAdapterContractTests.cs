using JayTom.Dws.Camera;
using JayTom.Dws.Camera.Testing;

namespace JayTom.Dws.Tests;

/// <summary>以无硬件模拟器验证所有相机适配器必须遵循的契约。</summary>
public sealed class CameraAdapterContractTests
{
    /// <summary>验证初始化、启动、预览、拍照、停止和释放的单向生命周期。</summary>
    [Fact]
    public async Task Simulator_obeys_camera_lifecycle_contract()
    {
        var camera = new SimulatedCamera();
        var info = new CameraInfo { SerialNumber = "SIM-CONTRACT" };
        int initialized = 0;
        int started = 0;
        int photos = 0;
        int stopped = 0;
        int unregistered = 0;
        camera.CameraInitialized += (_, _) => initialized++;
        camera.CameraStarted += (_, _) => started++;
        camera.PhotoTaken += (_, args) =>
        {
            Assert.Equal("PKG-1", args.Barcode);
            Assert.Equal(42, args.PackageTimestampMilliseconds);
            photos++;
        };
        camera.CameraStopped += (_, _) => stopped++;
        camera.CameraUnregistered += (_, _) => unregistered++;

        KeyValuePair<bool, string> initializeResult = await camera.Initialize(info);
        KeyValuePair<bool, string> startResult = await camera.Start();
        camera.StartRealTimeImage();
        await camera.TakePhotoAsync("PKG-1", 42, TimeSpan.Zero);
        KeyValuePair<bool, string> stopResult = await camera.Stop();
        camera.Dispose();
        camera.Dispose();

        Assert.True(initializeResult.Key);
        Assert.True(startResult.Key);
        Assert.True(stopResult.Key);
        Assert.Equal(1, initialized);
        Assert.Equal(1, started);
        Assert.Equal(1, photos);
        Assert.Equal(1, stopped);
        Assert.Equal(1, unregistered);
        Assert.Equal(CameraStatus.Uninitialized, camera.Status);
    }

    /// <summary>验证取消、非法状态、断线和厂商异常映射语义。</summary>
    [Fact]
    public async Task Simulator_exposes_failure_and_cancellation_contracts()
    {
        using var camera = new SimulatedCamera();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        int disconnected = 0;
        Exception? observedFailure = null;
        camera.CameraDisconnected += (_, _) => disconnected++;
        camera.CameraExceptionOccurred += (_, args) => observedFailure = args.Exception;

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            camera.Initialize(new CameraInfo(), cancellation.Token));
        KeyValuePair<bool, string> startResult = await camera.Start();
        await camera.Initialize(new CameraInfo { SerialNumber = "SIM-FAILURE" });
        camera.Disconnect();
        var failure = new InvalidOperationException("vendor failure");
        camera.RaiseFailure(failure);

        Assert.False(startResult.Key);
        Assert.Equal("camera_not_initialized", startResult.Value);
        Assert.Equal(1, disconnected);
        Assert.Same(failure, observedFailure);
        Assert.Equal(CameraStatus.Failure, camera.Status);
    }
}
