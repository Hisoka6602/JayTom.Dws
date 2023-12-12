using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_BarCodeInfo", Schema = "dbo")]
    public class BarCodeInfoModel : BaseModel {

        /// <summary>
        /// 时间戳Id
        /// </summary>
        [Column("TimestampedGuid"), Required]
        public long TimestampedGuid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        [Column("Barcode"), Required]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 重量
        /// </summary>
        [Column("Weight")]
        public float Weight { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        [Column("Volume")]
        public float Volume { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        [Column("Length")]
        public float Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        [Column("Width")]
        public float Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        [Column("Height")]
        public float Height { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        [Column("ScanTime"), Required]
        public DateTime ScanTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        [Column("RequestStatus")]
        public UploadStatus RequestStatus { get; set; } = UploadStatus.NotUploaded;

        /// <summary>
        /// 其他项
        /// </summary>
        [Column("Other")]
        public string? Other { get; set; }

        /// <summary>
        /// 分拣信息
        /// </summary>
        public virtual SortingInfoModel? SortingInfo { get; set; }

        /// <summary>
        /// 上传信息
        /// </summary>
        public virtual UploadInfoModel? UploadInfo { get; set; }

        /// <summary>
        /// 体积信息
        /// </summary>
        public virtual VolumeInfoModel? VolumeInfo { get; set; }

        /// <summary>
        /// 称重信息
        /// </summary>
        public virtual WeightInfoModel? WeightInfo { get; set; }

        /// <summary>
        /// Ocr信息
        /// </summary>
        public virtual OcrInfoModel? OcrInfo { get; set; }

        /// <summary>
        /// 图片信息
        /// </summary>
        public virtual ICollection<ImageInfoModel>? ImageInfos { get; set; }

        /// <summary>
        /// 视频云信息
        /// </summary>
        public virtual CloudVideoUploadInfoModel? CloudVideoUploadInfo { get; set; }
    }

    public enum UploadStatus {

        /// <summary>
        /// 上传成功
        /// </summary>
        Succeeded = 0,

        /// <summary>
        /// 上传失败
        /// </summary>
        Failed = 1,

        /// <summary>
        /// 未上传
        /// </summary>
        NotUploaded = 2
    }
}