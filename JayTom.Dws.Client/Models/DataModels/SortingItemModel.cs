using System;
using Prism.Mvvm;
using JayTom.Dws.Data.Package;
using Newtonsoft.Json.Serialization;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.Models.DataModels {

    public class SortingItemModel : BindableBase {
        private bool _isSortingUsed;
        private SortMode _sortingMode;
        private bool _isCreatedByLowerMachine;
        private CommunicationsType _communicationMethod = CommunicationsType.None;
        private string _checksumProtocolName = string.Empty;
        private ObservableCollection<InstructionInfoItemModel> _instructionInfoItems = new();
        private string _sortingCode = string.Empty;
        private string _connectionName = string.Empty;
        private bool _isAbnormalSorting;
        private AbnormalSortingType _abnormalSortingType;

        /// <summary>
        /// 是否使用分拣
        /// </summary>
        public bool IsSortingUsed {
            get => _isSortingUsed;
            set => SetProperty(ref _isSortingUsed, value);
        }

        /// <summary>
        /// 分拣流水号
        /// </summary>
        public string SortingCode {
            get => _sortingCode;
            set => SetProperty(ref _sortingCode, value);
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
        /// 连接名称
        /// </summary>
        public string ConnectionName {
            get => _connectionName;
            set => SetProperty(ref _connectionName, value);
        }

        /// <summary>
        /// 是否异常分拣
        /// </summary>
        public bool IsAbnormalSorting {
            get => _isAbnormalSorting;
            set => SetProperty(ref _isAbnormalSorting, value);
        }

        /// <summary>
        /// 异常分拣类型
        /// </summary>
        public AbnormalSortingType AbnormalSortingType {
            get => _abnormalSortingType;
            set => SetProperty(ref _abnormalSortingType, value);
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