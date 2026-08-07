using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.Cameras
{

    public class UsbCameraSettingsInfoModel : BindableBase
    {
        private int _exposure;
        private Size _resolution;
        private int _brightness;
        private int _contrast;
        private int _hue;
        private int _saturation;
        private int _sharpness;
        private int _gamma;
        private int _whiteBalance;
        private int _bklightComp;
        private int _gain;
        private int _zoom;
        private int _focus;
        private int _iris;
        private int _pan;
        private int _tilt;
        private int _roll;
        private bool _isCustomExposureEnabled;
        private bool _isCustomBrightnessEnabled;
        private bool _isCustomContrastEnabled;
        private bool _isCustomHueEnabled;
        private bool _isCustomSaturationEnabled;
        private bool _isCustomSharpnessEnabled;
        private bool _isCustomGammaEnabled;
        private bool _isCustomWhiteBalanceEnabled;
        private bool _isCustomBacklightCompensationEnabled;
        private bool _isCustomGainEnabled;
        private bool _isCustomZoomEnabled;
        private bool _isCustomFocusEnabled;
        private bool _isCustomApertureEnabled;
        private bool _isCustomHorizontalRotationEnabled;
        private bool _isCustomVerticalRotationEnabled;
        private bool _isCustomFlipEnabled;

        /// <summary>
        /// 曝光度
        /// </summary>
        public int Exposure
        {
            get => _exposure;
            set => SetProperty(ref _exposure, value);
        }

        /// <summary>
        /// 分辨率
        /// </summary>
        public Size Resolution
        {
            get => _resolution;
            set => SetProperty(ref _resolution, value);
        }

        /// <summary>
        /// 亮度
        /// </summary>
        public int Brightness
        {
            get => _brightness;
            set => SetProperty(ref _brightness, value);
        }

        /// <summary>
        /// 对比度
        /// </summary>
        public int Contrast
        {
            get => _contrast;
            set => SetProperty(ref _contrast, value);
        }

        /// <summary>
        /// 色调
        /// </summary>
        public int Hue
        {
            get => _hue;
            set => SetProperty(ref _hue, value);
        }

        /// <summary>
        /// 饱和度
        /// </summary>
        public int Saturation
        {
            get => _saturation;
            set => SetProperty(ref _saturation, value);
        }

        /// <summary>
        /// 锐度
        /// </summary>
        public int Sharpness
        {
            get => _sharpness;
            set => SetProperty(ref _sharpness, value);
        }

        /// <summary>
        /// 伽马值
        /// </summary>
        public int Gamma
        {
            get => _gamma;
            set => SetProperty(ref _gamma, value);
        }

        /// <summary>
        /// 白平衡
        /// </summary>
        public int WhiteBalance
        {
            get => _whiteBalance;
            set => SetProperty(ref _whiteBalance, value);
        }

        /// <summary>
        /// 背光补偿
        /// </summary>
        public int BklightComp
        {
            get => _bklightComp;
            set => SetProperty(ref _bklightComp, value);
        }

        /// <summary>
        /// 增益
        /// </summary>
        public int Gain
        {
            get => _gain;
            set => SetProperty(ref _gain, value);
        }

        /// <summary>
        /// 变焦
        /// </summary>
        public int Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        /// <summary>
        /// 对焦
        /// </summary>
        public int Focus
        {
            get => _focus;
            set => SetProperty(ref _focus, value);
        }

        /// <summary>
        /// 光圈
        /// </summary>
        public int Iris
        {
            get => _iris;
            set => SetProperty(ref _iris, value);
        }

        /// <summary>
        /// 水平旋转
        /// </summary>
        public int Pan
        {
            get => _pan;
            set => SetProperty(ref _pan, value);
        }

        /// <summary>
        /// 垂直旋转
        /// </summary>
        public int Tilt
        {
            get => _tilt;
            set => SetProperty(ref _tilt, value);
        }

        /// <summary>
        /// 翻转
        /// </summary>
        public int Roll
        {
            get => _roll;
            set => SetProperty(ref _roll, value);
        }

        /// <summary>
        /// 是否自定义曝光度
        /// </summary>
        public bool IsCustomExposureEnabled
        {
            get => _isCustomExposureEnabled;
            set => SetProperty(ref _isCustomExposureEnabled, value);
        }

        /// <summary>
        /// 是否自定义亮度
        /// </summary>
        public bool IsCustomBrightnessEnabled
        {
            get => _isCustomBrightnessEnabled;
            set => SetProperty(ref _isCustomBrightnessEnabled, value);
        }

        /// <summary>
        /// 是否自定义对比度
        /// </summary>
        public bool IsCustomContrastEnabled
        {
            get => _isCustomContrastEnabled;
            set => SetProperty(ref _isCustomContrastEnabled, value);
        }

        /// <summary>
        /// 是否自定义色调
        /// </summary>
        public bool IsCustomHueEnabled
        {
            get => _isCustomHueEnabled;
            set => SetProperty(ref _isCustomHueEnabled, value);
        }

        /// <summary>
        /// 是否自定义饱和度
        /// </summary>
        public bool IsCustomSaturationEnabled
        {
            get => _isCustomSaturationEnabled;
            set => SetProperty(ref _isCustomSaturationEnabled, value);
        }

        /// <summary>
        /// 是否自定义锐度
        /// </summary>
        public bool IsCustomSharpnessEnabled
        {
            get => _isCustomSharpnessEnabled;
            set => SetProperty(ref _isCustomSharpnessEnabled, value);
        }

        /// <summary>
        /// 是否自定义伽马值
        /// </summary>
        public bool IsCustomGammaEnabled
        {
            get => _isCustomGammaEnabled;
            set => SetProperty(ref _isCustomGammaEnabled, value);
        }

        /// <summary>
        /// 是否自定义白平衡
        /// </summary>
        public bool IsCustomWhiteBalanceEnabled
        {
            get => _isCustomWhiteBalanceEnabled;
            set => SetProperty(ref _isCustomWhiteBalanceEnabled, value);
        }

        /// <summary>
        /// 是否自定义背光补偿
        /// </summary>
        public bool IsCustomBacklightCompensationEnabled
        {
            get => _isCustomBacklightCompensationEnabled;
            set => SetProperty(ref _isCustomBacklightCompensationEnabled, value);
        }

        /// <summary>
        /// 是否自定义增益
        /// </summary>
        public bool IsCustomGainEnabled
        {
            get => _isCustomGainEnabled;
            set => SetProperty(ref _isCustomGainEnabled, value);
        }

        /// <summary>
        /// 是否自定义变焦
        /// </summary>
        public bool IsCustomZoomEnabled
        {
            get => _isCustomZoomEnabled;
            set => SetProperty(ref _isCustomZoomEnabled, value);
        }

        /// <summary>
        /// 是否自定义对焦
        /// </summary>
        public bool IsCustomFocusEnabled
        {
            get => _isCustomFocusEnabled;
            set => SetProperty(ref _isCustomFocusEnabled, value);
        }

        /// <summary>
        /// 是否自定义光圈
        /// </summary>
        public bool IsCustomApertureEnabled
        {
            get => _isCustomApertureEnabled;
            set => SetProperty(ref _isCustomApertureEnabled, value);
        }

        /// <summary>
        /// 是否自定义水平旋转
        /// </summary>

        public bool IsCustomHorizontalRotationEnabled
        {
            get => _isCustomHorizontalRotationEnabled;
            set => SetProperty(ref _isCustomHorizontalRotationEnabled, value);
        }

        /// <summary>
        /// 是否自定义垂直旋转
        /// </summary>
        public bool IsCustomVerticalRotationEnabled
        {
            get => _isCustomVerticalRotationEnabled;
            set => SetProperty(ref _isCustomVerticalRotationEnabled, value);
        }

        /// <summary>
        /// 是否自定义翻转
        /// </summary>
        public bool IsCustomFlipEnabled
        {
            get => _isCustomFlipEnabled;
            set => SetProperty(ref _isCustomFlipEnabled, value);
        }
    }
}