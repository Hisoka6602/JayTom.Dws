using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.OcrSettingsModel {
    public class OcrSettingsInfoModel : BindableBase {
        private bool _isUseOcr;
        private bool _isThreeSegmentCode;
        private bool _isShowRecognitionTime;
        private bool _isShowReceiverInfo;
        private bool _isShowSenderInfo;
        private int _recognitionTimeout;

        /// <summary>
        /// 是否使用 OCR 识别
        /// </summary>
        public bool IsUseOcr {
            get => _isUseOcr;
            set => SetProperty(ref _isUseOcr, value);
        }

        /// <summary>
        /// 是否识别三段码
        /// </summary>
        public bool IsThreeSegmentCode {
            get => _isThreeSegmentCode;
            set => SetProperty(ref _isThreeSegmentCode, value);
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
        /// 识别超时时间
        /// </summary>
        public int RecognitionTimeout {
            get => _recognitionTimeout;
            set => SetProperty(ref _recognitionTimeout, value);
        }
    }
}