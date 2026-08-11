namespace JayTom.Dws.Abstractions.Geometry;

/// <summary>表示平台无关的二维整数矩形。</summary>
public readonly record struct Rectangle2D(int X, int Y, int Width, int Height);
