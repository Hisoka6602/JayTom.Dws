using System;

namespace JayTom.Dws.Ocr;

/// <summary>封装 OCR 运行异常事件数据。</summary>
public class OcrExceptionEventArgs : EventArgs {
    /// <summary>获取或设置异常内容。</summary>
    public Exception? Exception { get; set; }

    /// <summary>获取或设置异常发生时间。</summary>
    public DateTime? ExceptionTime { get; set; } = DateTime.Now;
}
