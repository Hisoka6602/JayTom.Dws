using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_VolumeInfo", Schema = "dbo")]
    public class VolumeInfoModel : BasePackageForeignKeyInfoModel {

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
        /// 格式化后的长
        /// </summary>
        [Column("FormattedLength")]
        public decimal FormattedLength { get; set; }

        /// <summary>
        /// 格式化后的宽
        /// </summary>
        [Column("FormattedWidth")]
        public decimal FormattedWidth { get; set; }

        /// <summary>
        /// 格式化后的高
        /// </summary>
        [Column("FormattedHeight")]
        public decimal FormattedHeight { get; set; }

        /// <summary>
        /// 格式化的体积
        /// </summary>
        [Column("FormattedVolume")]
        public decimal FormattedVolume { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreateTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}