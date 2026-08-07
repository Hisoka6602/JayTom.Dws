using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Plugin.Excel.Attributes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Client.Models.PackageSorting
{
    public class BarCodeSortingItemInfoModel : BasePackageSortingItemInfoModel
    {
        private long? _exitId;
        private string? _exitName;
        private string _sortingRuleGroup = string.Empty;
        private ObservableCollection<BarCodeRegexItemInfoModel> _barCodeRegexItems = new();
        private string _sortingName = string.Empty;

        /// <summary>
        /// 出口代码
        /// </summary>
        public long? ExitId
        {
            get => _exitId;
            set => SetProperty(ref _exitId, value);
        }

        /// <summary>
        /// 出口名称
        /// </summary>
        [DisplayName("格口名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string? ExitName
        {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 规则名称
        /// </summary>
        [DisplayName("规则名称"), MemberNotNull, ExcelInfo(Width = 4000)]
        public string SortingName
        {
            get => _sortingName;
            set => SetProperty(ref _sortingName, value);
        }

        /// <summary>
        /// 规则组
        /// </summary>
        [DisplayName("正则表达式"), MemberNotNull, ExcelInfo(Width = 8000)]
        public string SortingRuleGroup
        {
            get => _sortingRuleGroup;
            set => SetProperty(ref _sortingRuleGroup, value);
        }

        public ObservableCollection<BarCodeRegexItemInfoModel> BarCodeRegexItems
        {
            get => _barCodeRegexItems;
            set => SetProperty(ref _barCodeRegexItems, value);
        }
    }
}