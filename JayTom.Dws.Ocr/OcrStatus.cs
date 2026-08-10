namespace JayTom.Dws.Ocr;

/// <summary>描述 OCR 引擎生命周期状态。</summary>
public enum OcrStatus {
    /// <summary>尚未初始化。</summary>
    Uninitialized,
    /// <summary>已经连接。</summary>
    Connected,
    /// <summary>已经初始化。</summary>
    Initialized,
    /// <summary>正在运行。</summary>
    Running,
    /// <summary>连接已断开。</summary>
    Disconnected,
    /// <summary>发生故障。</summary>
    Failure,
    /// <summary>已经暂停。</summary>
    Paused
}
