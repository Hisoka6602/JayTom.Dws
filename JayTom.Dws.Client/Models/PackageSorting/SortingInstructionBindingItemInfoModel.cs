using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        public string? ExitName {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 延迟发送(ms)
        /// </summary>
        public int DelaySendMilliseconds {
            get => _delaySendMilliseconds;
            set => SetProperty(ref _delaySendMilliseconds, value);
        }

        /// <summary>
        /// 发送间隔(ms)
        /// </summary>
        public int SendIntervalMilliseconds {
            get => _sendIntervalMilliseconds;
            set => SetProperty(ref _sendIntervalMilliseconds, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
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