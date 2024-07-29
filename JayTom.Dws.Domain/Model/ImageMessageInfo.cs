using System;
using System.Linq;
using System.Text;
using System.Drawing;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Model {

    /// <summary>
    /// 存图参数
    /// </summary>
    public class ImageMessageInfo {
        public Image? Image { get; set; }

        public SaveImageType Type { get; set; }
        public string BarCode { get; set; } = string.Empty;
        public float Weight { get; set; }
        public DateTime ScanTime { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Volume { get; set; }
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