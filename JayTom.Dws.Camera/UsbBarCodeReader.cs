using System;
using Dynamsoft;
using System.IO;
using System.Linq;
using System.Text;
using Dynamsoft.UVC;
using Dynamsoft.DBR;
using System.Drawing;
using System.Xml.Linq;
using Newtonsoft.Json;
using Dynamsoft.Common;
using System.Management;
using System.Diagnostics;
using static System.String;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;

namespace JayTom.Dws.Camera {

    public class UsbBarCodeReader : IDisposable {
        private static string dbrLicenseKeys = "t0075oQAAAIvhAJJ+Mv2OHC+ZyzvrkkYyqMuHRgLktAwWHPtBRExDoEyZOSN3p9eHQ0csZBILJK+DKrBs2QaXyzJtmx0k+YgeciYvcCOd";
        private static string dntLicenseKeys = "t0071WQAAAIP64uktmNbWzB4BpR9uN81ZcXDga6MZQlXA+n8nb0L8q3jVDPpYvMlRHU7VP2eQUIYACdUYZhZd1ZqZ5cuIySHQErA=";
        private static ConcurrentDictionary<string, UsbCameraInfo> _cameraDictionary = new();
        public UsbCameraStatus UsbCameraStatus { get; private set; } = UsbCameraStatus.Uninitialized;
        public UsbCameraInfo UsbCameraInfo { get; private set; } = new();
        private Dynamsoft.UVC.Camera? _selectCamera;
        /*private EnumBarcodeFormat mEmBarcodeFormat = 0;
        private EnumBarcodeFormat_2 mEmBarcodeFormat_2 = 0;*/
        private BarcodeReader? mBarcodeReader;
        private PublicRuntimeSettings mCustomRuntimeSettings;
        private PublicRuntimeSettings? mNormalRuntimeSettings;
        private bool _isOpend = false;
        private SemaphoreSlim _semaphoreSlim = new(1, 1);
        private int _recognitionSkipFrames = 4;
        private bool _isLicense = false;

        public event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;

        private int _framenum = 0;

        private DateTime reTime = DateTime.Now;

        /// <summary>
        /// 相机管理
        /// </summary>
        private static CameraManager? _cameraManager;

        public UsbBarCodeReader() {
            //_twainManager = new TwainManager(dntLicenseKeys);
            _cameraManager ??= new CameraManager(dntLicenseKeys);
            if (!_isLicense) {
                EnumErrorCode ret = BarcodeReader.InitLicense(dbrLicenseKeys, out var errorMsg);
                if (ret != EnumErrorCode.DBR_SUCCESS) {
                    Console.WriteLine("InitLicense Failed:" + errorMsg);
                }
                else {
                    _isLicense = true;
                }
            }
            //mPDFRasterizer = new PDFRasterizer(dntLicenseKeys);
            //_imageCore = new ImageCore();
        }

        /// <summary>
        /// 枚举相机
        /// </summary>
        /// <returns></returns>
        public static List<UsbCameraInfo> EnumerateCameras() {
            var usbCameraInfos = new List<UsbCameraInfo>();
            try {
                var cameraManager = new CameraManager(dntLicenseKeys);

                //枚举相机
                //之后需要加一个过滤

                _cameraDictionary = new();
                var cameraNames = cameraManager?.GetCameraNames() ?? new List<string>();
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera'");
                var devices = searcher.Get();

                foreach (var device in devices) {
                    var select = cameraNames?.Select((s, i) => new {
                        Id = i,
                        Name = s
                    });
                    var orDefault = select?.FirstOrDefault(f => f.Name.Equals(device["Caption"]?.ToString()));
                    if (orDefault is not null && !string.IsNullOrEmpty(device["ClassGuid"]?.ToString())) {
                        //取出序列号ClassGuid
                        var usbCameraInfo = new UsbCameraInfo() {
                            CameraName = orDefault.Name,
                            CameraId = orDefault.Id,
                            CameraDescription = device["Description"]?.ToString(),
                            CameraManufacturer = device["Manufacturer"]?.ToString(),
                            CameraSerialNumber = device["ClassGuid"]?.ToString(),
                            CameraResolutions = cameraManager?.SelectCamera(orDefault.Name)?.SupportedResolutions?.Select(s =>
                                new Size(s.Width, s.Height))?.ToList()
                        };
                        usbCameraInfos.Add(usbCameraInfo);
                        _cameraDictionary.AddOrUpdate(device["ClassGuid"].ToString() ?? string.Empty, value => usbCameraInfo,
                            (key, oldValue) => usbCameraInfo);
                    }
                }

                cameraManager?.Dispose();
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return usbCameraInfos;
        }

        /// <summary>
        /// 设置Usb相机参数
        /// </summary>
        /// <param name="parameters"></param>
        public async Task<KeyValuePair<bool, string>> SetUsbCameraParameter(Dictionary<UsbCameraParameter, object> parameters) {
            await Task.Yield();
            var (key, value) = _cameraDictionary.FirstOrDefault(f => f.Key.Equals(UsbCameraInfo.CameraSerialNumber));
            if (!string.IsNullOrEmpty(key)) {
                try {
                    if (_selectCamera is not null) {
                        //设置参数
                        //设置分辨率(如果没有指定则使用最大分辨率)
                        /*var resolution = parameters.FirstOrDefault(f =>
                                 f.Key == UsbCameraParameter.Resolution)
                             .Value;
                        if (resolution is Size size) {
                            _selectCamera.CurrentResolution = new CamResolution(size.Width, size.Height);
                        }
                        else {
                            var orDefault = UsbCameraInfo.CameraResolutions?.OrderByDescending(o => o.Width * o.Height)?.FirstOrDefault();
                            if (orDefault is not null) {
                                _selectCamera.CurrentResolution = new CamResolution(orDefault.Value.Width, orDefault.Value.Height);
                            }
                        }*/
                        var exposure = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Exposure)
                            .Value;
                        if (exposure is int i) {
                            _selectCamera.Exposure.IfAuto = false;
                            _selectCamera.Exposure.Value = i;
                            //设置曝光度
                        }
                        else {
                            _selectCamera.Exposure.IfAuto = true;
                        }
                        //亮度
                        var brightness = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Brightness)
                            .Value;
                        if (brightness is int brightness1) {
                            _selectCamera.Exposure.IfAuto = false;
                            _selectCamera.Brightness.Value = brightness1;
                        }
                        else {
                            _selectCamera.Exposure.IfAuto = true;
                        }

                        //对比度
                        var contrast = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Contrast)
                            .Value;
                        if (contrast is int contrast1) {
                            // _selectCamera.Contrast.IfAuto = false;
                            _selectCamera.Contrast.Value = contrast1;
                        }

                        //色调
                        var hue = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Hue)
                            .Value;
                        if (hue is int hue1) {
                            //_selectCamera.Hue.IfAuto = false;
                            _selectCamera.Hue.Value = hue1;
                        }

                        //饱和度
                        var saturation = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Saturation)
                            .Value;
                        if (saturation is int saturation1) {
                            //_selectCamera.Saturation.IfAuto = false;
                            _selectCamera.Saturation.Value = saturation1;
                        }

                        //锐度
                        var sharpness = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Sharpness)
                            .Value;
                        if (sharpness is int sharpness1) {
                            //_selectCamera.Sharpness.IfAuto = false;
                            _selectCamera.Sharpness.Value = sharpness1;
                        }
                        /*else {
                            _selectCamera.Sharpness.IfAuto = true;
                        }*/
                        //伽马值
                        var gamma = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Gamma)
                            .Value;
                        if (gamma is int gamma1) {
                            //_selectCamera.Gamma.IfAuto = false;
                            //_selectCamera.Gamma.Value = gamma1;
                        }
                        /*else {
                            _selectCamera.Gamma.IfAuto = true;
                        }*/
                        //白平衡
                        var whiteBalance = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.WhiteBalance)
                            .Value;
                        if (whiteBalance is int balance) {
                            //_selectCamera.WhiteBalance.IfAuto = false;
                            _selectCamera.WhiteBalance.Value = balance;
                        }
                        /*else {
                            _selectCamera.WhiteBalance.IfAuto = true;
                        }*/
                        //背光补偿
                        var bklightComp = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.BklightComp)
                            .Value;
                        if (bklightComp is int comp) {
                            //_selectCamera.BklightComp.IfAuto = false;
                            _selectCamera.BklightComp.Value = comp;
                        }
                        /*else {
                            _selectCamera.BklightComp.IfAuto = true;
                        }*/
                        //增益
                        var gain = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Gain)
                            .Value;
                        if (gain is int gain1) {
                            //_selectCamera.Gain.IfAuto = false;
                            _selectCamera.Gain.Value = gain1;
                        }
                        /*else {
                            _selectCamera.Gain.IfAuto = true;
                        }*/
                        return new KeyValuePair<bool, string>(true, string.Empty);
                        //变焦
                        var zoom = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Zoom)
                            .Value;
                        if (zoom is int zoom1) {
                            _selectCamera.Zoom.IfAuto = false;
                            _selectCamera.Zoom.Value = zoom1;
                        }
                        else {
                            _selectCamera.Zoom.IfAuto = true;
                        }
                        //对焦
                        var focus = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Focus)
                            .Value;
                        if (focus is int focus1) {
                            _selectCamera.Focus.IfAuto = false;
                            _selectCamera.Focus.Value = focus1;
                        }
                        else {
                            _selectCamera.Focus.IfAuto = true;
                        }
                        //光圈
                        var iris = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Iris)
                            .Value;
                        if (iris is int iris1) {
                            _selectCamera.Iris.IfAuto = false;
                            _selectCamera.Iris.Value = iris1;
                        }
                        else {
                            _selectCamera.Iris.IfAuto = true;
                        }
                        //水平旋转
                        var pan = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Pan)
                            .Value;
                        if (pan is int pan1) {
                            _selectCamera.Pan.IfAuto = false;
                            _selectCamera.Pan.Value = pan1;
                        }
                        else {
                            _selectCamera.Pan.IfAuto = true;
                        }
                        //垂直旋转
                        var tilt = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Tilt)
                            .Value;
                        if (tilt is int tilt1) {
                            _selectCamera.Tilt.IfAuto = false;
                            _selectCamera.Tilt.Value = tilt1;
                        }
                        else {
                            _selectCamera.Tilt.IfAuto = true;
                        }
                        //翻转
                        var roll = parameters.FirstOrDefault(f =>
                                f.Key == UsbCameraParameter.Roll)
                            .Value;
                        if (roll is int roll1) {
                            _selectCamera.Roll.IfAuto = false;
                            _selectCamera.Roll.Value = roll1;
                        }
                        else {
                            _selectCamera.Roll.IfAuto = true;
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "相机未绑定");
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "相机未绑定");
            }
            return new KeyValuePair<bool, string>(false, "未知错误");
        }

        /// <summary>
        /// 设置读码参数
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> SetBarcodeReaderParameter(
            Dictionary<BarcodeReaderParameter, object> parameters) {
            await Task.Yield();
            if (mBarcodeReader is not null) {
                if (/*!_isOpend*/ true) {
                    mBarcodeReader.ResetRuntimeSettings();
                    var runtimeSettings = mBarcodeReader.GetRuntimeSettings();

                    //条码类型
                    var enumBarcodeFormat = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.EnumBarcodeFormat)
                        .Value;
                    if (enumBarcodeFormat is EnumBarcodeFormat format) {
                        runtimeSettings.BarcodeFormatIds = (int)format;
                    }

                    var enumBarcodeFormat2 = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.EnumBarcodeFormat2)
                        .Value;
                    if (enumBarcodeFormat2 is EnumBarcodeFormat_2 format2) {
                        runtimeSettings.BarcodeFormatIds = (int)format2;
                    }

                    var recognitionMode = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.RecognitionMode)
                        .Value ?? ScanMode.Speed;
                    if (recognitionMode is ScanMode scanMode) {
                        switch (scanMode) {
                            case ScanMode.Speed: {
                                    //runtimeSettings.BarcodeFormatIds = (int)(EnumBarcodeFormat.BF_CODE_128 | EnumBarcodeFormat.BF_CODE_39 | EnumBarcodeFormat.BF_QR_CODE);
                                    runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                    for (var i = 1; i < runtimeSettings.LocalizationModes.Length; i++)
                                        runtimeSettings.LocalizationModes[i] = EnumLocalizationMode.LM_SKIP;
                                    runtimeSettings.DeblurLevel = 3;
                                    runtimeSettings.ExpectedBarcodesCount = 0;
                                    runtimeSettings.ScaleDownThreshold = 2300;
                                    for (var i = 0; i < runtimeSettings.FurtherModes.TextFilterModes.Length; i++)
                                        runtimeSettings.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
                                    runtimeSettings.FurtherModes.TextFilterModes[0] = EnumTextFilterMode.TFM_GENERAL_CONTOUR;
                                    mBarcodeReader.UpdateRuntimeSettings(runtimeSettings);
                                    break;
                                }
                            case ScanMode.Balance: {
                                    runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_CONNECTED_BLOCKS;
                                    runtimeSettings.LocalizationModes[1] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                    for (var i = 2; i < runtimeSettings.LocalizationModes.Length; i++)
                                        runtimeSettings.LocalizationModes[i] = EnumLocalizationMode.LM_SKIP;
                                    runtimeSettings.DeblurLevel = 5;
                                    runtimeSettings.ExpectedBarcodesCount = 512;
                                    runtimeSettings.ScaleDownThreshold = 2300;
                                    runtimeSettings.FurtherModes.TextFilterModes[0] = EnumTextFilterMode.TFM_GENERAL_CONTOUR;
                                    for (var i = 1; i < runtimeSettings.FurtherModes.TextFilterModes.Length; i++)
                                        runtimeSettings.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
                                    mBarcodeReader.UpdateRuntimeSettings(runtimeSettings);
                                    break;
                                }
                            case ScanMode.Coverage: {
                                    runtimeSettings.DeblurLevel = 9;
                                    runtimeSettings.ExpectedBarcodesCount = 512;
                                    runtimeSettings.ScaleDownThreshold = 214748347;
                                    runtimeSettings.FurtherModes.TextFilterModes[0] = EnumTextFilterMode.TFM_GENERAL_CONTOUR;
                                    for (var i = 1; i < runtimeSettings.FurtherModes.TextFilterModes.Length; i++)
                                        runtimeSettings.FurtherModes.TextFilterModes[i] = EnumTextFilterMode.TFM_SKIP;
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                    runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_INVERTED;
                                    for (var i = 2; i < runtimeSettings.FurtherModes.GrayscaleTransformationModes.Length; i++)
                                        runtimeSettings.FurtherModes.GrayscaleTransformationModes[i] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                    mBarcodeReader.UpdateRuntimeSettings(runtimeSettings);
                                    break;
                                }
                            case ScanMode.Custom: {
                                    var expectedBarcodesCount = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.ExpectedBarcodesCount)
                                        .Value;
                                    if (expectedBarcodesCount is int count) {
                                        runtimeSettings.ExpectedBarcodesCount = count;
                                    }

                                    var deblurLevel = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.DeblurLevel)
                                        .Value;
                                    if (deblurLevel is int level) {
                                        runtimeSettings.DeblurLevel = level;
                                    }
                                    for (var i = 0; i < runtimeSettings.LocalizationModes.Length; i++)
                                        runtimeSettings.LocalizationModes[i] = EnumLocalizationMode.LM_SKIP;

                                    var localizationMode = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.LocalizationMode)
                                        .Value ?? LocalizationMode.Default;
                                    if (localizationMode is LocalizationMode mode) {
                                        switch (mode) {
                                            case LocalizationMode.Default:
                                                runtimeSettings.LocalizationModes = mNormalRuntimeSettings?.LocalizationModes;
                                                break;

                                            case LocalizationMode.ConnectedBlocks:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_CONNECTED_BLOCKS;
                                                break;

                                            case LocalizationMode.Statistics:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_STATISTICS;
                                                break;

                                            case LocalizationMode.Lines:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_LINES;
                                                break;

                                            case LocalizationMode.ScanDirectly:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                                break;

                                            case LocalizationMode.ConnectedBlocksAndScanDirectly:
                                                runtimeSettings.LocalizationModes[0] = EnumLocalizationMode.LM_CONNECTED_BLOCKS;
                                                runtimeSettings.LocalizationModes[1] = EnumLocalizationMode.LM_SCAN_DIRECTLY;
                                                break;
                                        }
                                    }

                                    var isUseTextFilterMode = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.IsUseTextFilterMode)
                                        .Value;
                                    if (isUseTextFilterMode is bool filterMode) {
                                        runtimeSettings.FurtherModes.TextFilterModes[0] = filterMode ? EnumTextFilterMode.TFM_GENERAL_CONTOUR : EnumTextFilterMode.TFM_SKIP;
                                    }

                                    var isUseRegionPredetectionMode = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.IsUseRegionPredetectionMode)
                                        .Value;
                                    if (isUseRegionPredetectionMode is bool predetectionMode) {
                                        runtimeSettings.FurtherModes.RegionPredetectionModes[0] = predetectionMode ? EnumRegionPredetectionMode.RPM_GENERAL_RGB_CONTRAST : EnumRegionPredetectionMode.RPM_SKIP;
                                    }

                                    var scaleDownThreshold = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.ScaleDownThreshold)
                                        .Value;
                                    if (scaleDownThreshold is int threshold) {
                                        runtimeSettings.ScaleDownThreshold = threshold < 512 ? 512 : threshold;
                                    }

                                    var grayscaleTransformationMode = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.GrayscaleTransformationMode)
                                        .Value ?? GrayscaleTransformationMode.Original;
                                    if (grayscaleTransformationMode is GrayscaleTransformationMode formationMode) {
                                        switch (formationMode) {
                                            case GrayscaleTransformationMode.Original:
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_INVERTED;
                                                break;

                                            case GrayscaleTransformationMode.Inverted:
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_INVERTED;
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                                break;

                                            case GrayscaleTransformationMode.OriginalAndInverted:
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[0] = EnumGrayscaleTransformationMode.GTM_ORIGINAL;
                                                runtimeSettings.FurtherModes.GrayscaleTransformationModes[1] = EnumGrayscaleTransformationMode.GTM_SKIP;
                                                break;
                                        }
                                    }

                                    var imagePreprocessingMode = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.ImagePreprocessingMode)
                                        .Value ?? ImagePreprocessingMode.General;
                                    if (imagePreprocessingMode is ImagePreprocessingMode preprocessingMode) {
                                        runtimeSettings.FurtherModes.ImagePreprocessingModes[0] = preprocessingMode switch {
                                            ImagePreprocessingMode.General => EnumImagePreprocessingMode.IPM_GENERAL,
                                            ImagePreprocessingMode.GrayEqualization => EnumImagePreprocessingMode.IPM_GRAY_EQUALIZE,
                                            ImagePreprocessingMode.GraySmoothing => EnumImagePreprocessingMode.IPM_GRAY_SMOOTH,
                                            ImagePreprocessingMode.SharpeningAndSmoothing => EnumImagePreprocessingMode
                                                .IPM_SHARPEN_SMOOTH,
                                            _ => runtimeSettings.FurtherModes.ImagePreprocessingModes[0]
                                        };
                                    }

                                    var minResultConfidence = parameters.FirstOrDefault(f =>
                                            f.Key == BarcodeReaderParameter.MinResultConfidence)
                                        .Value;
                                    if (minResultConfidence is int confidence) {
                                        runtimeSettings.MinResultConfidence = confidence * 10;
                                    }

                                    break;
                                }
                        }
                        var textureDetectionSensitivity = parameters.FirstOrDefault(f =>
                                f.Key == BarcodeReaderParameter.TextureDetectionSensitivity)
                            .Value;
                        if (textureDetectionSensitivity is int sensitivity) {
                            runtimeSettings.FurtherModes.TextureDetectionModes[0] = sensitivity == 0 ? EnumTextureDetectionMode.TDM_SKIP : EnumTextureDetectionMode.TDM_GENERAL_WIDTH_CONCENTRATION;
                            if (sensitivity > 0) {
                                mBarcodeReader.SetModeArgument("TextureDetectionModes", 0, "Sensitivity", sensitivity.ToString(), out var strErrorMessage);
                            }
                        }

                        var binarizationBlockSize = parameters.FirstOrDefault(f =>
                                f.Key == BarcodeReaderParameter.BinarizationBlockSize)
                            .Value;
                        if (binarizationBlockSize is int) {
                            mBarcodeReader.SetModeArgument("BinarizationModes", 0, "BlockSizeX", binarizationBlockSize.ToString(), out var strErrorMessage);
                        }
                    }
                    mBarcodeReader.UpdateRuntimeSettings(runtimeSettings);
                    return new KeyValuePair<bool, string>(false, "读码器设置成功");
                }
                else {
                    return new KeyValuePair<bool, string>(false, "运行中不能设置");
                }
            }
            else {
                return new KeyValuePair<bool, string>(false, "相机未绑定");
            }
        }

        /// <summary>
        /// 绑定
        /// </summary>
        /// <param name="info"></param>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> BindCamera(UsbCameraInfo info) {
            await Task.Delay(2000);
            var (key, value) = _cameraDictionary.FirstOrDefault(f => f.Key.Equals(info.CameraSerialNumber));
            if (!string.IsNullOrEmpty(key)) {
                UsbCameraInfo = value;
                _selectCamera = _cameraManager?.SelectCamera(value.CameraName);
                var orDefault = value.CameraResolutions?.OrderByDescending(o => o.Width * o.Height)?.FirstOrDefault();
                if (orDefault is not null && _selectCamera is not null) {
                    _selectCamera.CurrentResolution = new CamResolution(orDefault.Value.Width, orDefault.Value.Height);
                }

                mBarcodeReader = BarcodeReader.GetInstance();
                mNormalRuntimeSettings = mBarcodeReader?.GetRuntimeSettings();
                await SetBarcodeReaderParameter(new Dictionary<BarcodeReaderParameter, object>()
                 {
                    { BarcodeReaderParameter.RecognitionMode, ScanMode.Speed },
                    {BarcodeReaderParameter.IsUseTextFilterMode,true},
                    //{BarcodeReaderParameter.IsUseRegionPredetectionMode,true}
                });
                return true;
            }

            return false;
            // 实现绑定相机的逻辑
            // 根据相机序列号绑定相机设备
            // _selectCamera = _cameraManager?.SelectCamera(value.CameraName);
        }

        /// <summary>
        /// 开始
        /// </summary>
        public async Task<KeyValuePair<bool, string>> Start() {
            await Task.Delay(1000);
            try {
                if (_selectCamera is not null && !_isOpend) {
                    _selectCamera.Open();
                    //注册事件
                    _selectCamera.OnFrameCaptrue += SelectCameraOnOnFrameCaptrue;
                    _isOpend = true;
                    return new KeyValuePair<bool, string>(true, "启动成功");
                }
                else {
                    return new KeyValuePair<bool, string>(false, "相机未绑定");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
        }

        /// <summary>
        /// 捕获图片事件
        /// </summary>
        /// <param name="bitmap"></param>
        /// <exception cref="NotImplementedException"></exception>
        private async void SelectCameraOnOnFrameCaptrue(Bitmap bitmap) {
            //读码
            var fastClone = FastClone(bitmap);
            await Task.Yield();
            /*var tempBitmap = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat);

            tempBitmap.Dispose();*/
            /*Debug.WriteLine($"纯图片返回间隔{DateTime.Now.Subtract(reTime).TotalMilliseconds}");
            reTime = DateTime.Now;*/
            Task.Factory.StartNew(() => {
                ReadFromFrame(fastClone);
            });
        }

        /// <summary>
        /// 读码
        /// </summary>
        /// <param name="bitmap"></param>
        private async void ReadFromFrame(Bitmap bitmap) {
            if (_framenum >= _recognitionSkipFrames) {
                _framenum = 0;
                long elapsedMilliseconds = 0;
                TextResult[]? bars = null;
                var (buffer, stride, pixelFormat) = GetBitmapData(bitmap);
                try {
                    await _semaphoreSlim.WaitAsync();
                    if (mBarcodeReader is not null) {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        bars = mBarcodeReader?.DecodeBuffer(buffer, bitmap.Width, bitmap.Height, stride, pixelFormat,
                            "");
                        stopwatch.Stop();
                        elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
                finally {
                    _semaphoreSlim.Release();
                }

                //解析条码
                var barcodeScannedEventArgs = new BarcodeScannedEventArgs() {
                    ScanTime = DateTime.Now,
                    CameraSerialNumber = this.UsbCameraInfo.CameraSerialNumber,
                    Image = bitmap
                };
                if (bars is not null && bars.Length > 0) {
                    //识别到条码
                    barcodeScannedEventArgs.BarCodes = bars.Select(s => new BarcodeInfo {
                        Barcode = s.BarcodeText,
                        BarcodeRegion = s.LocalizationResult.ResultPoints?.ToList(),
                        BarcodeType = s.LocalizationResult.BarcodeFormatString,
                    })?.ToList();
                    barcodeScannedEventArgs.Image = bitmap;
                    barcodeScannedEventArgs.RecognitionTime = elapsedMilliseconds;
                    OnBarcodeScanned(barcodeScannedEventArgs);
                }
                else {
                    OnBarcodeScanned(barcodeScannedEventArgs);
                }
            }
            else {
                OnBarcodeScanned(new BarcodeScannedEventArgs() {
                    ScanTime = DateTime.Now,
                    CameraSerialNumber = this.UsbCameraInfo.CameraSerialNumber,
                    Image = bitmap
                });
            }

            _framenum++;
        }

        /// <summary>
        /// 停止
        /// </summary>
        public async Task<KeyValuePair<bool, string>> Stop() {
            //解绑事件
            await Task.Yield();
            try {
                if (_selectCamera is not null && _isOpend) {
                    //注册事件
                    _selectCamera.OnFrameCaptrue -= SelectCameraOnOnFrameCaptrue;
                    await Task.Delay(500);
                    _selectCamera.Close();
                    _isOpend = false;
                    return new KeyValuePair<bool, string>(true, "停止成功");
                }
                else {
                    return new KeyValuePair<bool, string>(false, "相机未绑定");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
        }

        //绑定相机
        //开始
        //停止

        /// <summary>
        /// 释放资源
        /// </summary>
        public async void Dispose() {
            await Stop();
            mBarcodeReader?.Recycle();
        }

        protected virtual async void OnBarcodeScanned(BarcodeScannedEventArgs e) {
            await Task.Yield();
            BarcodeScanned?.Invoke(this, e);
        }

        private Region? CreateRegionFromPoints(Point[] points) {
            if (points.Length < 3) {
                return null;
            }

            var path = new GraphicsPath();
            path.AddPolygon(points);

            var region = new Region(path);
            return region;
        }

        private static byte[] GetImageData(Bitmap bitmap) {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

            int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpData.PixelFormat) / 8;
            int bufferSize = bmpData.Stride * bitmap.Height;
            byte[] buffer = new byte[bufferSize];
            Marshal.Copy(bmpData.Scan0, buffer, 0, bufferSize);

            bitmap.UnlockBits(bmpData);

            return buffer;
        }

        // 获取位图的步长（stride）
        private static int GetStride(Bitmap bitmap) {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int stride = bmpData.Stride;
            bitmap.UnlockBits(bmpData);

            return stride;
        }

        // 获取位图的像素格式
        private static EnumImagePixelFormat GetPixelFormat(Bitmap bitmap) {
            switch (bitmap.PixelFormat) {
                case PixelFormat.Format24bppRgb:
                    return EnumImagePixelFormat.IPF_RGB_888;

                case PixelFormat.Format32bppArgb:
                    return EnumImagePixelFormat.IPF_ARGB_8888;
                // 根据你的实际情况添加其他像素格式的处理
                default:
                    throw new NotSupportedException("Unsupported pixel format");
            }
        }

        public static (byte[] buffer, int stride, EnumImagePixelFormat pixelFormat) GetBitmapData(Bitmap bitmap) {
            // 锁定Bitmap对象的内存区域，并获取其指针
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            IntPtr ptr = bitmapData.Scan0;

            // 计算每行像素需要的字节数
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int stride = bitmapData.Stride;

            // 创建缓冲区，并将Bitmap对象的数据复制到缓冲区
            int bufferSize = bitmapData.Height * Math.Abs(bitmapData.Stride);
            byte[] buffer = new byte[bufferSize];
            Marshal.Copy(ptr, buffer, 0, bufferSize);

            // 解锁Bitmap对象的内存区域
            bitmap.UnlockBits(bitmapData);

            // 获取像素格式
            EnumImagePixelFormat pixelFormat = GetImagePixelFormat(bitmap.PixelFormat);

            return (buffer, stride, pixelFormat);
        }

        private static EnumImagePixelFormat GetImagePixelFormat(PixelFormat pixelFormat) {
            switch (pixelFormat) {
                case PixelFormat.Format24bppRgb:
                    return EnumImagePixelFormat.IPF_RGB_888;

                case PixelFormat.Format32bppArgb:
                    return EnumImagePixelFormat.IPF_ARGB_8888;
                // 添加其他支持的像素格式

                default:
                    throw new NotSupportedException("Unsupported pixel format.");
            }
        }

        public static Bitmap FastClone(Bitmap sourceBitmap) {
            // 创建新的Bitmap对象
            Bitmap destBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height, sourceBitmap.PixelFormat);

            // 锁定源Bitmap对象的内存区域，并获取其指针
            BitmapData sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                ImageLockMode.ReadOnly, sourceBitmap.PixelFormat);
            IntPtr sourcePtr = sourceData.Scan0;

            // 锁定目标Bitmap对象的内存区域，并获取其指针
            BitmapData destData = destBitmap.LockBits(new Rectangle(0, 0, destBitmap.Width, destBitmap.Height),
                ImageLockMode.WriteOnly, destBitmap.PixelFormat);
            IntPtr destPtr = destData.Scan0;

            // 计算每行像素需要的字节数
            int bytesPerPixel = Image.GetPixelFormatSize(sourceBitmap.PixelFormat) / 8;
            int stride = sourceData.Stride;

            // 使用Marshal.Copy方法将源Bitmap对象的数据复制到目标Bitmap对象
            unsafe {
                byte* source = (byte*)sourcePtr.ToPointer();
                byte* dest = (byte*)destPtr.ToPointer();
                for (int y = 0; y < sourceBitmap.Height; y++) {
                    // 计算当前行的起始位置
                    byte* rowSource = source + (y * stride);
                    byte* rowDest = dest + (y * destData.Stride);

                    // 将当前行的像素数据复制到目标Bitmap对象
                    Buffer.MemoryCopy(rowSource, rowDest, destData.Stride, stride);
                }
            }

            // 解锁源Bitmap对象和目标Bitmap对象的内存区域
            sourceBitmap.UnlockBits(sourceData);
            destBitmap.UnlockBits(destData);

            return destBitmap;
        }

        public static unsafe Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            if (sourceImage is null) {
                return null;
            }

            var sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try {
                var thumbnail = new Bitmap(thumbnailWidth, thumbnailHeight);
                var thumbnailData = thumbnail.LockBits(new Rectangle(0, 0, thumbnailWidth, thumbnailHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                try {
                    byte* sourcePtr = (byte*)sourceData.Scan0;
                    byte* thumbnailPtr = (byte*)thumbnailData.Scan0;

                    var sourceBytesPerPixel = 4;
                    var thumbnailBytesPerPixel = 4;

                    var scaleX = (float)thumbnailWidth / sourceImage.Width;
                    var scaleY = (float)thumbnailHeight / sourceImage.Height;

                    var sourceWidth = sourceImage.Width;
                    var sourceHeight = sourceImage.Height;

                    for (int y = 0; y < thumbnailHeight; y++) {
                        for (int x = 0; x < thumbnailWidth; x++) {
                            var sourceX = (int)(x / scaleX);
                            var sourceY = (int)(y / scaleY);

                            var sourceIndex = (sourceY * sourceWidth + sourceX) * sourceBytesPerPixel;
                            var thumbnailIndex = (y * thumbnailWidth + x) * thumbnailBytesPerPixel;

                            thumbnailPtr[thumbnailIndex] = sourcePtr[sourceIndex];
                            thumbnailPtr[thumbnailIndex + 1] = sourcePtr[sourceIndex + 1];
                            thumbnailPtr[thumbnailIndex + 2] = sourcePtr[sourceIndex + 2];
                            thumbnailPtr[thumbnailIndex + 3] = sourcePtr[sourceIndex + 3];
                        }
                    }
                }
                finally {
                    thumbnail.UnlockBits(thumbnailData);
                }

                return thumbnail;
            }
            finally {
                sourceImage.UnlockBits(sourceData);
            }
        }
    }
}

public class UsbCameraInfo {

    /// <summary>
    /// 相机名称
    /// </summary>
    public string? CameraName { get; set; }

    /// <summary>
    /// 相机Id
    /// </summary>
    public int? CameraId { get; set; }

    /// <summary>
    /// 相机序列号
    /// </summary>
    public string? CameraSerialNumber { get; set; }

    /// <summary>
    /// 相机制造商
    /// </summary>
    public string? CameraManufacturer { get; set; }

    /// <summary>
    /// 相机版本
    /// </summary>
    public string? CameraVersion { get; set; }

    /// <summary>
    /// 相机型号
    /// </summary>
    public string? CameraModel { get; set; }

    /// <summary>
    /// 相机描述
    /// </summary>
    public string? CameraDescription { get; set; }

    /// <summary>
    /// 支持的分辨率
    /// </summary>
    public List<Size>? CameraResolutions { get; set; }
}

public class BarcodeScannedEventArgs : EventArgs {

    /// <summary>
    /// 图片
    /// </summary>
    public Bitmap? Image { get; set; }

    /// <summary>
    /// 条码集合
    /// </summary>
    public List<BarcodeInfo>? BarCodes { get; set; }

    /// <summary>
    /// 扫码时间
    /// </summary>
    public DateTime ScanTime { get; set; }

    /// <summary>
    /// 相机序列号
    /// </summary>
    public string? CameraSerialNumber { get; set; }

    /// <summary>
    /// 识别耗时
    /// </summary>
    public long RecognitionTime { get; set; }
}

public class BarcodeInfo {

    /// <summary>
    /// 条码
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 条码区域
    /// </summary>
    public List<Point>? BarcodeRegion { get; set; }

    /// <summary>
    /// 条码类型
    /// </summary>
    public string? BarcodeType { get; set; }
}

//增益
/*tempCamera.Exposure.IfAuto = false;
tempCamera.Exposure.Value = exposure;*/

//Exposure.IfAuto->是否自动增益
//Exposure.Value->增益值

//EnumBarcodeFormat->条码类型
//EnumBarcodeFormat_2->未知类型

//--------算法参数----------

// LocalizationModes->本地化模式()
// DeblurLevel->去模糊级别(0-9)
//ExpectedBarcodesCount->期望的条形码数量
//ScaleDownThreshold->缩放阈值
//TextFilterMode->是否使用文本过滤模式
//RegionPredetectionMode->是否使用区域预检测模式
//GrayscaleTransformationModes->灰度转换模式
//MinResultConfidence->最小结果置信度(0-9,乘以10)
//TextureDetectionSensitivity->纹理检测敏感度(0-9)
//BinarizationBlockSize->二值化块大小(0-999)
//RecognitionMode->识别模式
public enum UsbCameraStatus {

    /// <summary>
    /// 未初始化
    /// </summary>
    Uninitialized,

    /// <summary>
    /// 已开始
    /// </summary>
    Started,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,

    /// <summary>
    /// 异常
    /// </summary>
    Error
}

/// <summary>
/// 本地化模式
/// </summary>
public enum LocalizationMode {

    /// <summary>
    /// 默认
    /// </summary>
    Default = 0,

    /// <summary>
    /// 连通块
    /// </summary>
    ConnectedBlocks = 1,

    /// <summary>
    /// 统计
    /// </summary>
    Statistics = 2,

    /// <summary>
    /// 线条
    /// </summary>
    Lines = 3,

    /// <summary>
    /// 直接扫描
    /// </summary>
    ScanDirectly = 4,

    /// <summary>
    /// 连通块 + 直接扫描
    /// </summary>
    ConnectedBlocksAndScanDirectly = 5
}

/// <summary>
/// 灰度转换模式
/// </summary>
public enum GrayscaleTransformationMode {

    /// <summary>
    /// 原图
    /// </summary>
    Original = 0,

    /// <summary>
    /// 反色
    /// </summary>
    Inverted = 1,

    /// <summary>
    /// 原图+反色
    /// </summary>
    OriginalAndInverted = 2
}

/// <summary>
/// 图像预处理模式
/// </summary>
public enum ImagePreprocessingMode {

    /// <summary>
    /// 通用
    /// </summary>
    General = 0,

    /// <summary>
    /// 灰度均衡化
    /// </summary>
    GrayEqualization = 1,

    /// <summary>
    /// 灰度平滑
    /// </summary>
    GraySmoothing = 2,

    /// <summary>
    /// 锐化和平滑
    /// </summary>
    SharpeningAndSmoothing = 3
}

/// <summary>
/// 扫码模式
/// </summary>
public enum ScanMode {

    /// <summary>
    /// 速度
    /// </summary>
    Speed,

    /// <summary>
    /// 平衡
    /// </summary>
    Balance,

    /// <summary>
    /// 覆盖
    /// </summary>
    Coverage,

    /// <summary>
    /// 自定义
    /// </summary>

    Custom
}

/// <summary>
/// 读码参数
/// </summary>
public enum BarcodeReaderParameter {

    /// <summary>
    /// 条码类型
    /// </summary>
    EnumBarcodeFormat,

    /// <summary>
    /// 条码类型2
    /// </summary>
    EnumBarcodeFormat2,

    /// <summary>
    /// 本地化模式
    /// </summary>
    LocalizationMode,

    /// <summary>
    /// 去模糊级别(0-9)
    /// </summary>
    DeblurLevel,

    /// <summary>
    /// 期望的条形码数量
    /// </summary>
    ExpectedBarcodesCount,

    /// <summary>
    /// 缩放阈值
    /// </summary>
    ScaleDownThreshold,

    /// <summary>
    /// 是否使用文本过滤模式
    /// </summary>
    IsUseTextFilterMode,

    /// <summary>
    /// 是否使用区域预检测模式
    /// </summary>
    IsUseRegionPredetectionMode,

    /// <summary>
    /// 灰度转换模式
    /// </summary>
    GrayscaleTransformationMode,

    /// <summary>
    /// 图像预处理模式
    /// </summary>
    ImagePreprocessingMode,

    /// <summary>
    /// 最小结果置信度(0-9,乘以10)
    /// </summary>
    MinResultConfidence,

    /// <summary>
    /// 纹理检测敏感度(0-9)
    /// </summary>
    TextureDetectionSensitivity,

    /// <summary>
    /// 二值化块大小(0-999)
    /// </summary>
    BinarizationBlockSize,

    /// <summary>
    /// 识别模式
    /// </summary>
    RecognitionMode,

    /// <summary>
    /// 跳过的帧率
    /// </summary>
    RecognitionSkipFrames,
}

/// <summary>
/// Usb相机参数
/// </summary>
public enum UsbCameraParameter {

    /// <summary>
    /// 曝光度
    /// </summary>
    Exposure,

    /// <summary>
    /// 分辨率
    /// </summary>
    Resolution,

    /// <summary>
    /// 亮度
    /// </summary>
    Brightness,

    /// <summary>
    /// 对比度
    /// </summary>
    Contrast,

    /// <summary>
    /// 色调
    /// </summary>
    Hue,

    /// <summary>
    /// 饱和度
    /// </summary>
    Saturation,

    /// <summary>
    /// 锐度
    /// </summary>
    Sharpness,

    /// <summary>
    /// 伽马值
    /// </summary>
    Gamma,

    /// <summary>
    /// 白平衡
    /// </summary>
    WhiteBalance,

    /// <summary>
    /// 背光补偿
    /// </summary>
    BklightComp,

    /// <summary>
    /// 增益
    /// </summary>
    Gain,

    /// <summary>
    /// 变焦
    /// </summary>
    Zoom,

    /// <summary>
    /// 对焦
    /// </summary>
    Focus,

    /// <summary>
    /// 光圈
    /// </summary>
    Iris,

    /// <summary>
    /// 水平旋转
    /// </summary>
    Pan,

    /// <summary>
    /// 垂直旋转
    /// </summary>
    Tilt,

    /// <summary>
    /// 翻转
    /// </summary>
    Roll
}