using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>历史重量读模型。</summary>
public sealed record PackageHistoryWeight(
    decimal FormattedWeight,
    string OriginalText,
    DateTime CreateTime,
    SourceType SourceType);
