using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig {

    [Table("Conf_OcrSortingInfo", Schema = "dbo")]
    public class OcrSortingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 绑定出口代码
        /// </summary>
        [Column("ExitId"), InsertOrUpdate]
        public long ExitId { get; set; }

        [Column("SortingName"), InsertOrUpdate]
        public string SortingName { get; set; } = string.Empty;

        public virtual ICollection<OcrRuleInfoModel>? OcrRuleItems { get; set; }
    }
}