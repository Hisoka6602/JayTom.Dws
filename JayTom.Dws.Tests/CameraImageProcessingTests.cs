using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera;

namespace JayTom.Dws.Tests;

/// <summary>
/// 验证相机图像热路径的复制、解码和缩略图行为。
/// </summary>
public sealed class CameraImageProcessingTests {
    /// <summary>验证托管帧缓冲区可以在脱离 SDK 指针后按行复制为独立位图。</summary>
    [Fact]
    public void CopyPackedFrame_FromManagedBuffer_CopiesPixels() {
        var pixels = new byte[] {
            1, 2,
            3, 4
        };

        using var bitmap = CameraImageProcessing.CopyPackedFrame(
            pixels,
            pixels.Length,
            2,
            2,
            PixelFormat.Format8bppIndexed,
            2);

        Assert.NotNull(bitmap);
        Assert.Equal(2, bitmap.Width);
        Assert.Equal(2, bitmap.Height);
        pixels.AsSpan().Clear();
        Assert.NotEqual(Color.FromArgb(255, 0, 0, 0), bitmap.GetPixel(0, 0));
    }

    /// <summary>验证紧凑像素行不会被目标步长破坏。</summary>
    [Fact]
    public void CopyPackedFrame_CopiesRowsWithoutStrideCorruption() {
        byte[] pixels = [
            0, 0, 255,
            0, 255, 0,
            255, 0, 0,
            255, 255, 255
        ];
        var pointer = Marshal.AllocHGlobal(pixels.Length);
        try {
            Marshal.Copy(pixels, 0, pointer, pixels.Length);
            using var bitmap = CameraImageProcessing.CopyPackedFrame(
                pointer,
                pixels.Length,
                2,
                2,
                PixelFormat.Format24bppRgb,
                6);

            Assert.NotNull(bitmap);
            Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
            Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(1, 0).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(0, 1).ToArgb());
            Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
        }
        finally {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>验证压缩图像可从非托管内存直接解码。</summary>
    [Fact]
    public void DecodeCompressedData_DecodesDirectlyFromUnmanagedMemory() {
        using var source = new Bitmap(37, 19, PixelFormat.Format24bppRgb);
        using var stream = new MemoryStream();
        source.Save(stream, ImageFormat.Jpeg);
        var bytes = stream.GetBuffer();
        var length = checked((int)stream.Length);
        var pointer = Marshal.AllocHGlobal(length);
        try {
            Marshal.Copy(bytes, 0, pointer, length);
            using var decoded = CameraImageProcessing.DecodeCompressedFrame(pointer, length);

            Assert.NotNull(decoded);
            Assert.Equal(37, decoded.Width);
            Assert.Equal(19, decoded.Height);
        }
        finally {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>验证缩略图尺寸正确且源图像保持不变。</summary>
    [Fact]
    public void CreateThumbnail_PreservesSourceAndUsesRequestedDimensions() {
        using var source = new Bitmap(1920, 1080, PixelFormat.Format24bppRgb);
        using var thumbnail = CameraImageProcessing.CreateThumbnail(source, 800, 600);

        Assert.NotNull(thumbnail);
        Assert.Equal(800, thumbnail.Width);
        Assert.Equal(600, thumbnail.Height);
        Assert.Equal(1920, source.Width);
        Assert.Equal(1080, source.Height);
    }
}
