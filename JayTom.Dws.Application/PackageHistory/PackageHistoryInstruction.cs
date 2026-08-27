using JayTom.Dws.Models.Package;

namespace JayTom.Dws.Application.PackageHistory;

/// <summary>历史指令读模型。</summary>
public sealed record PackageHistoryInstruction(
    string InstructionContent,
    DateTime InstructionGeneratedTime,
    InstructionType InstructionType);
