namespace JayTom.Dws.Application.Sorting;

/// <summary>表示与客户端模型、数据库实体和厂商协议无关的分拣输入。</summary>
/// <param name="PackageId">包裹的稳定标识。</param>
/// <param name="Barcode">包裹条码。</param>
/// <param name="Strategy">本次决策采用的策略。</param>
/// <param name="CreatedAt">包裹进入分拣流程的时间。</param>
/// <param name="Attributes">策略可读取的扩展属性只读快照。</param>
public sealed record SortingRequest(
    long PackageId,
    string Barcode,
    SortingStrategyKind Strategy,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<string, string> Attributes);
