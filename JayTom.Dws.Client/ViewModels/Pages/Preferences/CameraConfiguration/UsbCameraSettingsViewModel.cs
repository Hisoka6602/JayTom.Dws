using System;
using Dynamsoft;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using TouchSocket.Core;
using JayTom.Dws.Camera;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using Org.BouncyCastle.Tsp;
using MaterialDesignColors;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using Size = System.Drawing.Size;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using MathNet.Numerics.Distributions;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.EventMediators;
using FontStyle = System.Drawing.FontStyle;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using FontFamily = System.Drawing.FontFamily;
using Matrix = System.Drawing.Drawing2D.Matrix;
using JayTom.Dws.Domain.Dto.CameraConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class UsbCameraSettingsViewModel : SettingsPageTemplateViewModel {
        private readonly IDeviceService _deviceService;
        private readonly IUsbCameraConfigRepository _usbCameraConfigRepository;

        private UsbCameraSettingsInfoModel _usbCameraSettingsInfo = new();
        private ObservableCollection<int> _deblurLevelItems = new(Enumerable.Range(0, 10).ToList());
        private ObservableCollection<int> _textureDetectionSensitivityItems = new(Enumerable.Range(0, 10).ToList());
        private UsbBarCodeReader? _usbBarCodeReader;
        private ObservableCollection<UsbCameraInfo> _cameraItems = new();

        private UsbCameraInfo _selectCameraInfo = new();
        private bool _isUpdate = false;
        private WriteableBitmap? _image = new(800, 600, 96, 96, PixelFormats.Bgr24, null);
        private bool _isLoaded = false;

        private CameraResolutionInfo _cameraResolution = new();
        private ObservableCollection<CameraResolutionInfo> _cameraResolutions = new();

        public UsbCameraSettingsViewModel(IDeviceService deviceService,
            IUsbCameraConfigRepository usbCameraConfigRepository,
            IConfigRepository configRepository) : base(configRepository) {
            _deviceService = deviceService;
            _usbCameraConfigRepository = usbCameraConfigRepository;

            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(item => {
                if (item is SettingsChangedEvent { SettingsName: "AlgorithmSettings" } model) {
                    Task.Run(async () => {
                        var usbBarcodeReaderDto = await _configRepository.FirstOrDefaultEntity<UsbBarcodeReaderDto>(SettingsName) ??
                                                  new UsbBarcodeReaderDto();

                        var barcodeMapping = new Dictionary<BarcodeType, EnumBarcodeFormat>
                        {
                            { BarcodeType.QRCode, EnumBarcodeFormat.BF_QR_CODE },
                            { BarcodeType.MicroQR, EnumBarcodeFormat.BF_MICRO_QR },
                            { BarcodeType.Code128, EnumBarcodeFormat.BF_CODE_128 },
                            { BarcodeType.Code39, EnumBarcodeFormat.BF_CODE_39 },
                            { BarcodeType.Code93, EnumBarcodeFormat.BF_CODE_93 },
                            { BarcodeType.CodeBar, EnumBarcodeFormat.BF_CODABAR },
                            { BarcodeType.EAN13, EnumBarcodeFormat.BF_EAN_13 },
                            { BarcodeType.EAN8, EnumBarcodeFormat.BF_EAN_8 },
                        };
                        var barcodeFormat = barcodeMapping.Where(kvp => (usbBarcodeReaderDto.BarcodeType & kvp.Key) == kvp.Key).Aggregate<KeyValuePair<BarcodeType, EnumBarcodeFormat>, EnumBarcodeFormat>(0, (current, kvp) => current | kvp.Value);

                        var dictionary = new Dictionary<BarcodeReaderParameter, object>()
                        {
                    { BarcodeReaderParameter.EnumBarcodeFormat,barcodeFormat },
                    { BarcodeReaderParameter.RecognitionMode,(ScanMode)usbBarcodeReaderDto.RecognitionMode },
                    { BarcodeReaderParameter.TextureDetectionSensitivity,usbBarcodeReaderDto.TextureDetectionSensitivity },
                    { BarcodeReaderParameter.BinarizationBlockSize,usbBarcodeReaderDto.BinarizationBlockSize },
                    { BarcodeReaderParameter.ExpectedBarcodesCount,usbBarcodeReaderDto.ExpectedBarcodesCount },
                    { BarcodeReaderParameter.DeblurLevel,usbBarcodeReaderDto.DeblurLevel },
                    { BarcodeReaderParameter.LocalizationMode,usbBarcodeReaderDto.LocalizationMode },
                    { BarcodeReaderParameter.IsUseTextFilterMode,usbBarcodeReaderDto.IsUseTextFilterMode },
                    { BarcodeReaderParameter.IsUseRegionPredetectionMode,usbBarcodeReaderDto.IsUseRegionPredetectionMode },
                    { BarcodeReaderParameter.ScaleDownThreshold,usbBarcodeReaderDto.ScaleDownThreshold },
                    { BarcodeReaderParameter.GrayscaleTransformationMode,usbBarcodeReaderDto.GrayscaleTransformationMode },
                    { BarcodeReaderParameter.ImagePreprocessingMode,usbBarcodeReaderDto.ImagePreprocessingMode },
                    { BarcodeReaderParameter.MinResultConfidence,usbBarcodeReaderDto.MinResultConfidence },
                    { BarcodeReaderParameter.RecognitionSkipFrames,usbBarcodeReaderDto.RecognitionSkipFrames },
                    { BarcodeReaderParameter.ScalePercentage,usbBarcodeReaderDto.ScalePercentage },
                };
                        if (_usbBarCodeReader is not null) {
                            var (key, value) = await _usbBarCodeReader.SetBarcodeReaderParameter(dictionary);
                            if (!key) {
                                await Application.Current.Dispatcher.InvokeAsync(() => {
                                    base.MessageQueue.Enqueue(value);
                                });
                            }
                        }
                    });
                }
            });
        }

        public WriteableBitmap? Image {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        /// <summary>
        /// 图片队列
        /// </summary>
        public ConcurrentQueue<Bitmap> BitmapQueue { get; init; } = new();

        public UsbCameraSettingsInfoModel UsbCameraSettingsInfo {
            get => _usbCameraSettingsInfo;
            set => SetProperty(ref _usbCameraSettingsInfo, value);
        }

        public ObservableCollection<CameraResolutionInfo> CameraResolutions {
            get => _cameraResolutions;
            set => SetProperty(ref _cameraResolutions, value);
        }

        public CameraResolutionInfo CameraResolution {
            get => _cameraResolution;
            set => SetProperty(ref _cameraResolution, value);
        }

        public ObservableCollection<int> DeblurLevelItems {
            get => _deblurLevelItems;
            set => SetProperty(ref _deblurLevelItems, value);
        }

        public ObservableCollection<UsbCameraInfo> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public UsbCameraInfo SelectCameraInfo {
            get => _selectCameraInfo;
            set => SetProperty(ref _selectCameraInfo, value);
        }

        public ObservableCollection<int> TextureDetectionSensitivityItems {
            get => _textureDetectionSensitivityItems;
            set => SetProperty(ref _textureDetectionSensitivityItems, value);
        }

        public override string Identifier => "UsbBarcodeReaderSettingsDialogHost";
        public override string SettingsName => "UsbBarcodeReaderSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _usbCameraConfigRepository.InsertOrUpdate(new UsbCameraConfigInfoModel() {
                Exposure = UsbCameraSettingsInfo.Exposure,
                Brightness = UsbCameraSettingsInfo.Brightness,
                Contrast = UsbCameraSettingsInfo.Contrast,
                Hue = UsbCameraSettingsInfo.Hue,
                Saturation = UsbCameraSettingsInfo.Saturation,
                Sharpness = UsbCameraSettingsInfo.Sharpness,
                Gamma = UsbCameraSettingsInfo.Gamma,
                WhiteBalance = UsbCameraSettingsInfo.WhiteBalance,
                BklightComp = UsbCameraSettingsInfo.BklightComp,
                Gain = UsbCameraSettingsInfo.Gain,
                Zoom = UsbCameraSettingsInfo.Zoom,
                Focus = UsbCameraSettingsInfo.Focus,
                Iris = UsbCameraSettingsInfo.Iris,
                Pan = UsbCameraSettingsInfo.Pan,
                Tilt = UsbCameraSettingsInfo.Tilt,
                Roll = UsbCameraSettingsInfo.Roll,
                IsCustomExposureEnabled = UsbCameraSettingsInfo.IsCustomExposureEnabled,
                IsCustomBrightnessEnabled = UsbCameraSettingsInfo.IsCustomBrightnessEnabled,
                IsCustomContrastEnabled = UsbCameraSettingsInfo.IsCustomContrastEnabled,
                IsCustomHueEnabled = UsbCameraSettingsInfo.IsCustomHueEnabled,
                IsCustomSaturationEnabled = UsbCameraSettingsInfo.IsCustomSaturationEnabled,
                IsCustomSharpnessEnabled = UsbCameraSettingsInfo.IsCustomSharpnessEnabled,
                IsCustomGammaEnabled = UsbCameraSettingsInfo.IsCustomGammaEnabled,
                IsCustomWhiteBalanceEnabled = UsbCameraSettingsInfo.IsCustomWhiteBalanceEnabled,
                IsCustomBacklightCompensationEnabled =
                                UsbCameraSettingsInfo.IsCustomBacklightCompensationEnabled,
                IsCustomGainEnabled = UsbCameraSettingsInfo.IsCustomGainEnabled,
                IsCustomZoomEnabled = UsbCameraSettingsInfo.IsCustomZoomEnabled,
                IsCustomFocusEnabled = UsbCameraSettingsInfo.IsCustomFocusEnabled,
                IsCustomApertureEnabled = UsbCameraSettingsInfo.IsCustomApertureEnabled,
                IsCustomHorizontalRotationEnabled = UsbCameraSettingsInfo.IsCustomHorizontalRotationEnabled,
                IsCustomVerticalRotationEnabled = UsbCameraSettingsInfo.IsCustomVerticalRotationEnabled,
                IsCustomFlipEnabled = UsbCameraSettingsInfo.IsCustomFlipEnabled,
                Name = SelectCameraInfo?.CameraName ?? string.Empty,
                SerialNumber = SelectCameraInfo?.CameraSerialNumber ?? string.Empty,
                ResolutionHeight = CameraResolution.Size.Height,
                ResolutionWidth = CameraResolution.Size.Width,
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            base.MessageQueue.Enqueue("请重启程序");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await Task.Factory.StartNew(async () => {
                    try {
                        while (true) {
                            var tryDequeue = BitmapQueue.TryDequeue(out var bitmap);
                            if (tryDequeue && bitmap is not null && this.Image is not null) {
                                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                                var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                await this.Image.Dispatcher.InvokeAsync(() => {
                                    this.Image.WritePixels(new Int32Rect(0, 0, bitmap.Width, bitmap.Height), bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                                    bitmap.UnlockBits(bitmapData);
                                }, DispatcherPriority.Render);
                            }
                            await Task.Delay(1);
                        }
                    }
                    catch (Exception e) {
                        Debug.WriteLine($"{e}");
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        public ICommand CameraUpdateCommand => new DelegateCommand<object>(CameraUpdateDelegate);

        private void CameraUpdateDelegate(object obj) {
            if (_deviceService.RunningStatus) {
                base.MessageQueue.Enqueue("请先停止识别再调试摄像头!");
                return;
            }

            Task.Run(async () => {
                try {
                    if (!_isUpdate) {
                        _isUpdate = true;
                        _usbBarCodeReader?.Dispose();
                        await Task.Delay(500);
                        _usbBarCodeReader = null;
                        BitmapQueue.Clear();
                        var usbCameraInfos = UsbBarCodeReader.EnumerateCameras();
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            CameraItems.Clear();
                            var list = usbCameraInfos.Select(s => s)?.ToList() ?? new List<UsbCameraInfo>();
                            CameraItems.AddRange(list);
                            if (CameraItems.Any()) {
                                SelectCameraInfo = CameraItems.FirstOrDefault() ?? new UsbCameraInfo();
                            }
                        });
                    }
                }
                finally {
                    _isUpdate = false;
                }
            });

            // _usbBarCodeReader ??= new UsbBarCodeReader();

            //刷新相机列表(判断是否在运行中,不在运行中才能刷新)

            //当相机列表下拉改变时刷新分辨率

            //定义接收图片事件

            //读码设置每个设置被改变时都需要重置设置，并使用改变后的设置(需要一个Command)
        }

        /// <summary>
        /// 切换相机
        /// </summary>
        public ICommand SwitchCameraCommand {
            get => new DelegateCommand<object>(SwitchCameraDelegate);
        }

        private async void SwitchCameraDelegate(object obj) {
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                if (SelectCameraInfo?.CameraResolutions?.Any() == true) {
                    CameraResolutions.Clear();
                    var cameraResolutionInfos = SelectCameraInfo?.CameraResolutions?.Select(s => new CameraResolutionInfo {
                        Size = new Size(s.Width, s.Height),
                        Display = $"{s.Width}x{s.Height}"
                    })?.OrderBy(s => s.Size.Width * s.Size.Height)?.ToList() ?? new List<CameraResolutionInfo>();
                    CameraResolutions.AddRange(cameraResolutionInfos);

                    //读参数

                    var usbCameraConfigInfoModel = await _usbCameraConfigRepository.FirstOrDefault(f =>
                        SelectCameraInfo != null && f.SerialNumber.Equals(SelectCameraInfo.CameraSerialNumber));
                    if (usbCameraConfigInfoModel is not null) {
                        UsbCameraSettingsInfo = new UsbCameraSettingsInfoModel() {
                            Exposure = usbCameraConfigInfoModel.Exposure,
                            Resolution = new Size(usbCameraConfigInfoModel.ResolutionWidth, usbCameraConfigInfoModel.ResolutionHeight),
                            Brightness = usbCameraConfigInfoModel.Brightness,
                            Contrast = usbCameraConfigInfoModel.Contrast,
                            Hue = usbCameraConfigInfoModel.Hue,
                            Saturation = usbCameraConfigInfoModel.Saturation,
                            Sharpness = usbCameraConfigInfoModel.Sharpness,
                            Gamma = usbCameraConfigInfoModel.Gamma,
                            WhiteBalance = usbCameraConfigInfoModel.WhiteBalance,
                            BklightComp = usbCameraConfigInfoModel.BklightComp,
                            Gain = usbCameraConfigInfoModel.Gain,
                            Zoom = usbCameraConfigInfoModel.Zoom,
                            Focus = usbCameraConfigInfoModel.Focus,
                            Iris = usbCameraConfigInfoModel.Iris,
                            Pan = usbCameraConfigInfoModel.Pan,
                            Tilt = usbCameraConfigInfoModel.Tilt,
                            Roll = usbCameraConfigInfoModel.Roll,
                            IsCustomExposureEnabled = usbCameraConfigInfoModel.IsCustomExposureEnabled,
                            IsCustomBrightnessEnabled = usbCameraConfigInfoModel.IsCustomBrightnessEnabled,
                            IsCustomContrastEnabled = usbCameraConfigInfoModel.IsCustomContrastEnabled,
                            IsCustomHueEnabled = usbCameraConfigInfoModel.IsCustomHueEnabled,
                            IsCustomSaturationEnabled = usbCameraConfigInfoModel.IsCustomSaturationEnabled,
                            IsCustomSharpnessEnabled = usbCameraConfigInfoModel.IsCustomSharpnessEnabled,
                            IsCustomGammaEnabled = usbCameraConfigInfoModel.IsCustomGammaEnabled,
                            IsCustomWhiteBalanceEnabled = usbCameraConfigInfoModel.IsCustomWhiteBalanceEnabled,
                            IsCustomBacklightCompensationEnabled =
                                usbCameraConfigInfoModel.IsCustomBacklightCompensationEnabled,
                            IsCustomGainEnabled = usbCameraConfigInfoModel.IsCustomGainEnabled,
                            IsCustomZoomEnabled = usbCameraConfigInfoModel.IsCustomZoomEnabled,
                            IsCustomFocusEnabled = usbCameraConfigInfoModel.IsCustomFocusEnabled,
                            IsCustomApertureEnabled = usbCameraConfigInfoModel.IsCustomApertureEnabled,
                            IsCustomHorizontalRotationEnabled =
                                usbCameraConfigInfoModel.IsCustomHorizontalRotationEnabled,
                            IsCustomVerticalRotationEnabled = usbCameraConfigInfoModel.IsCustomVerticalRotationEnabled,
                            IsCustomFlipEnabled = usbCameraConfigInfoModel.IsCustomFlipEnabled,
                        };

                        UsbCameraSettingsInfo.Resolution = SelectCameraInfo?.CameraResolutions?.FirstOrDefault(f =>
                                                               f.Width.Equals(UsbCameraSettingsInfo.Resolution.Width) &&
                                                               f.Height.Equals(UsbCameraSettingsInfo.Resolution
                                                                   .Height)) ??
                                                           SelectCameraInfo?.CameraResolutions?.LastOrDefault()
                                                           ?? new Size(0, 0);

                        CameraResolution = CameraResolutions.FirstOrDefault(f =>
                            f.Size.Width.Equals(UsbCameraSettingsInfo.Resolution.Width) &&
                            f.Size.Height.Equals(UsbCameraSettingsInfo.Resolution.Height)) ?? new CameraResolutionInfo();
                    }
                    else {
                        UsbCameraSettingsInfo = new UsbCameraSettingsInfoModel();
                    }

                    //切换相机
                    //切换分辨率x
                    //重新实例化
                    //重新加载
                    //切换显示
                }
            });
        }

        /// <summary>
        /// 切换分辨率
        /// </summary>
        public ICommand SwitchCameraResolutionCommand {
            get => new DelegateCommand<object>(SwitchCameraResolutionDelegate);
        }

        private async void SwitchCameraResolutionDelegate(object obj) {
            //实例化相机
            _usbBarCodeReader?.Dispose();
            await Task.Delay(500);
            _usbBarCodeReader = null;
            BitmapQueue.Clear();

            if (CameraResolution?.Size is { Width: > 0, Height: > 0 }) {
                _usbBarCodeReader = new UsbBarCodeReader();

                _usbBarCodeReader.BarcodeScanned += delegate (object? sender, BarcodeScannedEventArgs args) {
                    if (args.Image is not null) {
                        var thumbnail = UsbBarCodeReader.GenerateThumbnail(args.Image);
                        if (thumbnail is not null) {
                            List<Point>? points = null;
                            using var g = Graphics.FromImage(thumbnail);

                            foreach (var barcodeInfo in args?.BarCodes ?? new List<BarcodeInfo>()) {
                                points = barcodeInfo.BarcodeRegion;
                                if (points is not null && points.Count == 4 &&
                                    args?.Image is { Width: > 0, Height: > 0 }) {
                                    var stPointList = new Point[4];
                                    for (var i = 0; i < 4; i++) {
                                        stPointList[i].X = (int)(points[i].X *
                                                                 ((float)thumbnail.Width / args.Image.Width));
                                        stPointList[i].Y = (int)(points[i].Y *
                                                                 ((float)thumbnail.Height / args.Image.Height));
                                    }
                                    g.DrawPolygon(new System.Drawing.Pen(Color.Red, 5), stPointList);
                                }
                            }
                            g.DrawString($"{args?.RecognitionTime}ms", new Font(FontFamily.GenericSerif, 15, FontStyle.Bold),
                                new SolidBrush(Color.Red), 10, 10);
                            BitmapQueue.Enqueue(thumbnail);
                            Debug.WriteLine($"{JsonConvert.SerializeObject(args?.BarCodes)}");
                        }
                    }
                };

                var bindCamera = await _usbBarCodeReader.BindCamera(SelectCameraInfo);
                if (bindCamera) {
                    var (key, value) = await _usbBarCodeReader.Start();
                    if (!key) {
                        base.MessageQueue.Enqueue(value);
                    }
                    else {
                        //设置指定分辨率
                        await _usbBarCodeReader.SetUsbCameraParameter(new Dictionary<UsbCameraParameter, object>()
                        {
                            { UsbCameraParameter.Resolution, CameraResolution.Size }
                        });

                        UpdateCameraParametersDelegate(this);
                    }
                }
            }
        }

        /// <summary>
        /// 修改相机参数
        /// </summary>
        public ICommand UpdateCameraParametersCommand => new DelegateCommand<object>(UpdateCameraParametersDelegate);

        private async void UpdateCameraParametersDelegate(object obj) {
            //实时设置相机参数

            if (_usbBarCodeReader is not null) {
                var dictionary = new Dictionary<UsbCameraParameter, object> {
                    //分辨率
                    { UsbCameraParameter.Resolution, CameraResolution.Size }
                };

                //曝光度
                if (UsbCameraSettingsInfo.IsCustomExposureEnabled) {
                    dictionary.Add(UsbCameraParameter.Exposure, UsbCameraSettingsInfo.Exposure);
                }
                //亮度
                if (UsbCameraSettingsInfo.IsCustomBrightnessEnabled) {
                    dictionary.Add(UsbCameraParameter.Brightness, UsbCameraSettingsInfo.Brightness);
                }
                //对比度
                if (UsbCameraSettingsInfo.IsCustomContrastEnabled) {
                    dictionary.Add(UsbCameraParameter.Contrast, UsbCameraSettingsInfo.Contrast);
                }
                //色调
                if (UsbCameraSettingsInfo.IsCustomHueEnabled) {
                    dictionary.Add(UsbCameraParameter.Hue, UsbCameraSettingsInfo.Hue);
                }
                //锐度
                if (UsbCameraSettingsInfo.IsCustomSharpnessEnabled) {
                    dictionary.Add(UsbCameraParameter.Sharpness, UsbCameraSettingsInfo.Sharpness);
                }
                //伽马值
                if (UsbCameraSettingsInfo.IsCustomGammaEnabled) {
                    dictionary.Add(UsbCameraParameter.Gamma, UsbCameraSettingsInfo.Gamma);
                }
                //白平衡
                if (UsbCameraSettingsInfo.IsCustomWhiteBalanceEnabled) {
                    dictionary.Add(UsbCameraParameter.WhiteBalance, UsbCameraSettingsInfo.WhiteBalance);
                }
                //背光补偿
                if (UsbCameraSettingsInfo.IsCustomBacklightCompensationEnabled) {
                    dictionary.Add(UsbCameraParameter.BklightComp, UsbCameraSettingsInfo.BklightComp);
                }
                //增益
                await _usbBarCodeReader.SetUsbCameraParameter(dictionary);
            }
        }

        private static WriteableBitmap CreateTransparentBitmap(int width, int height) {
            var stride = (width * PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            var pixelData = new byte[stride * height];

            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);

            return bitmap;
        }
    }

    public class CameraResolutionInfo {
        public Size Size { get; set; }
        public string Display { get; set; } = string.Empty;
    }
}