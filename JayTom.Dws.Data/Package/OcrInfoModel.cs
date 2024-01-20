using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_OcrInfo", Schema = "dbo")]
    public class OcrInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 原始内容
        /// </summary>
        [Column("OriginalContent")]
        public string OriginalContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用Ocr
        /// </summary>
        [Column("IsUseOcr")]
        public bool IsUseOcr { get; set; }

        /// <summary>
        /// 三段码
        /// </summary>
        [Column("ThreeSegmentCode")]
        public string ThreeSegmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 识别耗时
        /// </summary>
        [Column("ElapsedMilliseconds")]
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 识别时间
        /// </summary>
        [Column("RecognizeTime")]
        public DateTime RecognizeTime { get; set; }

        /// <summary>
        /// 虚拟号码后四位。
        /// </summary>
        [Column("VirtualNumberLast4")]
        public string VirtualNumberLast4 { get; set; } = string.Empty;

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

        /// <summary>
        /// 详细信息
        /// </summary>
        public virtual ICollection<OcrDetailedInfoModel>? OcrDetailedInfos { get; set; }
    }
}