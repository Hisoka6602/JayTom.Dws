using JayTom.Dws.Camera.Concurrency;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Wayzim;

/// <summary>保存一帧已经脱离快仓工业相机 SDK 帧句柄生命周期的扫码数据。</summary>
internal sealed class WayzimIndustrialCapturedFrame {
    /// <summary>初始化待后台处理的快仓工业相机扫码帧。</summary>
    public WayzimIndustrialCapturedFrame(
        PooledFrameBuffer buffer,
        ImageModelCpp image,
        string serialNumber,
        DateTime scanTime,
        long frameNo) {
        Buffer = buffer;
        Image = image;
        SerialNumber = serialNumber;
        ScanTime = scanTime;
        FrameNo = frameNo;
    }

    /// <summary>获取通过共享数组池保存的原始像素帧。</summary>
    public PooledFrameBuffer Buffer { get; }
    /// <summary>获取图像尺寸和条码结果快照。</summary>
    public ImageModelCpp Image { get; }
    /// <summary>获取相机序列号快照。</summary>
    public string SerialNumber { get; }
    /// <summary>获取 SDK 返回该帧时立即记录的观测时间。</summary>
    public DateTime ScanTime { get; }
    /// <summary>获取拉帧成功时分配的帧号。</summary>
    public long FrameNo { get; }
}
