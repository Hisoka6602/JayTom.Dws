namespace JayTom.Dws.Client.Models.PackageSorting.Rule {

    public class OcrRuleItemInfoModel : BasePackageSortingItemInfoModel {
        private long _ocrSortingId;
        private string _regexPattern = string.Empty;

        /// <summary>
        /// OcrId
        /// </summary>
        public long OcrSortingId {
            get => _ocrSortingId;
            set => SetProperty(ref _ocrSortingId, value);
        }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexPattern {
            get => _regexPattern;
            set => SetProperty(ref _regexPattern, value);
        }
    }
}