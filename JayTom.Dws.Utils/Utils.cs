using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Utils {

    public static class Utils {

        public static string ConvertBitmapToBase64(this Bitmap bitmap) {
            try {
                using (MemoryStream memoryStream = new MemoryStream()) {
                    // 将位图保存到内存流中
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                    // 将内存流的数据转换为字节数组
                    var bitmapBytes = memoryStream.ToArray();

                    // 将字节数组转换为 Base64 编码的字符串
                    var base64String = Convert.ToBase64String(bitmapBytes);

                    return base64String;
                }
            }
            catch {
                return string.Empty;
            }
        }
    }
}