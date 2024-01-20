using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Models.PackageSorting.Excel
{

    public class ExcelApiSortingItemInfoModel : BasePackageSortingItemInfoModel {
        private long? _exitId;
        private string? _exitName;
        private string _sortingName = string.Empty;
        private UploadStatus _responseStatus;
        private bool _isUseStringComparison;
        private string _searchStringContent = string.Empty;
        private bool _isUseJsonField;
        private string _jsonField = string.Empty;
        private string _jsonFieldValue = string.Empty;

        /// <summary>
        /// 出口代码
        /// </summary>
        public long? ExitId {
            get => _exitId;
            set => SetProperty(ref _exitId, value);
        }

        /// <summary>
        /// 出口名称
        /// </summary>
        [DisplayName("格口名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string? ExitName {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 规则名称
        /// </summary>
        [DisplayName("规则名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string SortingName {
            get => _sortingName;
            set => SetProperty(ref _sortingName, value);
        }

        /// <summary>
        /// 上传状态
        /// </summary>
        [DisplayName("上传状态(0=成功、1=失败、2=未上传)"), MemberNotNull, ExcelInfo(Width = 8000, IsEnumToInt = true)]
        public UploadStatus ResponseStatus {
            get => _responseStatus;
            set => SetProperty(ref _responseStatus, value);
        }

        /// <summary>
        /// 是否使用字符串判断
        /// </summary>
        [DisplayName("是否使用字符串判断(1=是、0=否)"), MemberNotNull, ExcelInfo(Width = 8000, IsBooleanToInt = true)]
        public bool IsUseStringComparison {
            get => _isUseStringComparison;
            set => SetProperty(ref _isUseStringComparison, value);
        }

        /// <summary>
        /// 查找字符串内容
        /// </summary>
        [DisplayName("查找字符串内容"), MemberNotNull, ExcelInfo(Width = 6000)]
        public string SearchStringContent {
            get => _searchStringContent;
            set => SetProperty(ref _searchStringContent, value);
        }

        /// <summary>
        /// 是否使用Json取值
        /// </summary>
        [DisplayName("是否使用Json取值"), MemberNotNull, ExcelInfo(Width = 6000, IsBooleanToInt = true)]
        public bool IsUseJsonField {
            get => _isUseJsonField;
            set => SetProperty(ref _isUseJsonField, value);
        }

        /// <summary>
        /// Json字段
        /// </summary>
        [DisplayName("Json字段名"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string JsonField {
            get => _jsonField;
            set => SetProperty(ref _jsonField, value);
        }

        /// <summary>
        /// Json字段值
        /// </summary>
        [DisplayName("Json字段值"), MemberNotNull, ExcelInfo(Width = 5000)]
        public string JsonFieldValue {
            get => _jsonFieldValue;
            set => SetProperty(ref _jsonFieldValue, value);
        }
    }
}