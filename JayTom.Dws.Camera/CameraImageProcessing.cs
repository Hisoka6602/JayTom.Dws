using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace JayTom.Dws.Camera;

/// <summary>
/// 为相机热路径提供低分配的图像复制、解码与缩略图处理。
/// </summary>
public static class CameraImageProcessing {
    /// <summary>
    /// 从非托管压缩图像缓冲区直接解码，避免先复制到托管大数组。
    /// </summary>
    public static unsafe Bitmap? DecodeCompressedFrame(IntPtr source, int length) {
        if (source == IntPtr.Zero || length <= 0) {
            return null;
        }

        using var stream = new UnmanagedMemoryStream((byte*)source, length);
        using var image = Image.FromStream(stream, false, false);
        return new Bitmap(image);
    }

    /// <summary>
    /// 从现有压缩图像缓冲区解码为独立位图。
    /// </summary>
    public static Bitmap? DecodeCompressedFrame(byte[]? source, int length) {
        if (source is null || length <= 0 || length > source.Length) {
            return null;
        }

        using var stream = new MemoryStream(source, 0, length, false, true);
        using var image = Image.FromStream(stream, false, false);
        return new Bitmap(image);
    }

    /// <summary>
    /// 将非托管的紧凑像素缓冲区按行复制为独立位图，只执行一次必要的数据复制。
    /// </summary>
    public static unsafe Bitmap? CopyPackedFrame(
        IntPtr source,
        int sourceLength,
        int width,
        int height,
        PixelFormat pixelFormat,
        int sourceStride = 0) {
        if (source == IntPtr.Zero || sourceLength <= 0 || width <= 0 || height <= 0) {
            return null;
        }

        var bitsPerPixel = Image.GetPixelFormatSize(pixelFormat);
        if (bitsPerPixel is not (8 or 24 or 32)) {
            throw new NotSupportedException($"不支持的像素格式: {pixelFormat}");
        }

        var rowBytes = checked((width * bitsPerPixel + 7) / 8);
        sourceStride = sourceStride > 0 ? sourceStride : rowBytes;
        if (sourceStride < rowBytes || sourceLength < checked(sourceStride * height)) {
            throw new ArgumentException("源图像缓冲区长度不足。", nameof(sourceLength));
        }

        var bitmap = new Bitmap(width, height, pixelFormat);
        if (pixelFormat == PixelFormat.Format8bppIndexed) {
            var palette = bitmap.Palette;
            for (var index = 0; index < palette.Entries.Length; index++) {
                palette.Entries[index] = Color.FromArgb(255, index, index, index);
            }
            bitmap.Palette = palette;
        }

        try {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                pixelFormat);
            try {
                var destinationStride = Math.Abs(bitmapData.Stride);
                var sourcePointer = (byte*)source;
                var destinationPointer = (byte*)bitmapData.Scan0;
                for (var row = 0; row < height; row++) {
                    Buffer.MemoryCopy(
                        sourcePointer + row * sourceStride,
                        destinationPointer + row * destinationStride,
                        destinationStride,
                        rowBytes);
                }
            }
            finally {
                bitmap.UnlockBits(bitmapData);
            }
            return bitmap;
        }
        catch {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 使用低开销绘制管线创建固定尺寸缩略图，不修改或接管源图像。
    /// </summary>
    public static Bitmap? CreateThumbnail(Image? source, int width = 800, int height = 600) {
        if (source is null || width <= 0 || height <= 0) {
            return null;
        }

        var thumbnail = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(thumbnail);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        return thumbnail;
    }
}
