using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_OcrLogInfo", Schema = "dbo")]
    public class OcrLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 条码
        /// </summary>
        [Column("BarCode")]
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码。
        /// </summary>
        [Column("VirtualNumber")]
        public string VirtualNumber { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人地址。
        /// </summary>
        [Column("RecipientAddress")]
        public string RecipientAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人姓名。
        /// </summary>
        [Column("RecipientName")]
        public string RecipientName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置收件人电话。
        /// </summary>
        [Column("RecipientPhone")]
        public string RecipientPhone { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人姓名。
        /// </summary>
        [Column("SenderName")]
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// 发件人地址
        /// </summary>
        [Column("SenderAddress")]
        public string SenderAddress { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置寄件人电话。
        /// </summary>
        [Column("SenderPhone")]
        public string SenderPhone { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置三段码。
        /// </summary>
        [Column("ThreeSegmentCode")]
        public string ThreeSegmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置虚拟号码后四位。
        /// </summary>
        [Column("VirtualNumberLast4")]
        public string VirtualNumberLast4 { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置识别时间。
        /// </summary>
        [Column("RecognitionTime")]
        public DateTime RecognitionTime { get; set; }

        /// <summary>
        /// 获取或设置耗时(ms)
        /// </summary>
        [Column("ElapsedTime")]
        public long ElapsedTime { get; set; }

        /// <summary>
        /// 获取或设置识别时间戳。
        /// </summary>
        [Column("RecognitionTimestamp")]
        public long RecognitionTimestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        [Column("CameraSerialNumber")]
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 提交图时间
        /// </summary>
        [Column("SubmitTimestamp")]
        public long SubmitTimestamp { get; set; }
    }
}