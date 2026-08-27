using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>历史上传读模型。</summary>
public sealed record PackageHistoryUpload(
    UploadStatus RequestStatus,
    string RequestContent,
    string ResponseContent,
    DateTime RequestTime,
    DateTime ResponseTime,
    decimal DurationInSeconds,
    string InterfaceParameters,
    string RequestUrl,
    string ExceptionMessage);
