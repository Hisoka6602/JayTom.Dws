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
using JayTom.Dws.Camera.BarCodeReader;
using JayTom.Dws.Camera.FilterContainer;
using static MVIDCodeReaderNet.MVIDCodeReader;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.UsbCamera {

    public class NormalUsbCamera : IIndustrialCamera {
        private UsbBarCodeReader? _usbBarCodeReader;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly SemaphoreSlim _drawSlim = new(1);
        private readonly SemaphoreSlim _takePhotoSlim = new(1);
        private readonly SemaphoreSlim _barCodeSlim = new(1);
        private readonly SemaphoreSlim _readImageSlim = new(1);
        private long _frameNo = 0;

        /// <summary>
        /// 设备列表
        /// </summary>
        private static readonly ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

        public NormalUsbCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.IndustrialCamera;
        }

        public NormalUsbCamera() {
        }

        public void Dispose() {
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
                Id = usbCameraInfo.CameraIndex ?? 0,
                SupportedBindingType =
                    CameraBindingType.ScannerCamera
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
                        CameraIndex = checked((int)this.Info.Id),
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
                        _usbBarCodeReader.BarcodeScanned += delegate (object? sender, BarcodeScannedEventArgs args) {
                            HandleBarcodeScanned(args);
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

        /// <summary>
        /// 处理 USB 相机读码结果并为各消费者分配独立图像。
        /// </summary>
        private void HandleBarcodeScanned(BarcodeScannedEventArgs args) {
            var image = args.Image;
            if (image is null) {
                return;
            }

            try {
                var scanTime = DateTime.Now;
                var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
                var results = new List<(string Barcode, List<Point>? AreaCoords)>();
                foreach (var barcodeInfo in args.BarCodes ?? []) {
                    var validation = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo {
                        BarCode = barcodeInfo.Barcode ?? "NoRead",
                        ScanTime = scanTime
                    });
                    if (validation.IsValidationPassed ||
                        !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)) {
                        results.Add((
                            _barCodeFilterContainer.RegexReplace(
                                (validation.IsValidationPassed
                                    ? barcodeInfo.Barcode
                                    : _barCodeFilterContainer.FilterOutContent) ?? "NoRead"),
                            barcodeInfo.BarcodeRegion));
                    }
                }

                var barcodeConsumerCount = BarcodeRead is null ? 0 : results.Count;
                var realtimeConsumer = IsRealtimeImageEnabled && RealtimeImage is not null;
                if (barcodeConsumerCount == 0 && !realtimeConsumer) {
                    image.Dispose();
                    return;
                }

                var thumbnail = GenerateThumbnail(image);
                if (thumbnail is null) {
                    image.Dispose();
                    return;
                }
                if (IsShowBarcodeBorder && results.Count > 0) {
                    using var graphics = Graphics.FromImage(thumbnail);
                    using var pen = new Pen(BarcodeBorderColor, BarcodeBorderSize);
                    foreach (var result in results) {
                        if (result.AreaCoords is not { Count: 4 }) {
                            continue;
                        }
                        var points = new Point[result.AreaCoords.Count];
                        for (var index = 0; index < points.Length; index++) {
                            points[index] = new Point(
                                result.AreaCoords[index].X * thumbnail.Width / Math.Max(1, image.Width),
                                result.AreaCoords[index].Y * thumbnail.Height / Math.Max(1, image.Height));
                        }
                        graphics.DrawPolygon(pen, points);
                    }
                }

                var realtimeThumbnail = realtimeConsumer && barcodeConsumerCount > 0
                    ? new Bitmap(thumbnail)
                    : thumbnail;
                for (var index = 0; index < barcodeConsumerCount; index++) {
                    var isLast = index == barcodeConsumerCount - 1;
                    OnBarcodeRead(new BarcodeReadEventArgs {
                        Barcode = results[index].Barcode,
                        CameraSerialNumber = args.CameraSerialNumber ?? Info?.SerialNumber ?? string.Empty,
                        Image = isLast ? image : new Bitmap(image),
                        ScanTime = scanTime,
                        Timestamp = timestamp,
                        ThumbImage = isLast ? thumbnail : new Bitmap(thumbnail),
                        AreaCoords = results[index].AreaCoords,
                        FrameNo = _frameNo
                    });
                }
                if (barcodeConsumerCount == 0) {
                    image.Dispose();
                }
                if (realtimeConsumer) {
                    OnRealtimeImage(new RealtimeImageEventArgs {
                        ThumbImage = realtimeThumbnail,
                        Timestamp = timestamp
                    });
                }
                _frameNo++;
            }
            catch (Exception exception) {
                image.Dispose();
                OnCameraExceptionOccurred(new CameraExceptionEventArgs { Exception = exception });
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
                        CameraInfo = this.Info,
                        Camera = this
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

        public void SetParameters(Dictionary<string, object> parameters) {
            //设置参数
            if (_usbBarCodeReader is not null) {
                foreach (var parameter in parameters) {
                    switch (parameter.Key) {
                        case "BarcodeReaderParameter": {
                                //读码器参数
                                var (key, value) = _usbBarCodeReader.ApplyBarcodeReaderSettingsAsync(
                                    (BarcodeReaderSettings)parameter.Value).GetAwaiter().GetResult();
                                if (!key) {
                                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                        Exception = new Exception(value)
                                    });
                                }

                                break;
                            }
                        case "UsbCameraParameter": {
                                //相机参数
                                var (key, value) = _usbBarCodeReader.SetUsbCameraParameter(
                                    (Dictionary<UsbCameraParameter, object>)parameter.Value).GetAwaiter().GetResult();
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

        public Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, TimeSpan delay, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public int TakePhotoDelay { get; set; }
        public IOcr? Ocr { get; set; }
        public int BarcodeBorderSize { get; set; } = 5;
        public Color BarcodeBorderColor { get; set; } = System.Drawing.Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; }

        public event EventHandler<BarcodeReadEventArgs>? BarcodeRead;

        public event EventHandler<OcrResult>? OcrContentRecognized;

        public event EventHandler<BarcodeReadEventArgs>? FilteredBarcodeReturned;

        public void SetScanCodeFilterParams(ScanCodeFilterParams @params) {
            _barCodeFilterContainer = new BarCodeFilterContainer {
                Pattern = @params.RegularExpression,
                MaxSize = @params.DuplicateBarcodeFilterCount,
                ExpirationTime = TimeSpan.FromMilliseconds(@params.ScanInterval),
                FilterOutContent = @params.FilterOutContent,
                BarCodeFilterMode = @params.BarCodeFilterMode,
                CustomRegularExpressionItems = @params.CustomRegularExpressionItems,
                IsUseCustomRegexReplacement = @params.IsUseCustomRegexReplacement,
                IsUseFilteredBarcodeTypes = @params.IsUseFilteredBarcodeTypes,
                CustomRegexReplacementItems = @params.CustomRegexReplacementItems
            };

            BarCodeFilterContainer.ResetFilter();
        }

        protected virtual void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual void OnCameraDisconnected(CameraConnectionEventArgs e) {
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e) {
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual void OnCameraStopped(CameraStoppedEventArgs e) {
            Status = CameraStatus.Paused;
            CameraStopped?.Invoke(this, e);
        }

        protected virtual void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            Status = CameraStatus.Uninitialized;
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual void OnRealtimeImage(RealtimeImageEventArgs e) {
            var handler = RealtimeImage;
            if (handler is null) {
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        protected virtual void OnBarcodeRead(BarcodeReadEventArgs e) {
            var handler = BarcodeRead;
            if (handler is null) {
                e.Image?.Dispose();
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }
    }
}
