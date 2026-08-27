namespace JayTom.Dws.Application.Sorting;

/// <summary>定义从应用层发送给协议适配器的稳定命令。</summary>
/// <param name="PackageId">包裹的稳定标识。</param>
/// <param name="Barcode">用于协议附加信息的包裹条码。</param>
/// <param name="ExitId">目标格口标识。</param>
/// <param name="Instructions">待适配的逻辑指令。</param>
/// <param name="Interval">多条指令之间的发送间隔。</param>
public sealed record SortingProtocolCommand(
    long PackageId,
    string Barcode,
    long ExitId,
    IReadOnlyList<string> Instructions,
    TimeSpan Interval);
