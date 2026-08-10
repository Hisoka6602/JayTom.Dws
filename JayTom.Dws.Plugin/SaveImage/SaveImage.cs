using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Encoder = System.Drawing.Imaging.Encoder;

namespace JayTom.Dws.Plugin.SaveImage {

    /// <summary>
    /// 使用受控并发和显式 JPEG 质量保存图片。
    /// </summary>
    public class SaveImage : ISaveImage {
        private const long OriginalJpegQuality = 95L;
        private const long CompressedJpegQuality = 85L;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public Task<KeyValuePair<bool, string>> SaveOriginalImage(
            Image? image,
            string imageName,
            string imagePath,
            WatermarkParams? watermarkParams = null,
            CancellationToken cancellationToken = default) {
            return SaveJpeg(
                image,
                imageName,
                imagePath,
                watermarkParams,
                OriginalJpegQuality,
                "原图保存成功",
                cancellationToken);
        }

        public Task<KeyValuePair<bool, string>> SaveCompressedImage(
            Image? image,
            string imageName,
            string imagePath,
            WatermarkParams? watermarkParams = null,
            CancellationToken cancellationToken = default) {
            return SaveJpeg(
                image,
                imageName,
                imagePath,
                watermarkParams,
                CompressedJpegQuality,
                "压缩图保存成功",
                cancellationToken);
        }

        /// <summary>
        /// 以指定质量原子保存 JPEG 文件，并限制同时编码的图片数量。
        /// </summary>
        private async Task<KeyValuePair<bool, string>> SaveJpeg(
            Image? image,
            string imageName,
            string imagePath,
            WatermarkParams? watermarkParams,
            long quality,
            string successMessage,
            CancellationToken cancellationToken) {
            if (image is null) {
                return new KeyValuePair<bool, string>(false, "图片不能为空!");
            }

            var lockTaken = false;
            string? temporaryPath = null;
            try {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockTaken = true;
                cancellationToken.ThrowIfCancellationRequested();

                var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                if (jpegEncoder is null) {
                    return new KeyValuePair<bool, string>(false, "未找到 JPEG 编码器");
                }

                Directory.CreateDirectory(imagePath);
                var destinationPath = Path.Combine(imagePath, $"{imageName}.jpg");
                temporaryPath = Path.Combine(
                    imagePath,
                    $".{imageName}.{Guid.NewGuid():N}.tmp");

                using var writableImage = RequiresWritableCopy(image, watermarkParams)
                    ? CreateWritableCopy(image)
                    : null;
                var imageToSave = (Image?)writableImage ?? image;
                if (watermarkParams is not null) {
                    DrawWatermark(imageToSave, watermarkParams);
                }

                using var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                imageToSave.Save(temporaryPath, jpegEncoder, encoderParameters);
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, destinationPath, true);
                temporaryPath = null;

                return new KeyValuePair<bool, string>(true, successMessage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception exception) {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "保存 JPEG 图片失败");
                return new KeyValuePair<bool, string>(false, exception.Message);
            }
            finally {
                TryDeleteTemporaryFile(temporaryPath);
                if (lockTaken) {
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// 判断编码前是否需要创建可绘制的非索引副本。
        /// </summary>
        private static bool RequiresWritableCopy(Image image, WatermarkParams? watermarkParams) {
            return watermarkParams is not null ||
                   (image.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed;
        }

        /// <summary>
        /// 创建与原图分辨率一致的二十四位可绘制副本。
        /// </summary>
        private static Bitmap CreateWritableCopy(Image image) {
            var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            graphics.DrawImageUnscaled(image, 0, 0);
            return bitmap;
        }

        /// <summary>
        /// 在图片边界内绘制配置的文字水印。
        /// </summary>
        private static void DrawWatermark(Image image, WatermarkParams watermarkParams) {
            var watermarkText = string.Join("\n", watermarkParams.WatermarkContent ?? []);
            if (string.IsNullOrEmpty(watermarkText)) {
                return;
            }

            using var graphics = Graphics.FromImage(image);
            using var watermarkFont = new Font(
                "Microsoft YaHei",
                Math.Max(1, watermarkParams.FontSize),
                FontStyle.Bold);
            using var watermarkBrush = new SolidBrush(watermarkParams.WatermarkColor);
            var textSize = graphics.MeasureString(watermarkText, watermarkFont);
            const int margin = 10;
            var (x, y) = watermarkParams.WatermarkPosition switch {
                WatermarkPosition.TopRight => (image.Width - textSize.Width - margin, margin),
                WatermarkPosition.BottomLeft => (margin, image.Height - textSize.Height - margin),
                WatermarkPosition.BottomRight => (
                    image.Width - textSize.Width - margin,
                    image.Height - textSize.Height - margin),
                _ => (margin, margin)
            };

            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawString(
                watermarkText,
                watermarkFont,
                watermarkBrush,
                Math.Max(0, x),
                Math.Max(0, y));
        }

        /// <summary>
        /// 获取指定图片格式对应的编码器。
        /// </summary>
        private static ImageCodecInfo? GetEncoder(ImageFormat format) {
            return ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(codec => codec.FormatID == format.Guid);
        }

        /// <summary>
        /// 尝试清理未完成编码留下的临时文件。
        /// </summary>
        private static void TryDeleteTemporaryFile(string? temporaryPath) {
            if (string.IsNullOrEmpty(temporaryPath) || !File.Exists(temporaryPath)) {
                return;
            }

            try {
                File.Delete(temporaryPath);
            }
            catch (Exception exception) {
                NLog.LogManager.GetCurrentClassLogger()
                    .Warn(exception, $"清理临时图片失败:{temporaryPath}");
            }
        }
    }
}
