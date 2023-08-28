using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Plugin.SaveImage {

    public interface ISaveImage {

        /// <summary>
        /// 异步保存原图
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageName"></param>
        /// <param name="imagePath"></param>
        /// <param name="watermarkParams"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SaveOriginalImage(Image? image, string imageName, string imagePath, WatermarkParams? watermarkParams = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步保存压缩图
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageName"></param>
        /// <param name="imagePath"></param>
        /// <param name="watermarkParams"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SaveCompressedImage(Image? image, string imageName, string imagePath, WatermarkParams? watermarkParams = null, CancellationToken cancellationToken = default);
    }

    public class WatermarkParams {

        /// <summary>
        /// 水印字体大小
        /// </summary>
        public int FontSize { get; set; }

        /// <summary>
        /// 水印颜色
        /// </summary>
        public Color WatermarkColor { get; set; }

        /// <summary>
        /// 水印位置
        /// </summary>
        public WatermarkPosition WatermarkPosition { get; set; }

        /// <summary>
        /// 水印内容
        /// </summary>
        public List<string>? WatermarkContent { get; set; } // 水印内容
    }

    public enum WatermarkPosition {

        /// <summary>
        /// 左上角
        /// </summary>
        TopLeft,

        /// <summary>
        /// 右上角
        /// </summary>
        TopRight,

        /// <summary>
        /// 左下角
        /// </summary>
        BottomLeft,

        /// <summary>
        /// 右下角
        /// </summary>
        BottomRight
    }
}