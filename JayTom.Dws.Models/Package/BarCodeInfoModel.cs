using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_BarCodeInfo", Schema = "dbo")]
    public class BarCodeInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 条码
        /// </summary>
        [Column("Barcode"), Required]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        [Column("ScanTime"), Required]
        public DateTime ScanTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 来源
        /// </summary>
        [Column("Source")]
        public SourceType Source { get; set; }

        /// <summary>
        /// 输入序列(来源设备唯一标识)
        /// </summary>
        [Column("SerialNumber")]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 显示标识
        /// </summary>
        [Column("DisplayIdentifier")]
        public string DisplayIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// 源字符
        /// </summary>
        [Column("OriginalText"), NotMapped]
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 绑定时间
        /// </summary>

        [Column("BindTime"), NotMapped]
        public DateTime BindTime { get; set; } = DateTime.Now;
    }

    public enum BarCodeSourceType {

        /// <summary>
        /// 外部输入
        /// </summary>
        ExternalInput,

        /// <summary>
        /// 相机
        /// </summary>
        Camera,

        /// <summary>
        /// 控件输入
        /// </summary>
        ControlInput
    }
}