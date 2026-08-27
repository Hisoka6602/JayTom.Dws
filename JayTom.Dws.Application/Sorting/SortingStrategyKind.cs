namespace JayTom.Dws.Application.Sorting;

/// <summary>定义分拣决策可选择的业务策略类型。</summary>
public enum SortingStrategyKind
{
    /// <summary>按条码规则分拣。</summary>
    Barcode,

    /// <summary>按重量规则分拣。</summary>
    Weight,

    /// <summary>按体积规则分拣。</summary>
    Volume,

    /// <summary>按 OCR 结果分拣。</summary>
    Ocr,

    /// <summary>按物流规则分拣。</summary>
    Logistics,

    /// <summary>按外部 API 响应分拣。</summary>
    Api
}
