using System.Drawing;
using System.Drawing.Imaging;

namespace JayTom.Dws.Tests.Application;

/// <summary>
/// 验证 JPEG 存图不会降低分辨率，并正确处理取消与编码质量。
/// </summary>
public sealed class SaveImageTests {
    /// <summary>
    /// 验证原图和压缩图都保留像素尺寸，且原图编码质量高于压缩图。
    /// </summary>
    [Fact]
    public async Task SaveJpeg_preserves_dimensions_and_applies_quality_levels() {
        var directory = Directory.CreateTempSubdirectory("jaytom-image-test-");
        try {
            using var source = CreateDetailedBitmap(640, 480);
            var saver = new JayTom.Dws.Plugin.SaveImage.SaveImage();

            var originalResult = await saver.SaveOriginalImage(
                source,
                "original",
                directory.FullName);
            var compressedResult = await saver.SaveCompressedImage(
                source,
                "compressed",
                directory.FullName);

            Assert.True(originalResult.Key, originalResult.Value);
            Assert.True(compressedResult.Key, compressedResult.Value);
            using var original = new Bitmap(Path.Combine(directory.FullName, "original.jpg"));
            using var compressed = new Bitmap(Path.Combine(directory.FullName, "compressed.jpg"));
            Assert.Equal(source.Size, original.Size);
            Assert.Equal(source.Size, compressed.Size);
            Assert.True(
                new FileInfo(Path.Combine(directory.FullName, "original.jpg")).Length >
                new FileInfo(Path.Combine(directory.FullName, "compressed.jpg")).Length);
            Assert.Empty(Directory.EnumerateFiles(directory.FullName, "*.tmp"));
        }
        finally {
            directory.Delete(true);
        }
    }

    /// <summary>
    /// 验证取消等待不会错误增加信号量计数，后续图片仍可正常保存。
    /// </summary>
    [Fact]
    public async Task Canceled_save_does_not_corrupt_encoder_gate() {
        var directory = Directory.CreateTempSubdirectory("jaytom-image-cancel-test-");
        try {
            using var source = new Bitmap(32, 32, PixelFormat.Format24bppRgb);
            var saver = new JayTom.Dws.Plugin.SaveImage.SaveImage();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                saver.SaveOriginalImage(
                    source,
                    "canceled",
                    directory.FullName,
                    cancellationToken: cancellation.Token));
            var result = await saver.SaveOriginalImage(
                source,
                "successful",
                directory.FullName);

            Assert.True(result.Key, result.Value);
            Assert.False(File.Exists(Path.Combine(directory.FullName, "canceled.jpg")));
            Assert.True(File.Exists(Path.Combine(directory.FullName, "successful.jpg")));
        }
        finally {
            directory.Delete(true);
        }
    }

    /// <summary>
    /// 创建包含渐变和细节线条的测试位图，使 JPEG 质量差异可稳定观测。
    /// </summary>
    private static Bitmap CreateDetailedBitmap(int width, int height) {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        for (var index = 0; index < width; index += 4) {
            using var pen = new Pen(
                Color.FromArgb(
                    index % 256,
                    (index * 3) % 256,
                    (index * 7) % 256));
            graphics.DrawLine(pen, index, 0, width - index - 1, height - 1);
        }
        return bitmap;
    }
}
