using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_NodeInfo", Schema = "dbo")]
    public class NodeInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 节点名称
        /// </summary>
        [Column("NodeName"), Required]
        public string NodeName { get; set; } = string.Empty;

        /// <summary>
        /// 节点序号
        /// </summary>
        [Column("NodeNum"), Required]
        public int NodeNum { get; set; }

        /*/// <summary>
        /// 条码
        /// </summary>
        [Column("Barcode"), Required]
        public string Barcode { get; set; } = string.Empty;*/

        /// <summary>
        /// 扫码时间
        /// </summary>
        [Column("ScanTime"), Required]
        public DateTime ScanTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 图片路径
        /// </summary>
        [Column("ImagePath")]
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// 源字符
        /// </summary>
        [Column("OriginalText")]
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// 输入序列(来源设备唯一标识)
        /// </summary>
        [Column("SerialNumber")]
        public string SerialNumber { get; set; } = string.Empty;
    }
}