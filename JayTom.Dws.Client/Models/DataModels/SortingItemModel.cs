using System;
using Prism.Mvvm;
using JayTom.Dws.Data.LocalData;

namespace JayTom.Dws.Client.Models.DataModels {

    public class SortingItemModel : BindableBase {
        private bool _isSortingUsed;
        private long _exitId;
        private long _logisticsId;
        private SortMode _sortingMode;
        private string _sentInstruction = string.Empty;
        private string _receivedInstruction = string.Empty;
        private DateTime _packageCreationTime;
        private string _packageCreationInstruction = string.Empty;
        private bool _isCreatedByLowerMachine;
        private string _commandTarget = string.Empty;
        private CommunicationsType _communicationMethod = CommunicationsType.None;
        private string _checksumProtocolName = string.Empty;

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
        /// 发送的指令
        /// </summary>
        public string SentInstruction {
            get => _sentInstruction;
            set => SetProperty(ref _sentInstruction, value);
        }

        /// <summary>
        /// 接收的指令
        /// </summary>
        public string ReceivedInstruction {
            get => _receivedInstruction;
            set => SetProperty(ref _receivedInstruction, value);
        }

        /// <summary>
        /// 创建包裹时间
        /// </summary>
        public DateTime PackageCreationTime {
            get => _packageCreationTime;
            set => SetProperty(ref _packageCreationTime, value);
        }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        public string PackageCreationInstruction {
            get => _packageCreationInstruction;
            set => SetProperty(ref _packageCreationInstruction, value);
        }

        /// <summary>
        /// 是否有下位机创建
        /// </summary>
        public bool IsCreatedByLowerMachine {
            get => _isCreatedByLowerMachine;
            set => SetProperty(ref _isCreatedByLowerMachine, value);
        }

        /// <summary>
        /// 指令目标
        /// </summary>
        public string CommandTarget {
            get => _commandTarget;
            set => SetProperty(ref _commandTarget, value);
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
    }
}