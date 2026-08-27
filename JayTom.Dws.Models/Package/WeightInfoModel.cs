using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.Package {

    [Table("Data_WeightInfo", Schema = "dbo")]
    public class WeightInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 来源类型
        /// </summary>
        [Column("SourceType")]
        public SourceType SourceType { get; set; }

        /// <summary>
        /// 源字符
        /// </summary>
        [Column("OriginalText")]
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 格式化后重量
        /// </summary>
        [Column("FormattedWeight")]
        public decimal FormattedWeight { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreateTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 称重模式
        /// </summary>
        [Column("WeighingMode")]
        public WeighingMode WeighingMode { get; set; } = WeighingMode.None;
    }

    public enum SourceType {

        /// <summary>
        /// 无
        /// </summary>
        [Description("无")]
        None = 0,

        /// <summary>
        /// 串口
        /// </summary>
        [Description("串口")]
        SerialPort = 1,

        /// <summary>
        /// Tcp
        /// </summary>
        [Description("TCP")]
        Tcp = 2,

        /// <summary>
        /// 输入
        /// </summary>
        [Description("控件输入")]
        Input = 3,

        /// <summary>
        /// 相机
        /// </summary>
        [Description("相机")]
        Camera = 4,

        /// <summary>
        /// Ocr创建
        /// </summary>
        [Description("Ocr推理")]
        Ocr = 5,

        /// <summary>
        /// 扫码枪
        /// </summary>
        [Description("扫码枪")]
        BarcodeScanner = 6
    }

    /// <summary>
    /// 称重模式
    /// </summary>
    public enum WeighingMode {

        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 动态
        /// </summary>
        Dynamic = 1,

        /// <summary>
        /// 静态
        /// </summary>
        Static = 2
    }
}