using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class ImageSettinngsDto {

        /// <summary>
        /// 存图根目录
        /// </summary>
        public string ImageRootDirectory { get; set; } = string.Empty;

        /// <summary>
        ///  是否保存扫码图
        /// </summary>
        public bool IsSaveBarcodeImage { get; set; }

        /// <summary>
        /// 是否保存全景图
        /// </summary>
        public bool IsSavePanoramaImage { get; set; }

        /// <summary>
        /// 是否保存体积图
        /// </summary>
        public bool IsSaveVolumeImage { get; set; }

        /// <summary>
        /// 是否保存原图
        /// </summary>
        public bool IsSaveOriginalImage { get; set; }

        /// <summary>
        /// 是否使用水印
        /// </summary>
        public bool IsUseWatermark { get; set; }
    }

    public class WatermarkInfo {
    }
}