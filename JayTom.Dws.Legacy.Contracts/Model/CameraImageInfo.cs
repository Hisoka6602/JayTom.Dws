using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Abstractions.Imaging;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Model {

    public class CameraImageInfo {

        /// <summary>
        /// 图片
        /// </summary>
        public ImageHandle? Image { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 图片路径
        /// </summary>
        public string? ImageFilePath { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 条码时间戳
        /// </summary>
        [Newtonsoft.Json.JsonProperty("BarcodeTimestamp")]
        public long PackageTimestampMilliseconds { get; set; }
    }
}
