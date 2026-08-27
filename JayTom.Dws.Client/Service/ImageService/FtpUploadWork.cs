namespace JayTom.Dws.Client.Service.ImageService;

/// <summary>描述一项已经完成本地落盘、等待远程上传的图片工作。</summary>
internal sealed record FtpUploadWork(
    string LocalPath,
    string RemotePath,
    string Server,
    int Port,
    string Username,
    string Password);
