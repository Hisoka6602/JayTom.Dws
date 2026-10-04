using System;

namespace JayTom.Dws.Client.Service.ProcessingServices;

/// <summary>保留一次物理触发的原车身份快照，不持有业务包裹、图片或计时器。</summary>
internal readonly record struct LowerMachineCameraSlot(
    DateTime? CreatedAt, long PackageTimestamp, long CartId, long TriggerSequence);
