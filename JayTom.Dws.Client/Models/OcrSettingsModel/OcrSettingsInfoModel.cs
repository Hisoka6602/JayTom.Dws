using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.OcrSettingsModel {

    public class OcrSettingsInfoModel : BindableBase {
        private bool _isUseOcr;
        private bool _isShowLogisticsCompany;
        private bool _isShowRecognitionTime;
        private bool _isShowReceiverInfo;
        private bool _isShowSenderInfo;
        private bool _isShowCompartmentNumber;

        /// <summary>
        /// 是否使用 OCR 识别
        /// </summary>
        public bool IsUseOcr {
            get => _isUseOcr;
            set => SetProperty(ref _isUseOcr, value);
        }

        /// <summary>
        /// 是否显示物流公司
        /// </summary>
        public bool IsShowLogisticsCompany {
            get => _isShowLogisticsCompany;
            set => SetProperty(ref _isShowLogisticsCompany, value);
        }

        /// <summary>
        /// 是否显示识别耗时
        /// </summary>
        public bool IsShowRecognitionTime {
            get => _isShowRecognitionTime;
            set => SetProperty(ref _isShowRecognitionTime, value);
        }

        /// <summary>
        /// 是否显示收件人信息
        /// </summary>
        public bool IsShowReceiverInfo {
            get => _isShowReceiverInfo;
            set => SetProperty(ref _isShowReceiverInfo, value);
        }

        /// <summary>
        /// 是否显示发件人信息
        /// </summary>
        public bool IsShowSenderInfo {
            get => _isShowSenderInfo;
            set => SetProperty(ref _isShowSenderInfo, value);
        }

        /// <summary>
        /// 是否显示格口号
        /// </summary>
        public bool IsShowCompartmentNumber {
            get => _isShowCompartmentNumber;
            set => SetProperty(ref _isShowCompartmentNumber, value);
        }
    }
}