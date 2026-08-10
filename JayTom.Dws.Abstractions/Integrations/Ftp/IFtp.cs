namespace JayTom.Dws.Plugin.Ftp;

/// <summary>定义与具体 FTP 客户端实现无关的文件传输能力。</summary>
public interface IFtp {
    /// <summary>获取当前是否已经连接。</summary>
    bool IsConnected { get; }

    /// <summary>连接建立后触发。</summary>
    event EventHandler<EventArgs> Connected;

    /// <summary>连接断开后触发。</summary>
    event EventHandler<EventArgs> Disconnected;

    /// <summary>连接到指定 FTP 服务。</summary>
    Task<KeyValuePair<bool, string>> Connect(
        string server,
        int port,
        string username,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>获取当前目录的文件名列表。</summary>
    Task<List<string>?> GetFileList(CancellationToken cancellationToken = default);

    /// <summary>上传本地文件到远程路径。</summary>
    Task<KeyValuePair<bool, string>> UploadFile(
        string localFilePath,
        string remoteFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>删除指定远程文件。</summary>
    Task<KeyValuePair<bool, string>> DeleteFile(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>获取指定远程目录包含的总字节数。</summary>
    Task<long> GetDirectorySize(string directoryPath);

    /// <summary>判断指定远程目录是否存在。</summary>
    Task<bool> DirectoryExists(string directoryPath);

    /// <summary>获取指定远程目录的文件元数据。</summary>
    Task<List<FtpFileInfo>?> GetFileInfoList(
        string directoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>获取远程存储空间使用情况。</summary>
    Task<FtpDiskInfo?> GetDiskUsage();
}
