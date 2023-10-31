using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_WeightInfo", Schema = "dbo")]
    public class WeightInfoModel : BaseBarCodeForeignKeyInfo {

        /// <summary>
        /// 来源类型
        /// </summary>
        [Column("SourceType")]
        public SourceType SourceType { get; set; }

        /// <summary>
        /// 串口名称
        /// </summary>
        [Column("SerialPortName")]
        public string SerialPortName { get; set; } = string.Empty;

        /// <summary>
        /// 源字符
        /// </summary>
        [Column("OriginalText")]
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 格式化后重量
        /// </summary>
        [Column("FormattedWeight")]
        public double FormattedWeight { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreateTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    public enum SourceType {

        /// <summary>
        /// 串口
        /// </summary>
        SerialPort,

        /// <summary>
        /// Tcp
        /// </summary>
        Tcp,

        /// <summary>
        /// 输入
        /// </summary>
        Input,

        /// <summary>
        /// 相机
        /// </summary>
        Camera
    }
}