using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("BarCodeInfo", Schema = "dbo")]
    public class BarCodeInfoModel : BaseModel {

        /// <summary>
        /// 时间戳Id
        /// </summary>
        [Column("TimestampedGuid"), Required]
        public long TimestampedGuid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        [Column("Barcode")]
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
        [Column("ScanTime")]
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        [Column("RequestTime")]
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        [Column("RequestStatus")]
        public int RequestStatus { get; set; }

        /// <summary>
        /// 上传内容
        /// </summary>
        [Column("RequestContent")]
        public string RequestContent { get; set; } = string.Empty;

        /// <summary>
        /// 接口响应内容
        /// </summary>
        [Column("ResponseContent")]
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 接口响应时间
        /// </summary>
        [Column("ResponseTime")]
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 条码图片保存路径
        /// </summary>
        [Column("BarcodeImagePath")]
        public string? BarcodeImagePath { get; set; }

        /// <summary>
        /// 全景图片保存路径
        /// </summary>
        [Column("PanoramaImagePath")]
        public string? PanoramaImagePath { get; set; }

        /// <summary>
        /// 下位机指令内容
        /// </summary>
        [Column("InstructionContent")]
        public string? InstructionContent { get; set; }

        /// <summary>
        /// 指令发送时间
        /// </summary>
        [Column("InstructionSentTime")]
        public DateTime? InstructionSentTime { get; set; }

        /// <summary>
        /// 指令发送目标地址
        /// </summary>
        [Column("DestinationAddress")]
        public string? DestinationAddress { get; set; }

        /// <summary>
        /// 其他项
        /// </summary>
        [Column("Other")]
        public string? Other { get; set; }
    }
}