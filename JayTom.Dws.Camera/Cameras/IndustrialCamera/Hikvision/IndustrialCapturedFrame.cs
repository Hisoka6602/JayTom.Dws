using JayTom.Dws.Camera.Concurrency;
using MVIDCodeReaderNet;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

/// <summary>保存一帧已经脱离海康工业相机 SDK 原生回调生命周期的扫码数据。</summary>
internal sealed class IndustrialCapturedFrame {
    /// <summary>初始化待后台处理的海康工业相机扫码帧。</summary>
    public IndustrialCapturedFrame(
        PooledFrameBuffer buffer,
        MVIDCodeReader.MVID_CAM_OUTPUT_INFO output,
        DateTime scanTime,
        long timestamp,
        long frameNo) {
        Buffer = buffer;
        Output = output;
        ScanTime = scanTime;
        Timestamp = timestamp;
        FrameNo = frameNo;
    }

    /// <summary>获取通过共享数组池保存的原始像素帧。</summary>
    public PooledFrameBuffer Buffer { get; }
    /// <summary>获取 SDK 输出元数据和条码结果快照。</summary>
    public MVIDCodeReader.MVID_CAM_OUTPUT_INFO Output { get; }
    /// <summary>获取进入原生回调时立即记录的观测时间。</summary>
    public DateTime ScanTime { get; }
    /// <summary>获取观测时间对应的 Unix 毫秒时间戳。</summary>
    public long Timestamp { get; }
    /// <summary>获取回调到达时分配的帧号。</summary>
    public long FrameNo { get; }
}
