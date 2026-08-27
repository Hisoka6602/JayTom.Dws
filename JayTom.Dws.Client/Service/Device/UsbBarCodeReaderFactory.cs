using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JayTom.Dws.Camera;
using JayTom.Dws.Camera.BarCodeReader;

namespace JayTom.Dws.Client.Service.Device;

/// <summary>USB 条码相机 SDK 适配器工厂。</summary>
internal sealed class UsbBarCodeReaderFactory : IUsbBarCodeReaderFactory
{
    /// <summary>创建独立的 USB 条码相机会话。</summary>
    public IUsbBarCodeReader Create() => new UsbBarCodeReader();

    /// <summary>在线程池中枚举 USB 相机并响应取消。</summary>
    public async Task<IReadOnlyList<UsbCameraInfo>> EnumerateAsync(
        CancellationToken cancellationToken = default)
    {
        return await Task.Run<IReadOnlyList<UsbCameraInfo>>(
            UsbBarCodeReader.EnumerateCameras,
            cancellationToken).ConfigureAwait(false);
    }
}
