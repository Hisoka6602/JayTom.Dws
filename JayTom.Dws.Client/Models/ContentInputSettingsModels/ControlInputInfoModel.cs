using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ContentInputSettingsModels {

    public class ControlInputInfoModel : BindableBase {
        private bool _isReceiveBarcode;
        private bool _isReceiveWeight;
        private bool _isReceiveLength;
        private bool _isReceiveWidth;
        private bool _isReceiveHeight;
        private bool _isReceiveVolume;

        /// <summary>
        /// 是否接收条码
        /// </summary>
        public bool IsReceiveBarcode {
            get => _isReceiveBarcode;
            set => SetProperty(ref _isReceiveBarcode, value);
        }

        /// <summary>
        /// 是否接收重量
        /// </summary>
        public bool IsReceiveWeight {
            get => _isReceiveWeight;
            set => SetProperty(ref _isReceiveWeight, value);
        }

        /// <summary>
        /// 是否接收长度
        /// </summary>
        public bool IsReceiveLength {
            get => _isReceiveLength;
            set => SetProperty(ref _isReceiveLength, value);
        }

        /// <summary>
        /// 是否接收宽度
        /// </summary>
        public bool IsReceiveWidth {
            get => _isReceiveWidth;
            set => SetProperty(ref _isReceiveWidth, value);
        }

        /// <summary>
        /// 是否接收高度
        /// </summary>
        public bool IsReceiveHeight {
            get => _isReceiveHeight;
            set => SetProperty(ref _isReceiveHeight, value);
        }

        /// <summary>
        /// 是否接收体积
        /// </summary>
        public bool IsReceiveVolume {
            get => _isReceiveVolume;
            set => SetProperty(ref _isReceiveVolume, value);
        }
    }
}