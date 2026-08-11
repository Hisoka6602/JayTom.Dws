namespace JayTom.Dws.Abstractions.Integrations.Ftp;

/// <summary>描述 FTP 远程文件元数据。</summary>
public class FtpFileInfo {
    /// <summary>获取或设置文件名。</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>获取或设置文件大小字节数。</summary>
    public long FileSize { get; set; }

    /// <summary>获取或设置文件创建时间。</summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>获取或设置文件最后修改时间。</summary>
    public DateTime LastModifiedTime { get; set; }

    /// <summary>获取或设置文件完整远程路径。</summary>
    public string FullPath { get; set; } = string.Empty;
}
