using System;
using System.Buffers;
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
using JayTom.Dws.Camera.BarCodeReader;

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
        private int _recognitionSkipFrames = 4;
        private bool _isLicense = false;

        private CancellationTokenSource _stopCancellationTokenSource = new();
        private Task? _frameProcessingTask;
        /// <summary>通知常驻读码线程已有新帧。</summary>
        private readonly SemaphoreSlim _frameSignal = new(0, 1);
        /// <summary>只保留等待处理的最新一帧。</summary>
        private Bitmap? _pendingFrame;

        //图片缩放百分比
        private int _scalePercentage = 0;

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
                        var selectCamera = cameraManager?.SelectCamera(orDefault.Name);
                        var usbCameraInfo = new UsbCameraInfo() {
                            CameraName = orDefault.Name,
                            CameraId = orDefault.Id,
                            CameraDescription = device["Description"]?.ToString(),
                            CameraManufacturer = device["Manufacturer"]?.ToString(),
                            CameraSerialNumber = device["ClassGuid"]?.ToString(),
                            CameraResolutions = selectCamera?.SupportedResolutions?.Select(s =>
                                new Size(s.Width, s.Height))?.ToList()
                        };
                        usbCameraInfos.Add(usbCameraInfo);
                        selectCamera?.Close();
                        selectCamera?.Dispose();
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
                        var resolution = parameters.FirstOrDefault(f =>
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
                        }
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
                        return new KeyValuePair<bool, string>(true, string.Empty);
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

                    var recognitionSkipFrames = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.RecognitionSkipFrames)
                        .Value;
                    if (recognitionSkipFrames is int skipFrames) {
                        _recognitionSkipFrames = skipFrames;
                    }

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

                    var scalePercentage = parameters.FirstOrDefault(f =>
                            f.Key == BarcodeReaderParameter.ScalePercentage)
                        .Value;
                    if (scalePercentage is int percentage) {
                        _scalePercentage = percentage * 10;
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
                return new KeyValuePair<bool, string>(true, "读码器设置成功");
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
            await Task.Yield();
            NLog.LogManager.GetCurrentClassLogger().Error($"调用绑定");
            var (key, value) = _cameraDictionary.FirstOrDefault(f => f.Key.Equals(info.CameraSerialNumber));
            if (!string.IsNullOrEmpty(key)) {
                _cameraManager ??= new CameraManager(dntLicenseKeys);
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
                NLog.LogManager.GetCurrentClassLogger().Error($"成功绑定");
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
            await Task.Yield();
            try {
                NLog.LogManager.GetCurrentClassLogger().Error($"调用启动");
                if (_selectCamera is not null && !_isOpend) {
                    //注册事件
                    _stopCancellationTokenSource = new CancellationTokenSource();
                    _frameProcessingTask = Task.Run(
                        () => ProcessFramesAsync(_stopCancellationTokenSource.Token),
                        _stopCancellationTokenSource.Token);
                    _selectCamera.OnFrameCaptrue += SelectCameraOnOnFrameCaptrue;
                    _selectCamera.Open();
                    _isOpend = true;
                    NLog.LogManager.GetCurrentClassLogger().Error($"启动成功");
                    return new KeyValuePair<bool, string>(true, "启动成功");
                }
                else {
                    NLog.LogManager.GetCurrentClassLogger().Error($"相机未绑定:{_isOpend},{_selectCamera is not null}");
                    return new KeyValuePair<bool, string>(false, "相机未绑定");
                }
            }
            catch (Exception e) {
                _stopCancellationTokenSource.Cancel();
                try {
                    _frameSignal.Release();
                }
                catch (SemaphoreFullException) {
                }
                if (_frameProcessingTask is not null) {
                    try {
                        await _frameProcessingTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                    }
                    _frameProcessingTask = null;
                }
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
        }

        /// <summary>
        /// 捕获图片事件
        /// </summary>
        /// <param name="bitmap"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SelectCameraOnOnFrameCaptrue(Bitmap bitmap) {
            var previous = Interlocked.Exchange(ref _pendingFrame, FastClone(bitmap));
            previous?.Dispose();
            try {
                _frameSignal.Release();
            }
            catch (SemaphoreFullException) {
            }
        }

        /// <summary>
        /// 在单个常驻任务中处理最新 USB 帧，避免逐帧创建任务。
        /// </summary>
        private async Task ProcessFramesAsync(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                await _frameSignal.WaitAsync(token).ConfigureAwait(false);
                var frame = Interlocked.Exchange(ref _pendingFrame, null);
                if (frame is not null) {
                    ReadFromFrame(frame, token);
                }
            }
        }

        /// <summary>
        /// 读码
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="token"></param>
        private void ReadFromFrame(Bitmap bitmap, CancellationToken token) {
            try {
                token.ThrowIfCancellationRequested();
                if (_framenum >= _recognitionSkipFrames) {
                    _framenum = 0;
                    long elapsedMilliseconds = 0;
                    TextResult[]? bars = null;
                    using var scaledBitmap = _scalePercentage is > 0 and < 100
                        ? GenerateThumbnail(
                            bitmap,
                            Math.Max(1, bitmap.Width * _scalePercentage / 100),
                            Math.Max(1, bitmap.Height * _scalePercentage / 100))
                        : null;
                    var decodeBitmap = scaledBitmap ?? bitmap;
                    if (mBarcodeReader is not null) {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        bars = DecodeBitmap(mBarcodeReader, decodeBitmap);
                        stopwatch.Stop();
                        elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    }

                    var barcodeScannedEventArgs = new BarcodeScannedEventArgs {
                        ScanTime = DateTime.Now,
                        CameraSerialNumber = UsbCameraInfo.CameraSerialNumber,
                        Image = bitmap
                    };
                    if (bars is { Length: > 0 }) {
                        barcodeScannedEventArgs.BarCodes = [.. bars.Select(s => new BarcodeInfo {
                            Barcode = s.BarcodeText,
                            BarcodeRegion = s.LocalizationResult.ResultPoints?.ToList(),
                            BarcodeType = s.LocalizationResult.BarcodeFormatString
                        })];
                        barcodeScannedEventArgs.RecognitionTime = elapsedMilliseconds;
                    }
                    OnBarcodeScanned(barcodeScannedEventArgs);
                }
                else {
                    OnBarcodeScanned(new BarcodeScannedEventArgs {
                        ScanTime = DateTime.Now,
                        CameraSerialNumber = UsbCameraInfo.CameraSerialNumber,
                        Image = bitmap
                    });
                }

                _framenum++;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) {
                bitmap.Dispose();
            }
            catch (Exception exception) {
                bitmap.Dispose();
                NLog.LogManager.GetCurrentClassLogger().Error($"{exception}");
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public async Task<KeyValuePair<bool, string>> Stop() {
            //解绑事件
            await Task.Yield();
            try {
                if (_selectCamera is not null && _isOpend) {
                    _selectCamera.OnFrameCaptrue -= SelectCameraOnOnFrameCaptrue;
                    _stopCancellationTokenSource.Cancel();
                    try {
                        _frameSignal.Release();
                    }
                    catch (SemaphoreFullException) {
                    }
                    if (_frameProcessingTask is not null) {
                        try {
                            await _frameProcessingTask;
                        }
                        catch (OperationCanceledException) {
                        }
                        _frameProcessingTask = null;
                    }
                    Interlocked.Exchange(ref _pendingFrame, null)?.Dispose();
                    _selectCamera?.Close();
                    _selectCamera?.Dispose();
                    _selectCamera = null;
                    _isOpend = false;
                    return new KeyValuePair<bool, string>(true, "停止成功");
                }
                else {
                    return new KeyValuePair<bool, string>(false, "相机未绑定");
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
        }

        //绑定相机
        //开始
        //停止

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            Stop().GetAwaiter().GetResult();
            //mBarcodeReader?.Recycle();
            mBarcodeReader?.Dispose();
            mBarcodeReader = null;
            _cameraManager?.Dispose();
            _cameraManager = null;
        }

        protected virtual void OnBarcodeScanned(BarcodeScannedEventArgs e) {
            var handler = BarcodeScanned;
            if (handler is null) {
                e.Image?.Dispose();
                return;
            }
            handler.Invoke(this, e);
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
            Rectangle rect = new(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try {
                var stride = Math.Abs(bmpData.Stride);
                var buffer = new byte[stride * bitmap.Height];
                for (var row = 0; row < bitmap.Height; row++) {
                    Marshal.Copy(
                        IntPtr.Add(bmpData.Scan0, row * bmpData.Stride),
                        buffer,
                        row * stride,
                        stride);
                }
                return buffer;
            }
            finally {
                bitmap.UnlockBits(bmpData);
            }
        }

        private static TextResult[]? DecodeBitmap(BarcodeReader reader, Bitmap bitmap) {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            var stride = Math.Abs(bitmapData.Stride);
            var bufferLength = checked(bitmapData.Height * stride);
            var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
            try {
                for (var row = 0; row < bitmapData.Height; row++) {
                    Marshal.Copy(
                        IntPtr.Add(bitmapData.Scan0, row * bitmapData.Stride),
                        buffer,
                        row * stride,
                        stride);
                }
                return reader.DecodeBuffer(
                    buffer,
                    bitmap.Width,
                    bitmap.Height,
                    stride,
                    GetImagePixelFormat(bitmap.PixelFormat),
                    string.Empty);
            }
            finally {
                bitmap.UnlockBits(bitmapData);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        // 获取位图的步长（stride）
        private static int GetStride(Bitmap bitmap) {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try {
                return Math.Abs(bmpData.Stride);
            }
            finally {
                bitmap.UnlockBits(bmpData);
            }
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
            try {
                var stride = Math.Abs(bitmapData.Stride);
                var buffer = new byte[bitmapData.Height * stride];
                for (var row = 0; row < bitmapData.Height; row++) {
                    var sourceRow = IntPtr.Add(bitmapData.Scan0, row * bitmapData.Stride);
                    Marshal.Copy(sourceRow, buffer, row * stride, stride);
                }

                return (buffer, stride, GetImagePixelFormat(bitmap.PixelFormat));
            }
            finally {
                bitmap.UnlockBits(bitmapData);
            }
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
            return sourceBitmap.Clone(
                new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                sourceBitmap.PixelFormat);
        }

        public static Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
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
