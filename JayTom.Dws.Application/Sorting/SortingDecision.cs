namespace JayTom.Dws.Application.Sorting;

/// <summary>表示策略计算得到的目标格口和逻辑指令。</summary>
/// <param name="ExitId">目标格口标识。</param>
/// <param name="Instructions">尚未转换为厂商报文的逻辑指令。</param>
public sealed record SortingDecision(
    long ExitId,
    IReadOnlyList<string> Instructions);
