using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Model {

    /// <summary>
    /// 存图参数
    /// </summary>
    public class ImageMessageInfo {
        public ImageHandle? Image { get; set; }

        public SaveImageType Type { get; set; }
        public string BarCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public DateTime ScanTime { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Volume { get; set; }
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机自定义名称
        /// </summary>
        public string CameraCustomName { get; set; } = string.Empty;

        public long PackageTimestamped { get; set; }
    }
}
