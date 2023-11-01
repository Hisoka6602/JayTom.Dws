using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Plugin.Excel.Attributes;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class SortingInstructionBindingItemInfoModel : BasePackageSortingItemInfoModel {
        private ObservableCollection<SortingInstructionItemInfoModel> _sortingInstructionItems = new();
        private string _sortingInstructionGroup = string.Empty;
        private long? _exitId;
        private string? _exitName;
        private int _delaySendMilliseconds;
        private int _sendIntervalMilliseconds;
        private bool _isActive;

        /// <summary>
        /// 分拣指令组(字符串)
        /// </summary>
        [DisplayName("分拣指令组"), MemberNotNull, ExcelInfo(Width = 8000)]
        public string SortingInstructionGroup {
            get => _sortingInstructionGroup;
            set => SetProperty(ref _sortingInstructionGroup, value);
        }

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
        /// 延迟发送(ms)
        /// </summary>
        [DisplayName("延迟发送(ms)"), MemberNotNull, ExcelInfo(Width = 4000)]
        public int DelaySendMilliseconds {
            get => _delaySendMilliseconds;
            set => SetProperty(ref _delaySendMilliseconds, value);
        }

        /// <summary>
        /// 发送间隔(ms)
        /// </summary>
        [DisplayName("发送间隔(ms)"), MemberNotNull, ExcelInfo(Width = 4000)]
        public int SendIntervalMilliseconds {
            get => _sendIntervalMilliseconds;
            set => SetProperty(ref _sendIntervalMilliseconds, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        [DisplayName("是否生效(0=不生效、1=生效)"), MemberNotNull, ExcelInfo(Width = 4000, IsBooleanToInt = true)]
        public bool IsActive {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public ObservableCollection<SortingInstructionItemInfoModel> SortingInstructionItems {
            get => _sortingInstructionItems;
            set => SetProperty(ref _sortingInstructionItems, value);
        }
    }
}