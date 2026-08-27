using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>历史分拣读模型。</summary>
public sealed record PackageHistorySorting(
    bool IsSortingUsed,
    string SortingCode,
    SortMode SortingMode,
    bool IsCreatedByLowerMachine,
    CommunicationsType CommunicationMethod,
    string ChecksumProtocolName,
    string ConnectionName,
    bool IsAbnormalSorting,
    AbnormalSortingType AbnormalSortingType,
    IReadOnlyList<PackageHistoryInstruction> InstructionInfos);
