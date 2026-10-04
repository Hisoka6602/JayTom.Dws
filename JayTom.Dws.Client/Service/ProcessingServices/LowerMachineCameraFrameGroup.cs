using System;
using System.Collections.Generic;
using JayTom.Dws.Camera;

namespace JayTom.Dws.Client.Service.ProcessingServices;

/// <summary>保存按触发次数关联的多相机最终结果，单独记录组期限。</summary>
internal sealed class LowerMachineCameraFrameGroup
{
    /// <summary>这次触发应到齐的相机数量。</summary>
    public int ExpectedCameraCount { get; init; }
    /// <summary>保存各相机在同次物理触发的最终结果。</summary>
    public Dictionary<string, (BarcodeReadEventArgs Args, bool IsNoRead)> Frames { get; } = new(StringComparer.Ordinal);
    /// <summary>表示结果到齐或等待期限结束，可以按原触发次序消费。</summary>
    public bool IsReady { get; set; }
    /// <summary>保存可取消的单调时钟融合截止任务。</summary>
    public IDisposable? Deadline { get; set; }
}
