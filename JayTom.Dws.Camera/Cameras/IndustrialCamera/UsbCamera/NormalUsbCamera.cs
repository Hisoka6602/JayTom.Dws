using System;
using System.Linq;
using System.Text;
using System.Drawing;
using JayTom.Dws.Ocr;
using System.Windows;
using MvCodeReaderSDKNet;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using Point = System.Drawing.Point;
using System.Collections.Concurrent;
using JayTom.Dws.Camera.FilterContainer;
using static MVIDCodeReaderNet.MVIDCodeReader;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.UsbCamera {

    public class NormalUsbCamera : IIndustrialCamera {
        private UsbBarCodeReader? _usbBarCodeReader;
        private SemaphoreSlim _semaphoreSlim = new(1, 1);
        private SemaphoreSlim _drawSlim = new(1);
        private SemaphoreSlim _takePhotoSlim = new(1);
        private SemaphoreSlim _barCodeSlim = new(1);
        private SemaphoreSlim _readImageSlim = new(1);

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        //过滤器
        private readonly BarCodeFilterContainer _barCodeFilterContainer = new();

        public NormalUsbCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.IndustrialCamera;
        }

        public NormalUsbCamera() {
        }

        public async void Dispose() {
            await Task.Yield();
            if (_usbBarCodeReader is not null) {
                _usbBarCodeReader.Dispose();
                _usbBarCodeReader = null;
                OnCameraDisconnected(new CameraConnectionEventArgs() {
                    CameraInfo = this.Info
                });
                OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                    CameraInfo = this.Info
                });
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"设备未初始化")
                });
            }
        }

        public CameraInfo? Info { get; private set; }
        public SdkType SdkType => SdkType.IndustrialCameraSdk;
        public string SdkName => "Dynamsoft";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.ScannerCamera;

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            _devInfo.Clear();
            var cameraInfos = new List<CameraInfo>();
            var usbCameraInfos = UsbBarCodeReader.EnumerateCameras();
            foreach (var cameraInfo in usbCameraInfos.Select(usbCameraInfo => new CameraInfo() {
                Brand = usbCameraInfo.CameraManufacturer ?? string.Empty,
                Model = usbCameraInfo.CameraModel ?? string.Empty,
                Version = usbCameraInfo.CameraVersion ?? string.Empty,
                SerialNumber = usbCameraInfo.CameraSerialNumber ?? string.Empty, //还有一个设备序列号nDeviceNumber不想知道是干吗用的
                Name = usbCameraInfo.CameraName ?? string.Empty,
                Type = CameraType.IndustrialCamera,
                ConnectionType = CameraConnectionType.Usb,
                Id = usbCameraInfo.CameraId ?? 0,
                //如果是海康的工业相机则支持
            })) {
                if (cameraInfo.Brand.Equals("Microsoft")) {
                    _devInfo.AddOrUpdate(cameraInfo.SerialNumber, cameraInfo, (k, v) => cameraInfo);
                    cameraInfos.Add(cameraInfo);
                }
            }
            return cameraInfos;
        }

        public event EventHandler<CameraExceptionEventArgs>? CameraExceptionOccurred;

        public event EventHandler<CameraConnectionEventArgs>? CameraDisconnected;

        public event EventHandler<CameraInitializedEventArgs>? CameraInitialized;

        public event EventHandler<CameraStartedEventArgs>? CameraStarted;

        public event EventHandler<CameraStoppedEventArgs>? CameraStopped;

        public event EventHandler<CameraUnregisteredEventArgs>? CameraUnregistered;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        public async Task<KeyValuePair<bool, string>> Initialize(object param) {
            await Task.Yield();
            if (_usbBarCodeReader != null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }

            if (param is CameraInfo cameraInfo) {
                this.Info = cameraInfo;
                //取出对应Id
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    cameraInfo.Id = devInfo.Id;
                    if (cameraInfo.Id < 0) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("设备Id不存在!")
                        });
                        return new KeyValuePair<bool, string>(false, "设备Id不存在!");
                    }

                    _usbBarCodeReader ??= new UsbBarCodeReader();
                    var bindCamera = await _usbBarCodeReader.BindCamera(new UsbCameraInfo() {
                        CameraSerialNumber = this.Info.SerialNumber,
                        CameraName = this.Info.Name,
                        CameraId = this.Info.Id,
                        CameraVersion = this.Info.Version,
                        CameraManufacturer = this.Info.Brand,
                        CameraModel = this.Info.Model,
                    });
                    if (!bindCamera) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("绑定相机失败!")
                        });
                        return new KeyValuePair<bool, string>(false, "绑定相机失败!");
                    }
                    else {
                        _usbBarCodeReader.BarcodeScanned += async delegate (object? sender, BarcodeScannedEventArgs args) {
                            try {
                                if (args?.Image is not null) {
                                    var scanTime = DateTime.Now;
                                    var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
                                    Bitmap? generateThumbnail;
                                    generateThumbnail = GenerateThumbnail(args.Image);
                                    List<Point>? points = null;
                                    if (generateThumbnail is not null) {
                                        //设置图像边框
                                        using var g = Graphics.FromImage(generateThumbnail);

                                        foreach (var barcodeInfo in args?.BarCodes ?? new List<BarcodeInfo>()) {
                                            points = barcodeInfo.BarcodeRegion;
                                            if (points is not null && points.Count == 4 &&
                                                generateThumbnail is not null &&
                                                args?.Image is { Width: > 0, Height: > 0 }) {
                                                var stPointList = new Point[4];
                                                for (var i = 0; i < 4; i++) {
                                                    stPointList[i].X = (int)(points[i].X *
                                                                             ((float)generateThumbnail.Width / args.Image.Width));
                                                    stPointList[i].Y = (int)(points[i].Y *
                                                                             ((float)generateThumbnail.Height / args.Image.Height));
                                                }
                                                g.DrawPolygon(new System.Drawing.Pen(BarcodeBorderColor, BarcodeBorderSize), stPointList);
                                            }
                                        }
                                    }

                                    foreach (var barcodeInfo in from barcodeInfo in args?.BarCodes ?? new List<BarcodeInfo>()
                                                                let validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                                                                    BarCode = barcodeInfo.Barcode ?? "NoRead",
                                                                    ScanTime = DateTime.Now
                                                                })
                                                                where validateData
                                                                select barcodeInfo) {
                                        OnBarcodeRead(new BarcodeReadEventArgs() {
                                            Barcode = barcodeInfo.Barcode ?? "NoRead",
                                            CameraSerialNumber = args?.CameraSerialNumber ?? this.Info.SerialNumber,
                                            Image = args?.Image,
                                            ScanTime = scanTime,
                                            Timestamp = timestamp,
                                            ThumbImage = generateThumbnail,
                                            AreaCoords = points
                                        });
                                    }

                                    if (IsRealtimeImageEnabled) {
                                        OnRealtimeImage(new RealtimeImageEventArgs() {
                                            ThumbImage = generateThumbnail,
                                            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                                        });
                                    }
                                }
                            }
                            catch (Exception e) {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = e
                                });
                            }

                            //扫码回调
                            //获取缩略图
                            //画框
                        };
                    }
                    OnCameraInitialized(new CameraInitializedEventArgs() {
                        CameraInfo = this.Info
                    });
                    return new KeyValuePair<bool, string>(true, "初始化成功");
                }
                else {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("设备不存在或已离线,请重新枚举!")
                    });
                    return new KeyValuePair<bool, string>(false, "设备不存在或已离线,请重新枚举!");
                }
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("初始化传参类型错误!")
                });
                return new KeyValuePair<bool, string>(false, "初始化传参类型错误!");
            }
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            await Task.Yield();
            if (_usbBarCodeReader is not null) {
                var (key, value) = await _usbBarCodeReader.Start();
                if (!key) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(value)
                    });
                }
                else {
                    OnCameraStarted(new CameraStartedEventArgs() {
                        CameraInfo = this.Info
                    });
                }
                return new KeyValuePair<bool, string>(key, value);
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"设备未初始化")
                });
                return new KeyValuePair<bool, string>(false, $"设备未初始化");
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await Task.Yield();
            if (_usbBarCodeReader is not null) {
                var (key, value) = await _usbBarCodeReader.Stop();
                if (!key) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(value)
                    });
                }
                return new KeyValuePair<bool, string>(key, value);
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"设备未初始化")
                });
                return new KeyValuePair<bool, string>(false, $"设备未初始化");
            }
        }

        public async void SetParameters(Dictionary<string, object> parameters) {
            //设置参数
            if (_usbBarCodeReader is not null) {
                foreach (var parameter in parameters) {
                    switch (parameter.Key) {
                        case "BarcodeReaderParameter": {
                                //读码器参数
                                var (key, value) = await _usbBarCodeReader.SetBarcodeReaderParameter(
                                    (Dictionary<BarcodeReaderParameter, object>)parameter.Value);
                                if (!key) {
                                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                        Exception = new Exception(value)
                                    });
                                }

                                break;
                            }
                        case "UsbCameraParameter": {
                                //相机参数
                                var (key, value) = await _usbBarCodeReader.SetUsbCameraParameter(
                                    (Dictionary<UsbCameraParameter, object>)parameter.Value);
                                if (!key) {
                                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                        Exception = new Exception(value)
                                    });
                                }

                                break;
                            }
                    }
                }
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("设备未初始化")
                });
            }
        }

        public bool IsRealtimeImageEnabled { get; private set; }

        public void StartRealTimeImage() {
            IsRealtimeImageEnabled = true;
        }

        public void StopRealTimeImage() {
            IsRealtimeImageEnabled = false;
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public int TakePhotoDelay { get; set; }
        public IOcr? Ocr { get; set; }
        public int BarcodeBorderSize { get; set; } = 5;
        public Color BarcodeBorderColor { get; set; } = System.Drawing.Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; }

        public event EventHandler<BarcodeReadEventArgs>? BarcodeRead;

        public event EventHandler<OcrResult>? OcrContentRecognized;

        public void SetScanCodeFilterParams(ScanCodeFilterParams @params) {
            _barCodeFilterContainer.Pattern = @params.RegularExpression;
            _barCodeFilterContainer.MaxSize = @params.DuplicateBarcodeFilterCount;
            _barCodeFilterContainer.ExpirationTime = TimeSpan.FromMilliseconds(@params.ScanInterval);
        }

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnCameraInitialized(CameraInitializedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(CameraConnectionEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnCameraStarted(CameraStartedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual async void OnCameraStopped(CameraStoppedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Paused;
            CameraStopped?.Invoke(this, e);
        }

        protected virtual async void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Uninitialized;
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual async void OnRealtimeImage(RealtimeImageEventArgs e) {
            await Task.Yield();
            RealtimeImage?.Invoke(this, e);
        }

        public unsafe Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
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

        protected virtual async void OnBarcodeRead(BarcodeReadEventArgs e) {
            await Task.Yield();
            BarcodeRead?.Invoke(this, e);
        }
    }
}