namespace JayTom.Dws.Client.Models.PackageSorting.Rule {

    public class BarCodeRegexItemInfoModel : BasePackageSortingItemInfoModel {
        private long _barCodeSortingId;
        private string _regexPattern = string.Empty;

        /// <summary>
        /// 绑定Id
        /// </summary>
        public long BarCodeSortingId {
            get => _barCodeSortingId;
            set => SetProperty(ref _barCodeSortingId, value);
        }

        /// <summary>
        /// 正则
        /// </summary>
        public string RegexPattern {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }
    }
}