using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras {

    public class BarcodeReaderSettingsInfoModel : BindableBase {
        private bool _isUseOrCode = true;
        private bool _isUseMicroQr = true;
        private bool _isUseCode39 = true;
        private bool _isUseCode93 = true;
        private bool _isUseCode128 = true;
        private bool _isUseCodeBar = true;
        private bool _isUseItf = true;
        private bool _ean13 = true;
        private bool _ean8 = true;
        private int _localizationMode;
        private int _deblurLevel;
        private int _expectedBarcodesCount;
        private int _scaleDownThreshold;
        private bool _isUseTextFilterMode;
        private bool _isUseRegionPredetectionMode;
        private int _grayscaleTransformationMode;
        private int _imagePreprocessingMode;
        private int _minResultConfidence;
        private int _textureDetectionSensitivity;
        private int _binarizationBlockSize;
        private int _recognitionMode;
        private int _recognitionSkipFrames;

        /// <summary>
        /// 是否使用OrCode码
        /// </summary>
        public bool IsUseOrCode {
            get => _isUseOrCode;
            set => SetProperty(ref _isUseOrCode, value);
        }

        /// <summary>
        /// 是否使用MicroQR
        /// </summary>
        public bool IsUseMicroQr {
            get => _isUseMicroQr;
            set => SetProperty(ref _isUseMicroQr, value);
        }

        /// <summary>
        /// 是否使用Code39
        /// </summary>
        public bool IsUseCode39 {
            get => _isUseCode39;
            set => SetProperty(ref _isUseCode39, value);
        }

        /// <summary>
        /// 是否使用Code93
        /// </summary>
        public bool IsUseCode93 {
            get => _isUseCode93;
            set => SetProperty(ref _isUseCode93, value);
        }

        /// <summary>
        /// 是否使用Code128
        /// </summary>
        public bool IsUseCode128 {
            get => _isUseCode128;
            set => SetProperty(ref _isUseCode128, value);
        }

        /// <summary>
        /// 是否使用CodeBar
        /// </summary>
        public bool IsUseCodeBar {
            get => _isUseCodeBar;
            set => SetProperty(ref _isUseCodeBar, value);
        }

        /// <summary>
        /// 是否使用ITF
        /// </summary>
        public bool IsUseItf {
            get => _isUseItf;
            set => SetProperty(ref _isUseItf, value);
        }

        /// <summary>
        /// 是否使用Ean13
        /// </summary>
        public bool Ean13 {
            get => _ean13;
            set => SetProperty(ref _ean13, value);
        }

        /// <summary>
        /// 是否使用
        /// </summary>
        public bool Ean8 {
            get => _ean8;
            set => SetProperty(ref _ean8, value);
        }

        /// <summary>
        /// 本地化模式
        /// </summary>
        public int LocalizationMode {
            get => _localizationMode;
            set => SetProperty(ref _localizationMode, value);
        }

        /// <summary>
        /// 去模糊级别
        /// </summary>
        public int DeblurLevel {
            get => _deblurLevel;
            set => SetProperty(ref _deblurLevel, value);
        }

        /// <summary>
        /// 期望的条形码数量
        /// </summary>
        public int ExpectedBarcodesCount {
            get => _expectedBarcodesCount;
            set => SetProperty(ref _expectedBarcodesCount, value);
        }

        /// <summary>
        /// 缩放阈值
        /// </summary>
        public int ScaleDownThreshold {
            get => _scaleDownThreshold;
            set => SetProperty(ref _scaleDownThreshold, value);
        }

        /// <summary>
        /// 是否使用文本过滤模式
        /// </summary>
        public bool IsUseTextFilterMode {
            get => _isUseTextFilterMode;
            set => SetProperty(ref _isUseTextFilterMode, value);
        }

        /// <summary>
        /// 是否使用区域预检测模式
        /// </summary>
        public bool IsUseRegionPredetectionMode {
            get => _isUseRegionPredetectionMode;
            set => SetProperty(ref _isUseRegionPredetectionMode, value);
        }

        /// <summary>
        /// 灰度转换模式
        /// </summary>
        public int GrayscaleTransformationMode {
            get => _grayscaleTransformationMode;
            set => SetProperty(ref _grayscaleTransformationMode, value);
        }

        /// <summary>
        /// 图像预处理模式
        /// </summary>
        public int ImagePreprocessingMode {
            get => _imagePreprocessingMode;
            set => SetProperty(ref _imagePreprocessingMode, value);
        }

        /// <summary>
        /// 最小结果置信度
        /// </summary>
        public int MinResultConfidence {
            get => _minResultConfidence;
            set => SetProperty(ref _minResultConfidence, value);
        }

        /// <summary>
        /// 纹理检测敏感度
        /// </summary>
        public int TextureDetectionSensitivity {
            get => _textureDetectionSensitivity;
            set => SetProperty(ref _textureDetectionSensitivity, value);
        }

        /// <summary>
        /// 二值化块大小
        /// </summary>
        public int BinarizationBlockSize {
            get => _binarizationBlockSize;
            set => SetProperty(ref _binarizationBlockSize, value);
        }

        /// <summary>
        /// 识别模式
        /// </summary>
        public int RecognitionMode {
            get => _recognitionMode;
            set => SetProperty(ref _recognitionMode, value);
        }

        /// <summary>
        /// 识别跳过帧
        /// </summary>
        public int RecognitionSkipFrames {
            get => _recognitionSkipFrames;
            set => SetProperty(ref _recognitionSkipFrames, value);
        }
    }
}