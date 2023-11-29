using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OnnxTest.YoloParser {

    public class DimensionsBase {

        /// <summary>
        /// 包含对象沿 x 轴的位置。
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// 包含对象沿 y 轴的位置。
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// 包含对象的高度。
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// 包含对象的宽度。
        /// </summary>
        public float Width { get; set; }
    }
}