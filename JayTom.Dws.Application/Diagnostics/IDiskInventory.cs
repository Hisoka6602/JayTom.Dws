namespace JayTom.Dws.Application.Diagnostics;

/// <summary>提供与操作系统实现解耦的磁盘清单应用端口。</summary>
public interface IDiskInventory
{
    /// <summary>异步读取固定磁盘容量快照。</summary>
    Task<IReadOnlyList<DiskVolumeSnapshot>> ListAsync(
        CancellationToken cancellationToken = default);
}
