namespace JayTom.Dws.Application.PackageHistory;

/// <summary>
/// 表示一次成功云视频上传的持久化回执。
/// </summary>
/// <param name="ResponseContent">云端响应内容。</param>
/// <param name="TargetAddress">上传目标地址。</param>
/// <param name="UploadTime">云端确认的上传时间。</param>
/// <param name="UploadContent">实际上传内容。</param>
/// <param name="UploadDuration">上传耗时，单位为毫秒。</param>
/// <param name="ScanImageCount">扫码图片数量。</param>
/// <param name="PanoramaImageCount">全景图片数量。</param>
public sealed record CloudVideoUploadReceipt(
    string? ResponseContent,
    string? TargetAddress,
    DateTime? UploadTime,
    string? UploadContent,
    int? UploadDuration,
    int ScanImageCount,
    int PanoramaImageCount);
