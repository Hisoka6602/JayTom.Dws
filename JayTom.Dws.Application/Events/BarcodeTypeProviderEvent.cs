namespace JayTom.Dws.Application.Events;

/// <summary>表示条码类型识别得到的尺寸与重量扣减值。</summary>
public sealed class BarcodeTypeProviderEvent
{
    /// <summary>获取条码。</summary>
    public string Barcode { get; init; } = string.Empty;

    /// <summary>获取长度扣减值。</summary>
    public decimal LengthToDeduct { get; init; }

    /// <summary>获取宽度扣减值。</summary>
    public decimal WidthToDeduct { get; init; }

    /// <summary>获取重量扣减值。</summary>
    public decimal WeightToDeduct { get; init; }

    /// <summary>获取高度扣减值。</summary>
    public decimal HeightToDeduct { get; init; }

    /// <summary>获取体积扣减值。</summary>
    public decimal VolumeToDeduct { get; init; }
}
