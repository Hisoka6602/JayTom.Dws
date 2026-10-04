using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Model {

    public class SavedImageInfo {
        public long PackageTimestamp { get; set; }

        /// <summary>本地存图阶段已经补齐条码、扫码时间和重量水印。</summary>
        public bool HasBarcodeScanWeightWatermark { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 图片类型
        /// </summary>
        public SaveImageType? ImageType { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }
    }
}
