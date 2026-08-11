namespace JayTom.Dws.Abstractions.Graphics;

/// <summary>表示平台无关的 RGBA 颜色值。</summary>
public readonly record struct RgbaColor(byte A, byte R, byte G, byte B)
{
    /// <summary>按透明度、红、绿、蓝通道创建颜色。</summary>
    public static RgbaColor FromArgb(byte alpha, byte red, byte green, byte blue) =>
        new(alpha, red, green, blue);
}
