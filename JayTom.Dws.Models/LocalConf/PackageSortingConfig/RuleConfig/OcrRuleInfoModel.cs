using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf.PackageSortingConfig.RuleConfig {
    [Table("Conf_OcrRuleInfo", Schema = "dbo")]
    public class OcrRuleInfoModel : BasePackageSortingConfig {

        /// <summary>
        /// Ocr分拣Id
        /// </summary>
        [Column("OcrSortingId"), Required, InsertOrUpdate]
        public long OcrSortingId { get; set; }

        /// <summary>
        /// Json内容
        /// </summary>
        [Column("JsonContent"), Required, InsertOrUpdate]
        public string JsonContent { get; set; } = string.Empty;

        [ForeignKey(nameof(OcrSortingId))]
        public virtual OcrSortingInfoModel? OcrSortingInfo { get; set; }
    }
}
