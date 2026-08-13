using JayTom.Dws.Camera.Concurrency;
using MvCodeReaderSDKNet;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;

/// <summary>保存一帧已经脱离海康智能相机 SDK 指针生命周期的扫码数据。</summary>
internal sealed class HikvisionCapturedFrame {
    /// <summary>初始化待后台处理的海康智能相机扫码帧。</summary>
    public HikvisionCapturedFrame(
        PooledFrameBuffer buffer,
        MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 frameInfo,
        MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 barcodeResult,
        DateTime scanTime,
        long timestamp,
        long frameNo) {
        Buffer = buffer;
        FrameInfo = frameInfo;
        BarcodeResult = barcodeResult;
        ScanTime = scanTime;
        Timestamp = timestamp;
        FrameNo = frameNo;
    }

    /// <summary>获取通过共享数组池保存的完整图像帧。</summary>
    public PooledFrameBuffer Buffer { get; }
    /// <summary>获取 SDK 图像元数据快照。</summary>
    public MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 FrameInfo { get; }
    /// <summary>获取 SDK 条码结果快照。</summary>
    public MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 BarcodeResult { get; }
    /// <summary>获取 SDK 返回该帧时立即记录的观测时间。</summary>
    public DateTime ScanTime { get; }
    /// <summary>获取观测时间对应的 Unix 毫秒时间戳。</summary>
    public long Timestamp { get; }
    /// <summary>获取接收该帧时分配的帧号。</summary>
    public long FrameNo { get; }
}
