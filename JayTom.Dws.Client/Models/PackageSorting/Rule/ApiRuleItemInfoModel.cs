namespace JayTom.Dws.Client.Models.PackageSorting.Rule {

    public class ApiRuleItemInfoModel : BasePackageSortingItemInfoModel {
        private long _apiSortingId;
        private string _jsonContent = string.Empty;
        private string _formatJsonContent = string.Empty;

        /// <summary>
        /// OcrId
        /// </summary>
        public long ApiSortingId {
            get => _apiSortingId;
            set => SetProperty(ref _apiSortingId, value);
        }

        /// <summary>
        /// 规则
        /// </summary>
        public string JsonContent {
            get => _jsonContent;
            set => SetProperty(ref _jsonContent, value);
        }

        public string FormatJsonContent {
            get => _formatJsonContent;
            set => SetProperty(ref _formatJsonContent, value);
        }
    }
}