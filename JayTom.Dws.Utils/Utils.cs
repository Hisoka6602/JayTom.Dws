using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JayTom.Dws.Utils {

    public static class Utils {

        public static string ConvertImageToBase64(this Image image) {
            try {
                using (MemoryStream memoryStream = new MemoryStream()) {
                    image.Save(memoryStream, image.RawFormat);

                    var imageBytes = memoryStream.ToArray();

                    var base64String = Convert.ToBase64String(imageBytes);

                    return base64String;
                }
            }
            catch {
                return string.Empty;
            }
        }

        public static Image ConvertBase64ToImage(this string base64String) {
            try {
                var imageBytes = Convert.FromBase64String($"{base64String}");

                using (MemoryStream memoryStream = new MemoryStream(imageBytes)) {
                    // 将字节数组转换为Image对象
                    return Image.FromStream(memoryStream);
                }
            }
            catch {
                return null;
            }
        }

        public static Image AddTextWatermark(this Image image, string watermarkText, Color brushColor, int fontSize = 70, string familyName = "Arial") {
            try {
                using var graphics = Graphics.FromImage(image);
                using var font = new Font(familyName, fontSize, FontStyle.Bold);
                using var brush = new SolidBrush(brushColor);
                graphics.DrawString(watermarkText, font, brush, new PointF(10, 10));
            }
            catch (Exception) {
                return image;
            }
            return image;
        }
    }
}