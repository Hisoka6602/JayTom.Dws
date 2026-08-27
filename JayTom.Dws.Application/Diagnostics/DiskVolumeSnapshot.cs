namespace JayTom.Dws.Application.Diagnostics;

/// <summary>供展示层使用的磁盘容量只读快照。</summary>
public sealed record DiskVolumeSnapshot(
    string Name,
    long UsedBytes,
    decimal UsedPercentage);
