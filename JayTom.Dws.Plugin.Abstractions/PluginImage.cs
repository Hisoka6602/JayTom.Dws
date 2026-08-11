namespace JayTom.Dws.Plugin.Contracts;

/// <summary>
/// 表示不依赖桌面图形类型的插件图像载荷。
/// </summary>
public sealed class PluginImage {
    /// <summary>获取编码后的图像字节。</summary>
    public required ReadOnlyMemory<byte> Data { get; init; }

    /// <summary>获取图像媒体类型，例如 image/jpeg。</summary>
    public required string MediaType { get; init; }

    /// <summary>获取图像宽度像素数；未知时为零。</summary>
    public int Width { get; init; }

    /// <summary>获取图像高度像素数；未知时为零。</summary>
    public int Height { get; init; }
}
