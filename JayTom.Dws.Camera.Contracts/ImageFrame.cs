// DWS-COHESIVE-CONTRACTS: 帧租约、像素格式和解码结果共享同一内存所有权协议。
using System.Buffers;

namespace JayTom.Dws.Camera.Contracts;

/// <summary>定义与图形 UI 无关的像素格式。</summary>
public enum FramePixelFormat {
    /// <summary>灰度 8 位。</summary>
    Gray8,
    /// <summary>24 位 RGB。</summary>
    Rgb24,
    /// <summary>24 位 BGR。</summary>
    Bgr24,
    /// <summary>JPEG 编码。</summary>
    Jpeg
}

/// <summary>
/// 显式拥有池化帧内存，并在释放时归还内存池。
/// </summary>
public sealed class ImageFrameLease : IDisposable {
    private IMemoryOwner<byte>? _owner;

    /// <summary>创建帧租约。</summary>
    public ImageFrameLease(
        IMemoryOwner<byte> owner,
        int length,
        int width,
        int height,
        int stride,
        FramePixelFormat pixelFormat,
        DateTimeOffset capturedAt) {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, owner.Memory.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, 1);
        _owner = owner;
        Length = length;
        Width = width;
        Height = height;
        Stride = stride;
        PixelFormat = pixelFormat;
        CapturedAt = capturedAt;
    }

    /// <summary>获取有效帧数据。</summary>
    public ReadOnlyMemory<byte> Data => (_owner ?? throw new ObjectDisposedException(
        nameof(ImageFrameLease))).Memory[..Length];

    /// <summary>获取有效字节数。</summary>
    public int Length { get; }

    /// <summary>获取像素宽度。</summary>
    public int Width { get; }

    /// <summary>获取像素高度。</summary>
    public int Height { get; }

    /// <summary>获取每行字节数。</summary>
    public int Stride { get; }

    /// <summary>获取像素格式。</summary>
    public FramePixelFormat PixelFormat { get; }

    /// <summary>获取采集时间。</summary>
    public DateTimeOffset CapturedAt { get; }

    /// <inheritdoc />
    public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Dispose();
}

/// <summary>表示与 UI 几何类型无关的条码区域。</summary>
public sealed record BarcodeRegion(
    int Left,
    int Top,
    int Right,
    int Bottom);

/// <summary>表示不可变条码识别结果。</summary>
public sealed record BarcodeDetection(
    string Text,
    string Format,
    BarcodeRegion Region,
    decimal Confidence);

/// <summary>定义基于帧租约的异步条码解码器。</summary>
public interface IBarcodeDecoder {
    /// <summary>识别一帧中的条码。</summary>
    ValueTask<IReadOnlyList<BarcodeDetection>> DecodeAsync(
        ImageFrameLease frame,
        CancellationToken cancellationToken);
}
