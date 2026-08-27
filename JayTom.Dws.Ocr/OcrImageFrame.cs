using System;

namespace JayTom.Dws.Ocr;

/// <summary>以编码字节表达平台中立、不可变的 OCR 图像载荷。</summary>
public sealed record OcrImageFrame
{
    /// <summary>获取编码后的图像字节。</summary>
    public required ReadOnlyMemory<byte> Data { get; init; }

    /// <summary>获取图像媒体类型，例如 image/jpeg。</summary>
    public required string MediaType { get; init; }

    /// <summary>获取图像宽度像素数。</summary>
    public int Width { get; init; }

    /// <summary>获取图像高度像素数。</summary>
    public int Height { get; init; }
}
