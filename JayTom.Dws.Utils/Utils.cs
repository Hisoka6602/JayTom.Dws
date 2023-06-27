using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Utils {

    public static class Utils {

        public static string ConvertImageToBase64(this Image image) {
            try {
                using (MemoryStream memoryStream = new MemoryStream()) {
                    // 将Image对象保存到内存流中
                    image.Save(memoryStream, image.RawFormat);

                    // 将内存流中的图像数据转换为字节数组
                    var imageBytes = memoryStream.ToArray();

                    // 将字节数组转换为Base64字符串
                    var base64String = Convert.ToBase64String(imageBytes);

                    return base64String;
                }
            }
            catch {
                return string.Empty;
            }
        }
    }
}