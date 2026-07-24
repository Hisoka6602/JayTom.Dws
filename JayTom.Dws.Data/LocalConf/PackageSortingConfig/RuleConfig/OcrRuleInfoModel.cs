using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig {
    [Table("Conf_OcrRuleInfo", Schema = "dbo")]
    public class OcrRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// Ocr分拣Id
        /// </summary>
        [Column("OcrSortingId"), Required, InsertOrUpdata]
        public long OcrSortingId { get; set; }

        /// <summary>
        /// Json内容
        /// </summary>
        [Column("JsonContent"), Required, InsertOrUpdata]
        public string JsonContent { get; set; } = string.Empty;

        [ForeignKey(nameof(OcrSortingId))]
        public virtual OcrSortingInfoModel? OcrSortingInfo { get; set; }
    }
}
