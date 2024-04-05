using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using Encoder = System.Drawing.Imaging.Encoder;

namespace JayTom.Dws.Plugin.SaveImage {

    public class SaveImage : ISaveImage {
        private SemaphoreSlim _semaphore = new(10);

        public async Task<KeyValuePair<bool, string>> SaveOriginalImage(Image? image, string imageName, string imagePath, WatermarkParams? watermarkParams = null,
            CancellationToken cancellationToken = default) {
            if (image is null) return new KeyValuePair<bool, string>(false, "图片不能为空!");
            try {
                await _semaphore.WaitAsync(cancellationToken);
                if (image.PixelFormat == PixelFormat.Format8bppIndexed) {
                    // 创建缩略图时使用原始的宽度和高度
                    image = image.GetThumbnailImage(image.Width, image.Height, () => false, IntPtr.Zero);
                }
                using var processedImage = new Bitmap(image);
                if (watermarkParams is not null) {
                    // 添加水印
                    var watermarkTestText = string.Join("\n", watermarkParams.WatermarkContent ?? new List<string>());

                    using (var graphics = Graphics.FromImage(processedImage)) {
                        // 在此处进行水印绘制操作
                        using var watermarkFont = new Font("Microsoft YaHei", watermarkParams.FontSize, FontStyle.Bold);
                        using var watermarkBrush = new SolidBrush(watermarkParams.WatermarkColor);

                        float x, y;
                        switch (watermarkParams.WatermarkPosition) {
                            case WatermarkPosition.TopLeft:
                                x = 10;
                                y = 10;
                                break;

                            case WatermarkPosition.TopRight:
                                x = processedImage.Width - graphics.MeasureString(watermarkTestText, watermarkFont).Width - 10;
                                y = 10;
                                break;

                            case WatermarkPosition.BottomLeft:
                                x = 10;
                                y = processedImage.Height - graphics.MeasureString(watermarkTestText, watermarkFont).Height - 10;
                                break;

                            case WatermarkPosition.BottomRight:
                                x = processedImage.Width - graphics.MeasureString(watermarkTestText, watermarkFont).Width - 10;
                                y = processedImage.Height - graphics.MeasureString(watermarkTestText, watermarkFont).Height - 10;
                                break;

                            default:
                                x = 10;
                                y = 10;
                                break;
                        }

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.DrawString(watermarkTestText, watermarkFont, watermarkBrush, x, y);
                    }
                }
                if (!Directory.Exists(imagePath)) {
                    Directory.CreateDirectory(imagePath);
                }
                // 保存修改后的图像
                processedImage.Save($"{imagePath}\\{imageName}.jpg", ImageFormat.Jpeg);
                return new KeyValuePair<bool, string>(true, "原图保存成功"); // 返回保存成功的信息
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{ex}");
                return new KeyValuePair<bool, string>(false, ex.Message); // 返回保存失败的信息
            }
            finally {
                //image?.Dispose();
                _semaphore.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> SaveCompressedImage(Image? image, string imageName, string imagePath, WatermarkParams? watermarkParams = null,
            CancellationToken cancellationToken = default) {
            if (image is null) return new KeyValuePair<bool, string>(false, "图片不能为空!");
            try {
                await _semaphore.WaitAsync(cancellationToken);
                using var bitmap = new Bitmap(image);
                // 获取原始图像的编码信息
                var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                if (jpegEncoder is not null) {
                    // 创建一个EncoderParameters对象，用于指定图像的质量
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

                    if (watermarkParams is not null) {
                        //添加水印
                        //组合水印
                        var watermarkTestText =
                            string.Join("\n", watermarkParams.WatermarkContent ?? new List<string>());
                        using var graphics = Graphics.FromImage(bitmap);
                        using var watermarkFont = new Font("Microsoft YaHei", watermarkParams.FontSize, FontStyle.Bold);
                        using var watermarkBrush = new SolidBrush(watermarkParams.WatermarkColor);

                        float x, y;
                        switch (watermarkParams.WatermarkPosition) {
                            case WatermarkPosition.TopLeft:
                                x = 10;
                                y = 10;
                                break;

                            case WatermarkPosition.TopRight:
                                x = bitmap.Width - graphics.MeasureString(watermarkTestText, watermarkFont).Width - 10;
                                y = 10;
                                break;

                            case WatermarkPosition.BottomLeft:
                                x = 10;
                                y = bitmap.Height - graphics.MeasureString(watermarkTestText, watermarkFont).Height -
                                    10;
                                break;

                            case WatermarkPosition.BottomRight:
                                x = bitmap.Width - graphics.MeasureString(watermarkTestText, watermarkFont).Width - 10;
                                y = bitmap.Height - graphics.MeasureString(watermarkTestText, watermarkFont).Height -
                                    10;
                                break;

                            default:
                                x = 10;
                                y = 10;
                                break;
                        }

                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.DrawString(watermarkTestText, watermarkFont, watermarkBrush, x, y);
                    }

                    //判断路径是否存在，不存在则创建
                    if (!Directory.Exists(imagePath)) {
                        Directory.CreateDirectory(imagePath);
                    }

                    bitmap.Save($"{imagePath}\\{imageName}.jpg", jpegEncoder, encoderParameters);
                    return new KeyValuePair<bool, string>(true, "压缩图保存成功"); // 返回保存成功的信息
                }
                else {
                    return new KeyValuePair<bool, string>(false, "转码失败");
                }
            }
            catch (Exception ex) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{ex}");
                return new KeyValuePair<bool, string>(false, ex.Message); // 返回保存失败的信息
            }
            finally {
                //如果一张图片包含两个条码则不能释放
                // image?.Dispose();
                _semaphore.Release();
            }
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format) {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}