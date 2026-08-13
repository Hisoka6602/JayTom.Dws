using ThridLibray;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Irayple;

/// <summary>保存一帧已经脱离华睿智能相机 SDK 回调生命周期的扫码数据。</summary>
internal sealed class DaHuaCapturedFrame {
    /// <summary>初始化待后台处理的华睿智能相机扫码帧。</summary>
    public DaHuaCapturedFrame(IGrabbedRawData rawData, DateTime scanTime, long frameNo) {
        RawData = rawData;
        ScanTime = scanTime;
        FrameNo = frameNo;
    }

    /// <summary>获取由厂商 SDK 克隆的独立帧数据。</summary>
    public IGrabbedRawData RawData { get; }
    /// <summary>获取进入 SDK 回调时立即记录的观测时间。</summary>
    public DateTime ScanTime { get; }
    /// <summary>获取回调到达时分配的帧号。</summary>
    public long FrameNo { get; }
}
