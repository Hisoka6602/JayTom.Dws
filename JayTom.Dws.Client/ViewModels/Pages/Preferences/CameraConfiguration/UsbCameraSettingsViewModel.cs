using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Camera;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Collections.Generic;
using Size = System.Drawing.Size;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration {

    public class UsbCameraSettingsViewModel : BindableBase {
        private readonly IDeviceService _deviceService;
        private BarcodeReaderSettingsInfoModel _barcodeReaderSettingsInfo = new();
        private UsbCameraSettingsInfoModel _usbCameraSettingsInfo = new();
        private ObservableCollection<int> _deblurLevelItems = new(Enumerable.Range(0, 10).ToList());
        private ObservableCollection<int> _textureDetectionSensitivityItems = new(Enumerable.Range(0, 10).ToList());
        private UsbBarCodeReader? _usbBarCodeReader;
        private ObservableCollection<UsbCameraInfo> _cameraItems = new();
        private SnackbarMessageQueue _usbCameraSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private UsbCameraInfo _selectCameraInfo = new();
        private bool _isUpdate = false;
        private WriteableBitmap? _image = new(800, 600, 96, 96, PixelFormats.Bgr24, null);
        private bool _isLoaded = false;

        public UsbCameraSettingsViewModel(IDeviceService deviceService) {
            _deviceService = deviceService;
        }

        public WriteableBitmap? Image {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        /// <summary>
        /// 图片队列
        /// </summary>
        public ConcurrentQueue<Bitmap> BitmapQueue { get; init; } = new();

        public SnackbarMessageQueue UsbCameraSettingsMessageQueue {
            get => _usbCameraSettingsMessageQueue;
            set => SetProperty(ref _usbCameraSettingsMessageQueue, value);
        }

        public BarcodeReaderSettingsInfoModel BarcodeReaderSettingsInfo {
            get => _barcodeReaderSettingsInfo;
            set => SetProperty(ref _barcodeReaderSettingsInfo, value);
        }

        public UsbCameraSettingsInfoModel UsbCameraSettingsInfo {
            get => _usbCameraSettingsInfo;
            set => SetProperty(ref _usbCameraSettingsInfo, value);
        }

        public ObservableCollection<CameraResolutionInfo> CameraResolutions { get; set; } = new();
        public CameraResolutionInfo CameraResolution { get; set; } = new();

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

        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
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

        public ICommand CameraUpdateCommand {
            get => new DelegateCommand<object>(CameraUpdateDelegate);
        }

        private void CameraUpdateDelegate(object obj) {
            if (_deviceService.RunningStatus) {
                UsbCameraSettingsMessageQueue.Enqueue("请先停止识别再调试摄像头!");
                return;
            }

            Task.Run(async () => {
                try {
                    if (!_isUpdate) {
                        _isUpdate = true;
                        var usbCameraInfos = UsbBarCodeReader.EnumerateCameras();
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            CameraItems.Clear();
                            var list = usbCameraInfos.Select(s => s)?.ToList() ?? new List<UsbCameraInfo>();
                            CameraItems.AddRange(list);
                        });
                    }
                }
                finally {
                    _isUpdate = false;
                }
            });

            // _usbBarCodeReader ??= new UsbBarCodeReader();

            Console.WriteLine(obj);

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
            await Application.Current.Dispatcher.InvokeAsync(() => {
                if (SelectCameraInfo?.CameraResolutions?.Any() == true) {
                    CameraResolutions.Clear();
                    var cameraResolutionInfos = SelectCameraInfo?.CameraResolutions?.Select(s => new CameraResolutionInfo {
                        Size = new Size(s.Width, s.Height),
                        Display = $"{s.Width}x{s.Height}"
                    })?.OrderBy(s => s.Size.Width * s.Size.Height)?.ToList() ?? new List<CameraResolutionInfo>();
                    CameraResolutions.AddRange(cameraResolutionInfos);

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
            _usbBarCodeReader = new UsbBarCodeReader();
            _usbBarCodeReader.ImageDataReceived += delegate (object? sender, Bitmap bitmap) {
                var thumbnail = UsbBarCodeReader.GenerateThumbnail(bitmap);
                if (thumbnail is not null) {
                    BitmapQueue.Enqueue(thumbnail);
                }
            };
            _usbBarCodeReader.BarcodeScanned += delegate (object? sender, BarcodeScannedEventArgs args) {
                if (args.Image is not null) {
                    var thumbnail = UsbBarCodeReader.GenerateThumbnail(args.Image);
                    if (thumbnail is not null) {
                        BitmapQueue.Enqueue(thumbnail);
                    }
                }
            };
            var bindCamera = await _usbBarCodeReader.BindCamera(SelectCameraInfo);
            if (bindCamera) {
                var (key, value) = await _usbBarCodeReader.Start();
                if (!key) {
                    UsbCameraSettingsMessageQueue.Enqueue(value);
                }
                else {
                    //设置指定分辨率
                    await _usbBarCodeReader.SetUsbCameraParameter(new Dictionary<UsbCameraParameter, object>()
                    {
                        { UsbCameraParameter.Resolution, CameraResolution.Size }
                    });
                }
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