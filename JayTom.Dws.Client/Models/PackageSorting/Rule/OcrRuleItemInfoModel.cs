using System.Net.Http.Json;

namespace JayTom.Dws.Client.Models.PackageSorting.Rule {

    public class OcrRuleItemInfoModel : BasePackageSortingItemInfoModel {
        private long _ocrSortingId;
        private string _jsonContent = string.Empty;

        /// <summary>
        /// OcrId
        /// </summary>
        public long OcrSortingId {
            get => _ocrSortingId;
            set => SetProperty(ref _ocrSortingId, value);
        }

        /// <summary>
        /// 规则
        /// </summary>
        public string JsonContent {
            get => _jsonContent;
            set => SetProperty(ref _jsonContent, value);
        }
    }
}