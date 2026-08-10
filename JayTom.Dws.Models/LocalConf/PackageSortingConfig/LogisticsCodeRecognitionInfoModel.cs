using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {
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
        [Column("Sound"), InsertOrUpdate]
        public byte[]? SoundBytes { get; set; }

        /// <summary>
        /// 声音文件名
        /// </summary>
        [Column("SoundName"), InsertOrUpdate]
        public string? SoundName { get; set; }

        /// <summary>
        /// 物流图标
        /// </summary>
        [Column("Icon"), InsertOrUpdate]
        public byte[]? IconBytes { get; set; }

        /// <summary>
        /// 图标名称
        /// </summary>
        [Column("IconName"), InsertOrUpdate]
        public string IconName { get; set; } = string.Empty;


        public virtual ICollection<LogisticsRegexInfoModel>? LogisticsRegexItems { get; set; }
    }
}