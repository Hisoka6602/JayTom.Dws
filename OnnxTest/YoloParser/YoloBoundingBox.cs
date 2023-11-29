using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OnnxTest.YoloParser {

    /// <summary>
    /// 边界框和维度
    /// </summary>
    public class YoloBoundingBox {

        /// <summary>
        /// 包含边界框的维度
        /// </summary>
        public BoundingBoxDimensions Dimensions { get; set; } = new();

        /// <summary>
        /// 包含在边界框内检测到的对象类
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 包含类的置信度
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 包含边界框维度的矩形表示形式
        /// </summary>
        public RectangleF Rect => new(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height);

        /// <summary>
        /// 包含与用于在图像上绘制的相应类关联的颜色
        /// </summary>
        public Color BoxColor { get; set; }
    }

    public class BoundingBoxDimensions : DimensionsBase { }
}