using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig {
    [Table("Conf_LogisticsCodeRecognitionInfo", Schema = "dbo")]
    public class LogisticsCodeRecognitionInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 物流代码
        /// </summary>
        [Column("LogisticsCode"), Required, UpdateBy]
        public string LogisticsCode { get; set; } = string.Empty;

        /// <summary>
        /// 物流名称
        /// </summary>
        [Column("LogisticsName"), Required, InsertOrUpdate]
        public string LogisticsName { get; set; } = string.Empty;

        /// <summary>
        /// 物流声音
        /// </summary>
        [NotMapped]
        public byte[]? SoundBytes { get; set; }

        /// <summary>数据库外部声音文件的稳定引用。</summary>
        [Column("SoundFileReference"), InsertOrUpdate]
        public string? SoundFileReference { get; set; }

        /// <summary>
        /// 声音文件名
        /// </summary>
        [Column("SoundName"), InsertOrUpdate]
        public string? SoundName { get; set; }

        /// <summary>
        /// 物流图标
        /// </summary>
        [NotMapped]
        public byte[]? IconBytes { get; set; }

        /// <summary>数据库外部图标文件的稳定引用。</summary>
        [Column("IconFileReference"), InsertOrUpdate]
        public string? IconFileReference { get; set; }

        /// <summary>
        /// 图标名称
        /// </summary>
        [Column("IconName"), InsertOrUpdate]
        public string IconName { get; set; } = string.Empty;


        public virtual ICollection<LogisticsRegexInfoModel>? LogisticsRegexItems { get; set; }
    }
}
