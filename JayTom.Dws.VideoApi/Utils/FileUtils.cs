using System.Drawing;
using System.Drawing.Imaging;

namespace JayTom.Dws.VideoApi.Utils {

    public class FileUtils {
        /// <summary>
        /// 转图片
        /// </summary>
        /// <param name="formFile"></param>
        /// <returns></returns>
        /*public static Task<Bitmap?> ConvertIFormFileToBitmapAsync(IFormFile formFile) {
            if (formFile is { Length: > 0 }) {
                using var stream = formFile.OpenReadStream();
                return Task.FromResult((Bitmap)Image.FromStream(stream));
            }
            return Task.FromResult<Bitmap?>(null);
        }*/

        public static Bitmap? ConvertIFormFileToBitmap(IFormFile formFile) {
            /*if (formFile is { Length: > 0 }) {
                using var stream = formFile.OpenReadStream();
                return (Bitmap)Image.FromStream(stream);
            }
            return null;*/
            if (formFile is { Length: > 0 }) {
                using var stream = formFile.OpenReadStream();
                var originalImage = Image.FromStream(stream);
                // 如果图像的像素格式不是32位，进行转换
                if (originalImage.PixelFormat != PixelFormat.Format32bppArgb) {
                    var convertedImage = new Bitmap(originalImage.Width, originalImage.Height, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(convertedImage)) {
                        g.DrawImage(originalImage, new Rectangle(0, 0, originalImage.Width, originalImage.Height));
                    }
                    originalImage.Dispose();
                    return convertedImage;
                }

                return (Bitmap)originalImage;
            }
            return null;
        }
    }
}