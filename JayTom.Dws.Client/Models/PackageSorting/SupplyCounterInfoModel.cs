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
    }
}