using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace JayTom.Dws.Interface.Jtexpress {

    /// <summary>
    /// 为极昼扫描上报生成满足图片服务大小限制的图片内容。
    /// </summary>
    internal static class JtPolarDayScanImageProcessor {
        /// <summary>
        /// 极昼图片接口允许的最大文件大小。
        /// </summary>
        internal const int MaxImageSizeBytes = 409600;

        /// <summary>
        /// 每轮缩放后保留的尺寸百分比。
        /// </summary>
        private const int ResizePercentage = 80;

        /// <summary>
        /// 最大尺寸缩减次数，防止异常图片导致无限处理。
        /// </summary>
        private const int MaxResizeAttempts = 8;

        /// <summary>
        /// 按顺序尝试的 JPEG 质量参数。
        /// </summary>
        private static readonly long[] JpegQualities = [85L, 70L, 55L, 40L];

        /// <summary>
        /// 系统 JPEG 编码器。
        /// </summary>
        private static readonly ImageCodecInfo JpegCodec =
            ImageCodecInfo.GetImageEncoders().First(codec =>
                codec.FormatID == ImageFormat.Jpeg.Guid);

        /// <summary>
        /// 将存图阶段已经处理完成的图片转换为符合大小限制的 JPEG 内容。
        /// </summary>
        /// <param name="source">已经完成水印处理的扫描图片。</param>
        /// <returns>不超过接口限制的 JPEG 字节。</returns>
        /// <exception cref="InvalidOperationException">图片无法压缩到接口限制内。</exception>
        internal static byte[] CreateUploadContent(Image source) {
            ArgumentNullException.ThrowIfNull(source);
            using var initialBitmap = new Bitmap(source);
            var currentBitmap = initialBitmap;
            Bitmap? resizedBitmap = null;

            try {
                for (var resizeAttempt = 0;
                     resizeAttempt <= MaxResizeAttempts;
                     resizeAttempt++) {
                    foreach (var quality in JpegQualities) {
                        var content = WriteJpeg(currentBitmap, quality);
                        if (content.Length <= MaxImageSizeBytes) {
                            return content;
                        }
                    }

                    if (resizeAttempt == MaxResizeAttempts ||
                        currentBitmap.Width <= 1 ||
                        currentBitmap.Height <= 1) {
                        break;
                    }

                    var nextWidth = Math.Max(
                        1,
                        currentBitmap.Width * ResizePercentage / 100);
                    var nextHeight = Math.Max(
                        1,
                        currentBitmap.Height * ResizePercentage / 100);
                    var nextBitmap = ResizeBitmap(
                        currentBitmap,
                        nextWidth,
                        nextHeight);
                    resizedBitmap?.Dispose();
                    resizedBitmap = nextBitmap;
                    currentBitmap = resizedBitmap;
                }
            }
            finally {
                resizedBitmap?.Dispose();
            }

            throw new InvalidOperationException(
                $"极昼扫描图片压缩后仍超过 {MaxImageSizeBytes} 字节限制");
        }

        /// <summary>
        /// 按指定尺寸创建高质量位图副本。
        /// </summary>
        private static Bitmap ResizeBitmap(
            Image source,
            int width,
            int height) {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, 0, 0, width, height);
            return bitmap;
        }

        /// <summary>
        /// 使用指定质量参数写入 JPEG 字节。
        /// </summary>
        private static byte[] WriteJpeg(Image image, long quality) {
            using var stream = new MemoryStream();
            using var encoderParameters = new EncoderParameters(1);
            using var qualityParameter = new EncoderParameter(
                Encoder.Quality,
                quality);
            encoderParameters.Param[0] = qualityParameter;
            image.Save(stream, JpegCodec, encoderParameters);
            return stream.ToArray();
        }
    }
}
