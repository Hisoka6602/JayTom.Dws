using JayTom.Dws.Application.Diagnostics;

namespace JayTom.Dws.Infrastructure.Diagnostics;

/// <summary>将操作系统计算机信息适配为应用层磁盘清单。</summary>
internal sealed class ComputerDiskInventory : IDiskInventory
{
    /// <summary>底层计算机信息提供器。</summary>
    private readonly global::JayTom.Dws.Infrastructure.IComputer.IComputer _computer;

    /// <summary>创建磁盘清单适配器。</summary>
    public ComputerDiskInventory(
        global::JayTom.Dws.Infrastructure.IComputer.IComputer computer)
    {
        _computer = computer;
    }

    /// <summary>读取并映射固定磁盘信息。</summary>
    public async Task<IReadOnlyList<DiskVolumeSnapshot>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var disks = await _computer.GetDiskInfoAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return disks.Select(disk => new DiskVolumeSnapshot(
                disk.Name,
                disk.UsedDiskSpace,
                disk.UsedDiskSpacePercentage))
            .ToArray();
    }
}
