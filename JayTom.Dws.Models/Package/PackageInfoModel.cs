using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.Package {

    [Table("Data_PackageInfo", Schema = "dbo")]
    public class PackageInfoModel : BaseModel {

        /// <summary>
        /// 包裹时间戳Id
        /// </summary>
        [Column("PackageTimestamped"), Required]
        public long PackageTimestamped { get; set; }

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        [Column("PackageCreateTime"), Required]
        public DateTime PackageCreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 其他项
        /// </summary>
        [Column("Other")]
        public string? Other { get; set; }

        /// <summary>
        /// 条码信息
        /// </summary>
        public virtual BarCodeInfoModel? BarCodeInfo { get; set; }

        /// <summary>
        /// 称重信息
        /// </summary>
        public virtual WeightInfoModel? WeightInfo { get; set; }

        /// <summary>
        /// 体积信息
        /// </summary>
        public virtual VolumeInfoModel? VolumeInfo { get; set; }

        /// <summary>
        /// 上传信息
        /// </summary>
        public virtual UploadInfoModel? UploadInfo { get; set; }

        /// <summary>
        /// 格口信息
        /// </summary>
        public virtual ExitInfoModel? ExitInfo { get; set; }

        /// <summary>
        /// 分拣信息
        /// </summary>
        public virtual SortingInfoModel? SortingInfo { get; set; }

        /// <summary>
        /// 物流信息
        /// </summary>
        public virtual LogisticsInfoModel? LogisticsInfo { get; set; }

        /// <summary>
        /// Ocr信息
        /// </summary>
        public virtual OcrInfoModel? OcrInfo { get; set; }

        /// <summary>
        /// 图片信息
        /// </summary>
        public virtual ICollection<ImageInfoModel>? ImageInfos { get; set; }

        /// <summary>
        /// Nvr信息
        /// </summary>
        public virtual ICollection<NvrInfoModel>? NvrInfos { get; set; }

        /// <summary>
        /// 视频云信息
        /// </summary>
        public virtual CloudVideoUploadInfoModel? CloudVideoUploadInfo { get; set; }

        /// <summary>
        /// 设备信息
        /// </summary>
        public virtual DeviceInfoModel? DeviceInfo { get; set; }

        /// <summary>
        /// 聚合包裹信息
        /// </summary>
        public virtual AggregatePackagesInfoModel? AggregatePackagesInfo { get; set; }
    }
}