namespace JayTom.Dws.Client.Models.PackageSorting.Rule {

    public class WeightRuleItemInfoModel : BasePackageSortingItemInfoModel {
        private long _weightSortingId;
        private string _formula = string.Empty;

        /// <summary>
        /// 重量分拣Id
        /// </summary>
        public long WeightSortingId {
            get => _weightSortingId;
            set => SetProperty(ref _weightSortingId, value);
        }

        /// <summary>
        /// 规则
        /// </summary>
        public string Formula {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }
    }
}