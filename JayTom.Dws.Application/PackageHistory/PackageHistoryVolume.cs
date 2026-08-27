using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>历史体积读模型。</summary>
public sealed record PackageHistoryVolume(
    decimal FormattedLength,
    decimal FormattedWidth,
    decimal FormattedHeight,
    decimal FormattedVolume,
    string OriginalText,
    DateTime CreateTime,
    SourceType SourceType);
