using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.BarCodeReader;

namespace JayTom.Dws.Client.Service.Device;

/// <summary>隔离 USB 条码相机具体 SDK 构造与设备枚举。</summary>
public interface IUsbBarCodeReaderFactory
{
    /// <summary>创建独立的 USB 条码相机会话。</summary>
    IUsbBarCodeReader Create();

    /// <summary>在后台枚举可用 USB 相机。</summary>
    Task<IReadOnlyList<UsbCameraInfo>> EnumerateAsync(CancellationToken cancellationToken = default);
}
