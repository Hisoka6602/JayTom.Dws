// DWS-COHESIVE-CONTRACTS: 相机生命周期、能力和工厂是一套版本化 ABI。
using JayTom.Dws.Abstractions.Results;
using JayTom.Dws.Domain.Devices;

namespace JayTom.Dws.Camera.Contracts;

/// <summary>定义相机设备可声明的能力。</summary>
[Flags]
public enum CameraCapabilities {
    /// <summary>无能力。</summary>
    None = 0,
    /// <summary>抓拍单帧。</summary>
    Snapshot = 1,
    /// <summary>连续帧。</summary>
    Streaming = 2,
    /// <summary>设备端条码识别。</summary>
    BarcodeRecognition = 4,
    /// <summary>体积测量。</summary>
    VolumeMeasurement = 8,
    /// <summary>录像回放。</summary>
    Playback = 16
}

/// <summary>表示不含厂商序列化文本的相机连接请求。</summary>
public sealed record CameraConnectionRequest(
    DeviceId DeviceId,
    string AdapterKey,
    string Host,
    int Port,
    IReadOnlyDictionary<string, string> Options);

/// <summary>表示相机的不可变描述。</summary>
public sealed record CameraDescriptor(
    DeviceId DeviceId,
    string DisplayName,
    string AdapterKey,
    CameraCapabilities Capabilities);

/// <summary>定义统一的异步相机生命周期。</summary>
public interface ICameraAdapter : IAsyncDisposable {
    /// <summary>获取相机描述。</summary>
    CameraDescriptor Descriptor { get; }

    /// <summary>异步连接相机。</summary>
    Task<Result> ConnectAsync(
        CameraConnectionRequest request,
        CancellationToken cancellationToken);

    /// <summary>异步开始采集。</summary>
    Task<Result> StartAsync(CancellationToken cancellationToken);

    /// <summary>异步停止采集。</summary>
    Task<Result> StopAsync(CancellationToken cancellationToken);

    /// <summary>异步断开连接。</summary>
    Task<Result> DisconnectAsync(CancellationToken cancellationToken);
}

/// <summary>按稳定键按需创建厂商相机适配器。</summary>
public interface ICameraAdapterFactory {
    /// <summary>获取可用适配器键。</summary>
    IReadOnlySet<string> AvailableAdapterKeys { get; }

    /// <summary>尝试创建适配器；缺少 SDK 时返回结构化失败而不终止宿主。</summary>
    OperationResult<ICameraAdapter> Create(string adapterKey);
}
