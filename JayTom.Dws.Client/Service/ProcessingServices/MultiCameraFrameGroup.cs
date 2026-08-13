using System;
using System.Collections.Generic;
using JayTom.Dws.Domain.Model;

namespace JayTom.Dws.Client.Service.ProcessingServices;

/// <summary>保存同一机械包裹对应的多相机帧及其精确融合期限。</summary>
internal sealed class MultiCameraFrameGroup
{
    /// <summary>保存组内首帧的设备观察时间。</summary>
    public DateTime AnchorScanTime { get; init; }

    /// <summary>保存组内首帧序号，用于优先关联同步触发帧。</summary>
    public long FrameNumber { get; init; }

    /// <summary>记录创建本组时必须到齐的扫描相机数量。</summary>
    public int ExpectedCameraCount { get; init; }

    /// <summary>按相机序列号保存当前包裹的唯一帧。</summary>
    public Dictionary<string, BarCodeFrameInfo> Frames { get; } =
        new(StringComparer.Ordinal);

    /// <summary>保存组内首个有效条码，用于无分配检测冲突。</summary>
    public string? ValidBarcode { get; set; }

    /// <summary>表示组内至少出现两个不同的有效条码。</summary>
    public bool HasBarcodeConflict { get; set; }

    /// <summary>保存可取消的融合截止任务。</summary>
    public IDisposable? Deadline { get; set; }
}
