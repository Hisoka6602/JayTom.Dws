using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;
using JayTom.Dws.Client.Models.PackageSorting.Rule;

namespace JayTom.Dws.Client.Models.PackageSorting {
    public class WeightSortingItemInfoModel : BasePackageSortingItemInfoModel {
        private long? _exitId;
        private string? _exitName;
        private string _sortingRuleGroup = string.Empty;
        private ObservableCollection<WeightRuleItemInfoModel> _weightRuleItems = new();
        private string _sortingName = string.Empty;

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
        /// 规则组
        /// </summary>
        [DisplayName("计算规则"), MemberNotNull, ExcelInfo(Width = 8000)]
        public string SortingRuleGroup {
            get => _sortingRuleGroup;
            set => SetProperty(ref _sortingRuleGroup, value);
        }

        public ObservableCollection<WeightRuleItemInfoModel> WeightRuleItems {
            get => _weightRuleItems;
            set => SetProperty(ref _weightRuleItems, value);
        }
    }
}