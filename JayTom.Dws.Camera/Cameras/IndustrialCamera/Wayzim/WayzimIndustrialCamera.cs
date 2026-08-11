using System;
using CamSDK;
using System.Linq;
using System.Text;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using MVIDCodeReaderNet;
using MvCodeReaderSDKNet;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.FilterContainer;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Wayzim {

    public class WayzimIndustrialCamera : IIndustrialCamera {
        private readonly SemaphoreSlim _takeSlim = new(1, 1);
        private long _frameNo = 0;

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

        /// <summary>
        /// 设备列表
        /// </summary>
        private static readonly ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        /// <summary>
        /// 图像回调线程
        /// </summary>
        private Task? _imageCallbackThread;

        private CancellationTokenSource? _cancellationTokenSource;

        public WayzimIndustrialCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.IndustrialCamera;
            var tryGetValue = _devInfo.TryGetValue(this.Info.SerialNumber, out var devInfo);
            if (tryGetValue && devInfo is not null) {
                this.Info.Id = devInfo.Id;
                _devInfo.AddOrUpdate(this.Info.SerialNumber, this.Info, (k, v) => this.Info);
            }
        }

        public WayzimIndustrialCamera() {
        }

        public void Dispose() {
            var cameraInfo = Info;
            if (Status != CameraStatus.Uninitialized &&
                Status != CameraStatus.Disconnected) {
                Stop().GetAwaiter().GetResult();
                _cancellationTokenSource?.Cancel();
                if (_imageCallbackThread != null) {
                    try {
                        _imageCallbackThread.GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException) {
                    }
                    _imageCallbackThread.Dispose();
                    _imageCallbackThread = null;
                }
            }

            OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                CameraInfo = cameraInfo
            });
            Info = null;
        }

        public CameraInfo? Info { get; private set; }
        public SdkType SdkType { get; private set; } = SdkType.IndustrialCameraSdk;
        public string SdkName => "WayzimCodeReader.dll";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Disconnected;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.ScannerCamera;

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            var cameraInfos = new List<CameraInfo>();
            var cam = new ICAM_CameraInfoCpp();
            var ret = ICAMAPI.ICAM_EnumerateDevices(ref cam);
            if (ret == 0) {
                for (int i = 0; i < cam.CameraCount; i++) {
                    var cameraInfo = new CameraInfo() {
                        Brand = "Wayzim",
                        ConnectionType = CameraConnectionType.Ethernet,
                        Id = cam.Cameras[i].CameraIndex,
                        IpAddress = ICAMAPI.ICAM_BytesToString(cam.Cameras[i].CamIp),
                        IsAvailable = true,
                        Model = "IndustrialCamera",
                        Name = ICAMAPI.ICAM_BytesToString(cam.Cameras[i].CamFriendlyName),
                        SerialNumber = ICAMAPI.ICAM_BytesToString(cam.Cameras[i].CamSerialNumber),
                        Type = CameraType.IndustrialCamera,
                        SupportedBindingType = CameraBindingType.ScannerCamera | CameraBindingType.PanoramaCamera
                    };
                    _devInfo.AddOrUpdate(cameraInfo.SerialNumber, cameraInfo, (k, v) => cameraInfo);
                    cameraInfos.Add(cameraInfo);
                }
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"枚举失败,状态码:{ret}")
                });
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
            //ICAMAPI.ICAM_RegisterCameraStateCallback(callback, IntPtr.Zero);
            if (Status != CameraStatus.Disconnected &&
                Status != CameraStatus.Uninitialized) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }
            if (param is CameraInfo cameraInfo) {
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    this.Info = devInfo;
                    ICAMAPI.ICAM_SetCamBeScanner(checked((int)devInfo.Id), BindingType == CameraBindingType.ScannerCamera ? 1 : 0);
                    ICAMAPI.ICAM_StartCamera(checked((int)devInfo.Id));
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

                //注册相机状态回调函数
                //判断是否扫码相机
                //创建取图线程
                //注册扫码回调
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
            if (Status != CameraStatus.Initialized || this.Info is null) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("设备未初始化!")
                });
                return new KeyValuePair<bool, string>(false, "设备未初始化!");
            }
            if (BindingType == CameraBindingType.ScannerCamera && _imageCallbackThread is null) {
                _cancellationTokenSource = new CancellationTokenSource();
                _imageCallbackThread = Task.Run(
                    () => ProcessFramesAsync(_cancellationTokenSource.Token),
                    _cancellationTokenSource.Token);
            }
            OnCameraStarted(new CameraStartedEventArgs() {
                CameraInfo = this.Info,
                Camera = this
            });
            return new KeyValuePair<bool, string>(true, "启动成功!");
        }

        /// <summary>
        /// 持续获取相机最新帧，并限制失败日志频率。
        /// </summary>
        private async Task ProcessFramesAsync(CancellationToken token) {
            var lastFailureReport = DateTime.MinValue;
            while (!token.IsCancellationRequested) {
                var image = new ImageModelCpp();
                var lockTaken = false;
                try {
                    await _takeSlim.WaitAsync(token).ConfigureAwait(false);
                    lockTaken = true;
                    var cameraInfo = Info;
                    if (cameraInfo is null) {
                        return;
                    }

                    var status = ICAMAPI.ICAM_FetchFrame(checked((int)cameraInfo.Id), ref image, 300);
                    if (status != 0) {
                        if (DateTime.Now - lastFailureReport >= TimeSpan.FromSeconds(5)) {
                            lastFailureReport = DateTime.Now;
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                                Exception = new Exception($"获取图像失败,状态码:{status}")
                            });
                        }
                        continue;
                    }

                    var bitmap = GetBitmap(image);
                    if (bitmap is not null) {
                        PublishFrame(image, bitmap, cameraInfo.SerialNumber);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) {
                    return;
                }
                catch (Exception exception) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs { Exception = exception });
                }
                finally {
                    ICAMAPI.ICAM_ReleaseFrame(ref image);
                    if (lockTaken) {
                        _takeSlim.Release();
                    }
                }
            }
        }

        /// <summary>
        /// 过滤并发布当前帧，确保每个事件消费者拥有独立图像。
        /// </summary>
        private void PublishFrame(ImageModelCpp image, Bitmap bitmap, string serialNumber) {
            var scanTime = DateTime.Now;
            var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
            var codeModels = BindingType == CameraBindingType.PanoramaCamera
                ? []
                : image.CodeModels ?? [];
            var results = new List<(string Barcode, List<Point> AreaCoords)>(codeModels.Length);
            foreach (var codeInfo in codeModels) {
                var barcode = Encoding.ASCII.GetString(codeInfo.strCode).TrimEnd('\0');
                if (string.IsNullOrWhiteSpace(barcode)) {
                    continue;
                }

                var validation = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo {
                    BarCode = barcode,
                    ScanTime = scanTime
                });
                if (validation.IsValidationPassed ||
                    !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)) {
                    results.Add((
                        _barCodeFilterContainer.RegexReplace(
                            validation.IsValidationPassed
                                ? barcode
                                : _barCodeFilterContainer.FilterOutContent),
                        ConvertPoint(codeInfo)));
                }
            }

            var barcodeConsumerCount = BarcodeRead is null ? 0 : results.Count;
            var realtimeConsumerCount = IsRealtimeImageEnabled && RealtimeImage is not null ? 1 : 0;
            if (barcodeConsumerCount + realtimeConsumerCount == 0) {
                bitmap.Dispose();
                return;
            }

            var thumbnail = GenerateThumbnail(bitmap);
            if (thumbnail is null) {
                bitmap.Dispose();
                return;
            }

            if (IsShowBarcodeBorder && results.Count > 0) {
                using var graphics = Graphics.FromImage(thumbnail);
                using var pen = new Pen(BarcodeBorderColor, BarcodeBorderSize);
                foreach (var result in results) {
                    var points = new Point[result.AreaCoords.Count];
                    for (var index = 0; index < points.Length; index++) {
                        points[index] = new Point(
                            result.AreaCoords[index].X * thumbnail.Width / Math.Max(1, image.Width),
                            result.AreaCoords[index].Y * thumbnail.Height / Math.Max(1, image.Height));
                    }
                    if (points.Length >= 3) {
                        graphics.DrawPolygon(pen, points);
                    }
                }
            }

            var realtimeThumbnail = realtimeConsumerCount == 1 && barcodeConsumerCount > 0
                ? new Bitmap(thumbnail)
                : thumbnail;
            for (var index = 0; index < barcodeConsumerCount; index++) {
                var isLast = index == barcodeConsumerCount - 1;
                OnBarcodeRead(new BarcodeTriggeredEventArgs {
                    Timestamp = timestamp,
                    Barcode = results[index].Barcode,
                    Image = isLast ? bitmap : new Bitmap(bitmap),
                    ThumbImage = isLast ? thumbnail : new Bitmap(thumbnail),
                    CameraSerialNumber = serialNumber,
                    ScanTime = scanTime,
                    AreaCoords = results[index].AreaCoords,
                    FrameNo = _frameNo
                });
            }

            if (barcodeConsumerCount == 0) {
                bitmap.Dispose();
            }
            if (realtimeConsumerCount == 1) {
                OnRealtimeImage(new RealtimeImageEventArgs {
                    Timestamp = timestamp,
                    ThumbImage = realtimeThumbnail
                });
            }
            _frameNo++;
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            if (Status == CameraStatus.Running &&
                this.Info is not null) {
                _cancellationTokenSource?.Cancel();
                if (_imageCallbackThread is not null) {
                    try {
                        await _imageCallbackThread.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                    }
                    _imageCallbackThread.Dispose();
                    _imageCallbackThread = null;
                }
                var icamStopCamera = ICAMAPI.ICAM_StopCamera(checked((int)this.Info.Id));
                if (icamStopCamera == 0) {
                    OnCameraStopped(new CameraStoppedEventArgs {
                        CameraInfo = Info
                    });
                    return new KeyValuePair<bool, string>(true, "停止成功");
                }
                return new KeyValuePair<bool, string>(false, $"停止失败,状态码:{icamStopCamera}");
            }
            OnCameraStopped(new CameraStoppedEventArgs() {
                CameraInfo = this.Info
            });

            return new KeyValuePair<bool, string>(true, "设备未运行");
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public bool IsRealtimeImageEnabled { get; private set; } = false;

        public void StartRealTimeImage() {
            IsRealtimeImageEnabled = true;
        }

        public void StopRealTimeImage() {
            IsRealtimeImageEnabled = false;
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public async Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation = default) {
            await Task.Delay(TakePhotoDelay, cancellation);
            await CapturePhotoAsync(barcode, packageTimestampMilliseconds, cancellation);
        }

        public async Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, TimeSpan delay, CancellationToken cancellation = default) {
            await Task.Delay(delay, cancellation);
            await CapturePhotoAsync(barcode, packageTimestampMilliseconds, cancellation);
        }

        private async Task CapturePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation) {
            if (Status != CameraStatus.Running || Info is null) {
                return;
            }

            var lockTaken = false;
            var image = new ImageModelCpp();
            try {
                Bitmap? bitmap = null;
                Bitmap? thumbnailImage = null;
                await _takeSlim.WaitAsync(cancellation);
                lockTaken = true;
                var status = ICAMAPI.ICAM_FetchFrame(checked((int)Info.Id), ref image, 300);
                if (status == 0) {
                    bitmap = GetBitmap(image);
                    thumbnailImage = GenerateThumbnail(bitmap);
                }
                else {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                        Exception = new Exception($"截图失败,状态码:{status}")
                    });
                }

                OnPhotoTaken(new PhotoTakenEventArgs {
                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    CameraSerialNumber = Info.SerialNumber,
                    Image = bitmap,
                    PhotoTime = DateTime.Now,
                    ThumbImage = thumbnailImage,
                    Barcode = barcode,
                    PackageTimestampMilliseconds = packageTimestampMilliseconds
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = new Exception($"截图失败:截取一帧图片异常,{e}")
                });
            }
            finally {
                ICAMAPI.ICAM_ReleaseFrame(ref image);
                if (lockTaken) {
                    _takeSlim.Release();
                }
            }
        }

        public int TakePhotoDelay { get; set; }

        /// <summary>
        /// Ocr
        /// </summary>
        public IOcr? Ocr { get; set; }

        public int BarcodeBorderSize { get; set; } = 5;
        public System.Drawing.Color BarcodeBorderColor { get; set; } = System.Drawing.Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;

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

        private List<Point> ConvertPoint(BarCodeModelCpp info) {
            var points = new List<Point>();
            if (info.stCornerPt.Length == 8) {
                for (var i = 0; i < info.stCornerPt.Length; i += 2) {
                    var x = info.stCornerPt[i];
                    var y = info.stCornerPt[i + 1];
                    points.Add(new Point(x, y));
                }

                return SortPointsInCounterClockwiseOrder(points);
            }

            return points;
        }

        private List<Point> SortPointsInCounterClockwiseOrder(List<Point> points) {
            // 计算多边形的中心点
            var center = new Point(points.Sum(p => p.X) / points.Count, points.Sum(p => p.Y) / points.Count);

            // 根据相对于中心点的极角排序点
            points.Sort((p1, p2) => {
                double angle1 = Math.Atan2(p1.Y - center.Y, p1.X - center.X);
                double angle2 = Math.Atan2(p2.Y - center.Y, p2.X - center.X);
                return angle1.CompareTo(angle2);
            });

            return points;
        }

        private Bitmap BytesToImg(byte[] bytes, int w, int h) {
            var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Marshal.Copy(bytes, 0, bmpData.Scan0, bytes.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        /// <summary>
        /// 将灰度图数据转换成新的bitmap图像
        /// </summary>
        /// <param name="imgData"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public Bitmap NewBitmapFromGrayData(byte[] imgData, int w, int h) {
            var bmp = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            var grayPal = bmp.Palette;
            for (int Y = 0; Y < grayPal.Entries.Length; Y++)
                grayPal.Entries[Y] = Color.FromArgb(255, Y, Y, Y);
            bmp.Palette = grayPal;
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, w, h),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            Marshal.Copy(imgData, 0, bmpData.Scan0, imgData.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private Bitmap? GetBitmap(ImageModelCpp img) {
            try {
                var pixelFormat = img.Type switch {
                    ImageType.IMAGE_MONO => PixelFormat.Format8bppIndexed,
                    ImageType.IMAGE_RGB24 => PixelFormat.Format24bppRgb,
                    _ => PixelFormat.Undefined
                };
                if (pixelFormat == PixelFormat.Undefined) {
                    return null;
                }

                var bytesPerPixel = pixelFormat == PixelFormat.Format8bppIndexed ? 1 : 3;
                return CameraImageProcessing.CopyPackedFrame(
                    img.ImageData,
                    checked((int)img.DataLen),
                    img.Width,
                    img.Height,
                    pixelFormat,
                    checked(img.Width * bytesPerPixel));
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
                return null;
            }
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        protected virtual void OnRealtimeImage(RealtimeImageEventArgs e) {
            var handler = RealtimeImage;
            if (handler is null) {
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
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

        protected virtual void OnPhotoTaken(PhotoTakenEventArgs e) {
            var handler = PhotoTaken;
            if (handler is null) {
                e.Image?.Dispose();
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
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
            Status = CameraStatus.Disconnected;
            CameraUnregistered?.Invoke(this, e);
        }
    }
}
