using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting.Rule;

namespace JayTom.Dws.Client.Models.PackageSorting {
    public class ApiSortingItemInfoModel : BasePackageSortingItemInfoModel {
        private long? _exitId;
        private string? _exitName;
        private string _sortingName = string.Empty;
        private string _sortingRuleGroup = string.Empty;
        private ObservableCollection<ApiRuleItemInfoModel>? _apiRuleItems = new();

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
        public string? ExitName {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 规则名称
        /// </summary>
        public string SortingName {
            get => _sortingName;
            set => SetProperty(ref _sortingName, value);
        }

        /// <summary>
        /// 规则组
        /// </summary>
        public string SortingRuleGroup {
            get => _sortingRuleGroup;
            set => SetProperty(ref _sortingRuleGroup, value);
        }

        public ObservableCollection<ApiRuleItemInfoModel>? ApiRuleItems {
            get => _apiRuleItems;
            set => SetProperty(ref _apiRuleItems, value);
        }
    }
}
