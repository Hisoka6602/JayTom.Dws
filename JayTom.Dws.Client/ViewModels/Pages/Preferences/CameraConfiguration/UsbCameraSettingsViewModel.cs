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
using System.Collections.ObjectModel;
using MathNet.Numerics.Distributions;
using JayTom.Dws.Camera.BarCodeReader;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using FontStyle = System.Drawing.FontStyle;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using FontFamily = System.Drawing.FontFamily;
using Matrix = System.Drawing.Drawing2D.Matrix;
using JayTom.Dws.Domain.Dto.CameraConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration
{

    public class UsbCameraSettingsViewModel : SettingsPageTemplateViewModel
    {
        private readonly IDeviceService _deviceService;
        private readonly IUsbCameraConfigRepository _usbCameraConfigRepository;

        private UsbCameraSettingsInfoModel _usbCameraSettingsInfo = new();
        private ObservableCollection<int> _deblurLevelItems = new([.. Enumerable.Range(0, 10)]);
        private ObservableCollection<int> _textureDetectionSensitivityItems = new([.. Enumerable.Range(0, 10)]);
        private UsbBarCodeReader? _usbBarCodeReader;
        private ObservableCollection<UsbCameraInfo> _cameraItems = new();

        private UsbCameraInfo _selectCameraInfo = new();
        private WriteableBitmap? _image = new(800, 600, 96, 96, PixelFormats.Bgr24, null);
        private bool _isLoaded = false;

        private CameraResolutionInfo _cameraResolution = new();
        private ObservableCollection<CameraResolutionInfo> _cameraResolutions = new();
        /// <summary>
        /// 有界预览图像队列。
        /// </summary>
        private readonly Queue<Bitmap> _bitmapQueue = new(2);
        /// <summary>
        /// 预览图像队列同步锁。
        /// </summary>
        private readonly System.Threading.Lock _bitmapQueueLock = new();
        /// <summary>
        /// 新预览图像到达信号。
        /// </summary>
        private readonly SemaphoreSlim _bitmapSignal = new(0, 1);
        /// <summary>
        /// 预览图像处理任务。
        /// </summary>
        private Task? _imageWorker;
        /// <summary>
        /// USB相机操作同步门，防止枚举、切换和参数更新并发访问同一原生设备。
        /// </summary>
        private readonly SemaphoreSlim _cameraOperationGate = new(1, 1);
        /// <summary>
        /// 预览任务取消源，页面卸载后停止常驻后台任务。
        /// </summary>
        private CancellationTokenSource? _imageWorkerCancellation;
        /// <summary>
        /// 相机参数更新版本，用于把高频界面变更合并为最新一次写入。
        /// </summary>
        private int _cameraParameterUpdateVersion;
        /// <summary>
        /// 相机参数更新工作器运行标记。
        /// </summary>
        private int _cameraParameterWorkerRunning;

        public UsbCameraSettingsViewModel(IDeviceService deviceService,
            IUsbCameraConfigRepository usbCameraConfigRepository,
            IConfigRepository configRepository) : base(configRepository)
        {
            _deviceService = deviceService;
            _usbCameraConfigRepository = usbCameraConfigRepository;
        }

        /// <summary>
        /// 接收算法设置变更并启动受观察的异步应用流程。
        /// </summary>
        private void OnSettingsChanged(SettingsChangedEvent item)
        {
            _ = OnSettingsChangedAsync(item);
        }

        /// <summary>
        /// 应用算法设置变更，并与相机切换操作互斥。
        /// </summary>
        private async Task OnSettingsChangedAsync(SettingsChangedEvent item)
        {
            if (item.SettingsName != "AlgorithmSettings")
            {
                return;
            }

            try
            {
                var usbBarcodeReaderDto =
                    await _configRepository.FirstOrDefaultEntity<UsbBarcodeReaderDto>(
                        "AlgorithmSettings") ?? new UsbBarcodeReaderDto();
                var barcodeFormat = GetBarcodeFormat(usbBarcodeReaderDto.BarcodeType);
                var dictionary = new Dictionary<BarcodeReaderParameter, object>
                {
                    { BarcodeReaderParameter.EnumBarcodeFormat, barcodeFormat },
                    { BarcodeReaderParameter.RecognitionMode, (ScanMode)usbBarcodeReaderDto.RecognitionMode },
                    { BarcodeReaderParameter.TextureDetectionSensitivity, usbBarcodeReaderDto.TextureDetectionSensitivity },
                    { BarcodeReaderParameter.BinarizationBlockSize, usbBarcodeReaderDto.BinarizationBlockSize },
                    { BarcodeReaderParameter.ExpectedBarcodesCount, usbBarcodeReaderDto.ExpectedBarcodesCount },
                    { BarcodeReaderParameter.DeblurLevel, usbBarcodeReaderDto.DeblurLevel },
                    { BarcodeReaderParameter.LocalizationMode, usbBarcodeReaderDto.LocalizationMode },
                    { BarcodeReaderParameter.IsUseTextFilterMode, usbBarcodeReaderDto.IsUseTextFilterMode },
                    { BarcodeReaderParameter.IsUseRegionPredetectionMode, usbBarcodeReaderDto.IsUseRegionPredetectionMode },
                    { BarcodeReaderParameter.ScaleDownThreshold, usbBarcodeReaderDto.ScaleDownThreshold },
                    { BarcodeReaderParameter.GrayscaleTransformationMode, usbBarcodeReaderDto.GrayscaleTransformationMode },
                    { BarcodeReaderParameter.ImagePreprocessingMode, usbBarcodeReaderDto.ImagePreprocessingMode },
                    { BarcodeReaderParameter.MinResultConfidence, usbBarcodeReaderDto.MinResultConfidence },
                    { BarcodeReaderParameter.RecognitionSkipFrames, usbBarcodeReaderDto.RecognitionSkipFrames },
                    { BarcodeReaderParameter.ScalePercentage, usbBarcodeReaderDto.ScalePercentage }
                };

                await _cameraOperationGate.WaitAsync();
                try
                {
                    if (_usbBarCodeReader is null)
                    {
                        return;
                    }

                    var (key, value) =
                        await _usbBarCodeReader.SetBarcodeReaderParameter(dictionary);
                    if (!key)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            base.MessageQueue.Enqueue(value));
                    }
                }
                finally
                {
                    _cameraOperationGate.Release();
                }
            }
            catch (Exception exception)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    base.MessageQueue.Enqueue($"应用USB读码设置失败:{exception.Message}"));
            }
        }

        /// <summary>
        /// 将条码类型位标志转换为读码器格式位标志，不创建临时映射集合。
        /// </summary>
        private static EnumBarcodeFormat GetBarcodeFormat(BarcodeType barcodeType)
        {
            var barcodeFormat = (EnumBarcodeFormat)0;
            if ((barcodeType & BarcodeType.QRCode) == BarcodeType.QRCode)
                barcodeFormat |= EnumBarcodeFormat.BF_QR_CODE;
            if ((barcodeType & BarcodeType.MicroQR) == BarcodeType.MicroQR)
                barcodeFormat |= EnumBarcodeFormat.BF_MICRO_QR;
            if ((barcodeType & BarcodeType.Code128) == BarcodeType.Code128)
                barcodeFormat |= EnumBarcodeFormat.BF_CODE_128;
            if ((barcodeType & BarcodeType.Code39) == BarcodeType.Code39)
                barcodeFormat |= EnumBarcodeFormat.BF_CODE_39;
            if ((barcodeType & BarcodeType.Code93) == BarcodeType.Code93)
                barcodeFormat |= EnumBarcodeFormat.BF_CODE_93;
            if ((barcodeType & BarcodeType.CodeBar) == BarcodeType.CodeBar)
                barcodeFormat |= EnumBarcodeFormat.BF_CODABAR;
            if ((barcodeType & BarcodeType.EAN13) == BarcodeType.EAN13)
                barcodeFormat |= EnumBarcodeFormat.BF_EAN_13;
            if ((barcodeType & BarcodeType.EAN8) == BarcodeType.EAN8)
                barcodeFormat |= EnumBarcodeFormat.BF_EAN_8;
            return barcodeFormat;
        }

        public WriteableBitmap? Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        public UsbCameraSettingsInfoModel UsbCameraSettingsInfo
        {
            get => _usbCameraSettingsInfo;
            set => SetProperty(ref _usbCameraSettingsInfo, value);
        }

        public ObservableCollection<CameraResolutionInfo> CameraResolutions
        {
            get => _cameraResolutions;
            set => SetProperty(ref _cameraResolutions, value);
        }

        public CameraResolutionInfo CameraResolution
        {
            get => _cameraResolution;
            set => SetProperty(ref _cameraResolution, value);
        }

        public ObservableCollection<int> DeblurLevelItems
        {
            get => _deblurLevelItems;
            set => SetProperty(ref _deblurLevelItems, value);
        }

        public ObservableCollection<UsbCameraInfo> CameraItems
        {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public UsbCameraInfo SelectCameraInfo
        {
            get => _selectCameraInfo;
            set => SetProperty(ref _selectCameraInfo, value);
        }

        public ObservableCollection<int> TextureDetectionSensitivityItems
        {
            get => _textureDetectionSensitivityItems;
            set => SetProperty(ref _textureDetectionSensitivityItems, value);
        }

        public override string Identifier => "UsbBarcodeReaderSettingsDialogHost";
        public override string SettingsName => "UsbBarcodeReaderSettings";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _usbCameraConfigRepository.InsertOrUpdate(new UsbCameraConfigInfoModel()
            {
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

        public override void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                EventAggregator.Instance.Subscribe<SettingsChangedEvent>(OnSettingsChanged);
                _imageWorkerCancellation?.Dispose();
                _imageWorkerCancellation = new CancellationTokenSource();
                _imageWorker = ProcessImageQueue(_imageWorkerCancellation.Token);
            }
        }

        /// <summary>
        /// 页面卸载命令。
        /// </summary>
        public ICommand UnloadedCommand => new DelegateCommand<object>(UnloadedDelegate);

        /// <summary>
        /// 页面卸载后停止预览任务并释放相机。
        /// </summary>
        private void UnloadedDelegate(object obj)
        {
            _isLoaded = false;
            EventAggregator.Instance.Unsubscribe<SettingsChangedEvent>(OnSettingsChanged);
            var cancellation = Interlocked.Exchange(ref _imageWorkerCancellation, null);
            cancellation?.Cancel();
            cancellation?.Dispose();
            _ = StopPreviewAsync();
        }

        /// <summary>
        /// 等待相机操作门后安全释放预览资源。
        /// </summary>
        private async Task StopPreviewAsync()
        {
            await _cameraOperationGate.WaitAsync();
            try
            {
                var reader = Interlocked.Exchange(ref _usbBarCodeReader, null);
                reader?.Dispose();
                ClearImages();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            finally
            {
                _cameraOperationGate.Release();
            }
        }

        /// <summary>
        /// 处理预览图像队列。
        /// </summary>
        private async Task ProcessImageQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _bitmapSignal.WaitAsync(token).ConfigureAwait(false);
                    Bitmap? bitmap = null;
                    lock (_bitmapQueueLock)
                    {
                        while (_bitmapQueue.Count > 0)
                        {
                            bitmap?.Dispose();
                            bitmap = _bitmapQueue.Dequeue();
                        }
                    }
                    var image = Image;
                    if (bitmap is null || image is null)
                    {
                        bitmap?.Dispose();
                        continue;
                    }
                    using (bitmap)
                    {
                        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly,
                            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        try
                        {
                            var stride = Math.Abs(bitmapData.Stride);
                            var scan0 = bitmapData.Stride < 0
                                ? IntPtr.Add(
                                    bitmapData.Scan0,
                                    bitmapData.Stride * (bitmapData.Height - 1))
                                : bitmapData.Scan0;
                            await image.Dispatcher.InvokeAsync(() =>
                            {
                                image.WritePixels(new Int32Rect(0, 0, bitmap.Width, bitmap.Height),
                                    scan0, stride * bitmapData.Height, stride);
                            }, DispatcherPriority.Background);
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData);
                        }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{e}");
                }
            }
        }

        /// <summary>
        /// 将图像加入有界预览队列。
        /// </summary>
        /// <param name="bitmap">待显示图像。</param>
        private void EnqueueImage(Bitmap bitmap)
        {
            lock (_bitmapQueueLock)
            {
                while (_bitmapQueue.Count >= 2)
                {
                    _bitmapQueue.Dequeue().Dispose();
                }
                _bitmapQueue.Enqueue(bitmap);
                if (_bitmapSignal.CurrentCount == 0)
                {
                    _bitmapSignal.Release();
                }
            }
        }

        /// <summary>
        /// 清空并释放预览图像。
        /// </summary>
        private void ClearImages()
        {
            lock (_bitmapQueueLock)
            {
                while (_bitmapQueue.Count > 0)
                {
                    _bitmapQueue.Dequeue().Dispose();
                }
            }
        }

        public ICommand CameraUpdateCommand => new DelegateCommand<object>(CameraUpdateDelegate);

        private void CameraUpdateDelegate(object obj)
        {
            if (_deviceService.RunningStatus)
            {
                base.MessageQueue.Enqueue("请先停止识别再调试摄像头!");
                return;
            }

            _ = UpdateCameraListAsync();
        }

        /// <summary>
        /// 在独立线程枚举USB相机，并完整观察刷新过程。
        /// </summary>
        private async Task UpdateCameraListAsync()
        {
            if (!await _cameraOperationGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                var reader = Interlocked.Exchange(ref _usbBarCodeReader, null);
                reader?.Dispose();
                ClearImages();
                var usbCameraInfos =
                    await Task.Run(UsbBarCodeReader.EnumerateCameras) ?? [];
                CameraItems.Clear();
                CameraItems.AddRange(usbCameraInfos);
                if (CameraItems.Count > 0)
                {
                    SelectCameraInfo = CameraItems[0];
                }
            }
            catch (Exception exception)
            {
                base.MessageQueue.Enqueue($"枚举USB相机失败:{exception.Message}");
            }
            finally
            {
                _cameraOperationGate.Release();
            }

            // _usbBarCodeReader ??= new UsbBarCodeReader();

            //刷新相机列表(判断是否在运行中,不在运行中才能刷新)

            //当相机列表下拉改变时刷新分辨率

            //定义接收图片事件

            //读码设置每个设置被改变时都需要重置设置，并使用改变后的设置(需要一个Command)
        }

        /// <summary>
        /// 切换相机
        /// </summary>
        public ICommand SwitchCameraCommand
        {
            get => new DelegateCommand<object>(SwitchCameraDelegate);
        }

        private async void SwitchCameraDelegate(object obj)
        {
            try
            {
                if (SelectCameraInfo?.CameraResolutions?.Any() == true)
                {
                    CameraResolutions.Clear();
                    var cameraResolutionInfos = SelectCameraInfo?.CameraResolutions?.Select(s => new CameraResolutionInfo
                    {
                        Size = new Size(s.Width, s.Height),
                        Display = $"{s.Width}x{s.Height}"
                    })?.OrderBy(s => s.Size.Width * s.Size.Height)?.ToList() ?? new List<CameraResolutionInfo>();
                    CameraResolutions.AddRange(cameraResolutionInfos);

                    //读参数

                    var usbCameraConfigInfoModel = await _usbCameraConfigRepository.FirstOrDefault(f =>
                        SelectCameraInfo != null && f.SerialNumber.Equals(SelectCameraInfo.CameraSerialNumber));
                    if (usbCameraConfigInfoModel is not null)
                    {
                        UsbCameraSettingsInfo = new UsbCameraSettingsInfoModel()
                        {
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
                    else
                    {
                        UsbCameraSettingsInfo = new UsbCameraSettingsInfoModel();
                        CameraResolution =
                            CameraResolutions.LastOrDefault() ?? new CameraResolutionInfo();
                    }

                    //切换相机
                    //切换分辨率x
                    //重新实例化
                    //重新加载
                    //切换显示
                }
            }
            catch (Exception exception)
            {
                base.MessageQueue.Enqueue($"切换USB相机失败:{exception.Message}");
            }
        }

        /// <summary>
        /// 切换分辨率
        /// </summary>
        public ICommand SwitchCameraResolutionCommand
        {
            get => new DelegateCommand<object>(SwitchCameraResolutionDelegate);
        }

        private async void SwitchCameraResolutionDelegate(object obj)
        {
            if (!await _cameraOperationGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                //实例化相机
                var previousReader = Interlocked.Exchange(ref _usbBarCodeReader, null);
                previousReader?.Dispose();
                ClearImages();

                if (CameraResolution?.Size is not { Width: > 0, Height: > 0 })
                {
                    return;
                }

                var reader = new UsbBarCodeReader();
                _usbBarCodeReader = reader;
                reader.BarcodeScanned += delegate (object? sender, BarcodeScannedEventArgs args)
                {
                    if (args.Image is not null)
                    {
                        var thumbnail = UsbBarCodeReader.GenerateThumbnail(args.Image);
                        if (thumbnail is not null)
                        {
                            List<Point>? points = null;
                            using var g = Graphics.FromImage(thumbnail);
                            using var borderPen = new System.Drawing.Pen(Color.Red, 5);

                            foreach (var barcodeInfo in args?.BarCodes ?? new List<BarcodeInfo>())
                            {
                                points = barcodeInfo.BarcodeRegion;
                                if (points is not null && points.Count == 4 &&
                                    args?.Image is { Width: > 0, Height: > 0 })
                                {
                                    var stPointList = new Point[4];
                                    for (var i = 0; i < 4; i++)
                                    {
                                        stPointList[i].X = points[i].X * thumbnail.Width / args.Image.Width;
                                        stPointList[i].Y = points[i].Y * thumbnail.Height / args.Image.Height;
                                    }
                                    g.DrawPolygon(borderPen, stPointList);
                                }
                            }
                            using var font = new Font(FontFamily.GenericSerif, 15, FontStyle.Bold);
                            using var brush = new SolidBrush(Color.Red);
                            g.DrawString($"{args?.RecognitionTime}ms", font, brush, 10, 10);
                            EnqueueImage(thumbnail);
                            Debug.WriteLine($"{JsonConvert.SerializeObject(args?.BarCodes)}");
                        }
                    }
                };

                var bindCamera = await reader.BindCamera(SelectCameraInfo);
                if (bindCamera)
                {
                    var (key, value) = await reader.Start();
                    if (!key)
                    {
                        base.MessageQueue.Enqueue(value);
                        reader.Dispose();
                        Interlocked.CompareExchange(ref _usbBarCodeReader, null, reader);
                    }
                    else
                    {
                        //设置指定分辨率
                        await reader.SetUsbCameraParameter(new Dictionary<UsbCameraParameter, object>()
                        {
                            { UsbCameraParameter.Resolution, CameraResolution.Size }
                        });

                        await ApplyCameraParametersAsync(reader);
                    }
                }
                else
                {
                    reader.Dispose();
                    Interlocked.CompareExchange(ref _usbBarCodeReader, null, reader);
                    base.MessageQueue.Enqueue("绑定USB相机失败");
                }
            }
            catch (Exception exception)
            {
                var reader = Interlocked.Exchange(ref _usbBarCodeReader, null);
                reader?.Dispose();
                base.MessageQueue.Enqueue($"切换USB相机分辨率失败:{exception.Message}");
            }
            finally
            {
                _cameraOperationGate.Release();
            }
        }

        /// <summary>
        /// 修改相机参数
        /// </summary>
        public ICommand UpdateCameraParametersCommand => new DelegateCommand<object>(UpdateCameraParametersDelegate);

        private void UpdateCameraParametersDelegate(object obj)
        {
            Interlocked.Increment(ref _cameraParameterUpdateVersion);
            _ = ProcessPendingCameraParametersAsync();
        }

        /// <summary>
        /// 合并高频参数更新，只向相机写入各批次中的最新状态。
        /// </summary>
        private async Task ProcessPendingCameraParametersAsync()
        {
            if (Interlocked.CompareExchange(ref _cameraParameterWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            var processedVersion = 0;
            try
            {
                while (true)
                {
                    processedVersion = Volatile.Read(ref _cameraParameterUpdateVersion);
                    await _cameraOperationGate.WaitAsync();
                    try
                    {
                        if (_usbBarCodeReader is not null)
                        {
                            await ApplyCameraParametersAsync(_usbBarCodeReader);
                        }
                    }
                    finally
                    {
                        _cameraOperationGate.Release();
                    }

                    if (processedVersion == Volatile.Read(ref _cameraParameterUpdateVersion))
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                base.MessageQueue.Enqueue($"更新USB相机参数失败:{exception.Message}");
            }
            finally
            {
                Volatile.Write(ref _cameraParameterWorkerRunning, 0);
                if (processedVersion != Volatile.Read(ref _cameraParameterUpdateVersion))
                {
                    _ = ProcessPendingCameraParametersAsync();
                }
            }
        }

        /// <summary>
        /// 将当前界面参数写入指定USB相机实例。
        /// </summary>
        private async Task ApplyCameraParametersAsync(UsbBarCodeReader reader)
        {
            var dictionary = new Dictionary<UsbCameraParameter, object>
            {
                //分辨率
                { UsbCameraParameter.Resolution, CameraResolution.Size }
            };

            //曝光度
            if (UsbCameraSettingsInfo.IsCustomExposureEnabled)
                dictionary.Add(UsbCameraParameter.Exposure, UsbCameraSettingsInfo.Exposure);
            //亮度
            if (UsbCameraSettingsInfo.IsCustomBrightnessEnabled)
                dictionary.Add(UsbCameraParameter.Brightness, UsbCameraSettingsInfo.Brightness);
            //对比度
            if (UsbCameraSettingsInfo.IsCustomContrastEnabled)
                dictionary.Add(UsbCameraParameter.Contrast, UsbCameraSettingsInfo.Contrast);
            //色调
            if (UsbCameraSettingsInfo.IsCustomHueEnabled)
                dictionary.Add(UsbCameraParameter.Hue, UsbCameraSettingsInfo.Hue);
            //饱和度
            if (UsbCameraSettingsInfo.IsCustomSaturationEnabled)
                dictionary.Add(UsbCameraParameter.Saturation, UsbCameraSettingsInfo.Saturation);
            //锐度
            if (UsbCameraSettingsInfo.IsCustomSharpnessEnabled)
                dictionary.Add(UsbCameraParameter.Sharpness, UsbCameraSettingsInfo.Sharpness);
            //伽马值
            if (UsbCameraSettingsInfo.IsCustomGammaEnabled)
                dictionary.Add(UsbCameraParameter.Gamma, UsbCameraSettingsInfo.Gamma);
            //白平衡
            if (UsbCameraSettingsInfo.IsCustomWhiteBalanceEnabled)
                dictionary.Add(UsbCameraParameter.WhiteBalance, UsbCameraSettingsInfo.WhiteBalance);
            //背光补偿
            if (UsbCameraSettingsInfo.IsCustomBacklightCompensationEnabled)
                dictionary.Add(UsbCameraParameter.BklightComp, UsbCameraSettingsInfo.BklightComp);
            //增益
            if (UsbCameraSettingsInfo.IsCustomGainEnabled)
                dictionary.Add(UsbCameraParameter.Gain, UsbCameraSettingsInfo.Gain);
            //缩放
            if (UsbCameraSettingsInfo.IsCustomZoomEnabled)
                dictionary.Add(UsbCameraParameter.Zoom, UsbCameraSettingsInfo.Zoom);
            //焦距
            if (UsbCameraSettingsInfo.IsCustomFocusEnabled)
                dictionary.Add(UsbCameraParameter.Focus, UsbCameraSettingsInfo.Focus);
            //光圈
            if (UsbCameraSettingsInfo.IsCustomApertureEnabled)
                dictionary.Add(UsbCameraParameter.Iris, UsbCameraSettingsInfo.Iris);
            //水平旋转
            if (UsbCameraSettingsInfo.IsCustomHorizontalRotationEnabled)
                dictionary.Add(UsbCameraParameter.Pan, UsbCameraSettingsInfo.Pan);
            //垂直旋转
            if (UsbCameraSettingsInfo.IsCustomVerticalRotationEnabled)
                dictionary.Add(UsbCameraParameter.Tilt, UsbCameraSettingsInfo.Tilt);
            //翻转
            if (UsbCameraSettingsInfo.IsCustomFlipEnabled)
                dictionary.Add(UsbCameraParameter.Roll, UsbCameraSettingsInfo.Roll);

            await reader.SetUsbCameraParameter(dictionary);
        }

        private static WriteableBitmap CreateTransparentBitmap(int width, int height)
        {
            var stride = (width * PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            var pixelData = new byte[stride * height];

            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);

            return bitmap;
        }
    }

    public class CameraResolutionInfo
    {
        public Size Size { get; set; }
        public string Display { get; set; } = string.Empty;
    }
}
