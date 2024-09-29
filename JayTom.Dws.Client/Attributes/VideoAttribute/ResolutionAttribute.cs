using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Attributes.VideoAttribute {
    /// <summary>
    /// 清晰度分辨率
    /// </summary>

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class ResolutionAttribute : Attribute {
        public int Width { get; }
        public int Height { get; }

        public ResolutionAttribute(int width, int height) {
            Width = width;
            Height = height;
        }
    }
}