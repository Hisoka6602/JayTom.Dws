using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class SupplyCounterInfoModel : BindableBase {
        private bool _isUseSupplyCounterMode;
        private bool _sendPreSequenceNumber;
        private bool _waitForVolumeInformation;
        private int _startPrecedingNumber;
        private int _precedingSignalMaxValue;
        private bool _isWaitForPrecedingSignalReplyBeforeCreatingNewPackage;
        private bool _isWaitForBindingCarSignalToCompletePackage;
        private int _precedingReplySignalTimeout;
        private int _bindingCarSignalReplyTimeout = 2000;
        private bool _removePackageAfterSignalTimeout;
        private bool _clearPackagesOnReset;
        private bool _resetFilterAfterRemovingPackage;

        /// <summary>
        /// 是否使用供包台模式
        /// </summary>
        public bool IsUseSupplyCounterMode {
            get => _isUseSupplyCounterMode;
            set => SetProperty(ref _isUseSupplyCounterMode, value);
        }

        /// <summary>
        /// 是否发送前置序号
        /// </summary>
        public bool SendPreSequenceNumber {
            get => _sendPreSequenceNumber;
            set => SetProperty(ref _sendPreSequenceNumber, value);
        }

        /// <summary>
        /// 是否等待体积信息
        /// </summary>
        public bool WaitForVolumeInformation {
            get => _waitForVolumeInformation;
            set => SetProperty(ref _waitForVolumeInformation, value);
        }

        /// <summary>
        /// 起始前置序号
        /// </summary>
        public int StartPrecedingNumber {
            get => _startPrecedingNumber;
            set => SetProperty(ref _startPrecedingNumber, value);
        }

        /// <summary>
        /// 前置信号极限值
        /// </summary>
        public int PrecedingSignalMaxValue {
            get => _precedingSignalMaxValue;
            set => SetProperty(ref _precedingSignalMaxValue, value);
        }

        /// <summary>
        /// 是否等待前置信号回复再创建新包裹
        /// </summary>
        public bool IsWaitForPrecedingSignalReplyBeforeCreatingNewPackage {
            get => _isWaitForPrecedingSignalReplyBeforeCreatingNewPackage;
            set => SetProperty(ref _isWaitForPrecedingSignalReplyBeforeCreatingNewPackage, value);
        }

        /// <summary>
        /// 是否等待绑定车号信号再完成包裹
        /// </summary>
        public bool IsWaitForBindingCarSignalToCompletePackage {
            get => _isWaitForBindingCarSignalToCompletePackage;
            set => SetProperty(ref _isWaitForBindingCarSignalToCompletePackage, value);
        }

        /// <summary>
        /// 前置回复信号超时时间
        /// </summary>
        public int PrecedingReplySignalTimeout {
            get => _precedingReplySignalTimeout;
            set => SetProperty(ref _precedingReplySignalTimeout, value);
        }

        /// <summary>
        /// 绑定信号回复超时时间
        /// </summary>
        public int BindingCarSignalReplyTimeout {
            get => _bindingCarSignalReplyTimeout;
            set => SetProperty(ref _bindingCarSignalReplyTimeout, value);
        }

        /// <summary>
        /// 前置信号超时后移除包裹
        /// </summary>
        public bool RemovePackageAfterSignalTimeout {
            get => _removePackageAfterSignalTimeout;
            set => SetProperty(ref _removePackageAfterSignalTimeout, value);
        }

        /// <summary>
        /// 复位后是否清空包裹
        /// </summary>
        public bool ClearPackagesOnReset {
            get => _clearPackagesOnReset;
            set => SetProperty(ref _clearPackagesOnReset, value);
        }

        /// <summary>
        /// 移除包裹后是否重置过滤
        /// </summary>
        public bool ResetFilterAfterRemovingPackage {
            get => _resetFilterAfterRemovingPackage;
            set => SetProperty(ref _resetFilterAfterRemovingPackage, value);
        }
    }
}