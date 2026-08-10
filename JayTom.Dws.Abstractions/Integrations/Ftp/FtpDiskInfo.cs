namespace JayTom.Dws.Plugin.Ftp;

/// <summary>描述 FTP 远程存储空间使用情况。</summary>
public class FtpDiskInfo {
    /// <summary>获取或设置总容量字节数。</summary>
    public long TotalSize { get; set; }

    /// <summary>获取或设置已使用容量字节数。</summary>
    public long UsedSize { get; set; }

    /// <summary>获取或设置已使用容量百分比。</summary>
    public decimal UsedPercentage { get; set; }
}
