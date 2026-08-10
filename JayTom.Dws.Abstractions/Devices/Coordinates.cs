namespace JayTom.Dws.Abstractions.Devices;

/// <summary>
/// 表示矩形区域的左上角与右下角坐标。
/// </summary>
public readonly record struct Coordinates(int X1, int Y1, int X2, int Y2);
