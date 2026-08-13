using CamSDK;
using JayTom.Dws.Camera.Concurrency;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim;

/// <summary>保存一帧已经脱离快仓智能相机 SDK 回调生命周期的扫码数据。</summary>
internal sealed class WayzimCapturedFrame {
    /// <summary>初始化待后台处理的快仓智能相机扫码帧。</summary>
    public WayzimCapturedFrame(
        PooledFrameBuffer buffer,
        CodeInfoStruct codeInfo,
        DateTime scanTime,
        long timestamp,
        long frameNo) {
        Buffer = buffer;
        CodeInfo = codeInfo;
        ScanTime = scanTime;
        Timestamp = timestamp;
        FrameNo = frameNo;
    }

    /// <summary>获取通过共享数组池保存的 JPEG 帧。</summary>
    public PooledFrameBuffer Buffer { get; }
    /// <summary>获取厂商条码元数据快照。</summary>
    public CodeInfoStruct CodeInfo { get; }
    /// <summary>获取进入 SDK 回调时立即记录的观测时间。</summary>
    public DateTime ScanTime { get; }
    /// <summary>获取观测时间对应的 Unix 毫秒时间戳。</summary>
    public long Timestamp { get; }
    /// <summary>获取回调到达时分配的帧号。</summary>
    public long FrameNo { get; }
}
