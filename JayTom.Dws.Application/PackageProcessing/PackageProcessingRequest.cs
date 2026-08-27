namespace JayTom.Dws.Application.PackageProcessing;

/// <summary>与设备和持久化模型无关的包裹处理输入快照。</summary>
public sealed record PackageProcessingRequest(
    long PackageKey,
    DateTime CreatedAt,
    string? Barcode,
    DateTime ScanTime,
    string SourceIdentifier,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    decimal? Volume);
