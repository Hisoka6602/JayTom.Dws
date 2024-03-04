using System;
using Prism.Mvvm;
using JayTom.Dws.Data.Package;
using Newtonsoft.Json.Serialization;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.Models.DataModels {

    public class SortingItemModel : BindableBase {
        private bool _isSortingUsed;
        private long _exitId;
        private long _logisticsId;
        private SortMode _sortingMode;
        private bool _isCreatedByLowerMachine;
        private CommunicationsType _communicationMethod = CommunicationsType.None;
        private string _checksumProtocolName = string.Empty;
        private ObservableCollection<InstructionInfoItemModel> _instructionInfoItems = new();

        /// <summary>
        /// 是否使用分拣
        /// </summary>
        public bool IsSortingUsed {
            get => _isSortingUsed;
            set => SetProperty(ref _isSortingUsed, value);
        }

        /// <summary>
        /// 格口Id
        /// </summary>
        public long ExitId {
            get => _exitId;
            set => SetProperty(ref _exitId, value);
        }

        /// <summary>
        /// 物流Id
        /// </summary>
        public long LogisticsId {
            get => _logisticsId;
            set => SetProperty(ref _logisticsId, value);
        }

        /// <summary>
        /// 分拣模式
        /// </summary>
        public SortMode SortingMode {
            get => _sortingMode;
            set => SetProperty(ref _sortingMode, value);
        }

        /// <summary>
        /// 是否有下位机创建
        /// </summary>
        public bool IsCreatedByLowerMachine {
            get => _isCreatedByLowerMachine;
            set => SetProperty(ref _isCreatedByLowerMachine, value);
        }

        /// <summary>
        /// 通讯方式
        /// </summary>
        public CommunicationsType CommunicationMethod {
            get => _communicationMethod;
            set => SetProperty(ref _communicationMethod, value);
        }

        /// <summary>
        /// 效验协议名称
        /// </summary>
        public string ChecksumProtocolName {
            get => _checksumProtocolName;
            set => SetProperty(ref _checksumProtocolName, value);
        }

        /// <summary>
        /// 指令信息
        /// </summary>
        public ObservableCollection<InstructionInfoItemModel> InstructionInfoItems {
            get => _instructionInfoItems;
            set => SetProperty(ref _instructionInfoItems, value);
        }
    }
}