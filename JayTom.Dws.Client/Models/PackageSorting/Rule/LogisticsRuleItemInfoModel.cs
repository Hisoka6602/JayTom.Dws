namespace JayTom.Dws.Client.Models.PackageSorting.Rule {

    public class LogisticsRuleItemInfoModel : BasePackageSortingItemInfoModel {

        /// <summary>
        /// 物流分拣Id
        /// </summary>
        public long LogisticsSortingId { get; set; }

        /// <summary>
        /// 物流Id
        /// </summary>
        public long LogisticsId { get; set; }

        /// <summary>
        /// 物流名称
        /// </summary>
        public string LogisticsName { get; set; } = string.Empty;
    }
}