using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig {

    [Table("Conf_BarCodeSortingInfo", Schema = "dbo")]
    public class BarCodeSortingInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// 绑定出口代码
        /// </summary>
        [Column("ExitId"), InsertOrUpdata]
        public long ExitId { get; set; }

        [Column("SortingName"), InsertOrUpdata]
        public string SortingName { get; set; } = string.Empty;

        public virtual ICollection<BarCodeRegexInfoModel>? BarCodeRegexItems { get; set; }
    }
}