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

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Wayzim {

    public class WayzimIndustrialCamera : IIndustrialCamera {
        private SemaphoreSlim _semaphoreSlim = new(1, 1);
        private SemaphoreSlim _takeSlim = new(1, 1);
        private long _frameNo = 0;

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

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

        public async void Dispose() {
            if (Status != CameraStatus.Uninitialized &&
                Status != CameraStatus.Disconnected) {
                await Stop();
                _cancellationTokenSource?.Cancel();
                await Task.Delay(200);
                if (_imageCallbackThread != null) {
                    await _imageCallbackThread;
                    _imageCallbackThread?.Dispose();
                    _imageCallbackThread = null;
                }
                this.Info = null;
            }

            OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                CameraInfo = this.Info
            });
            System.GC.Collect();
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
                        Id = cam.Cameras[i].CameraId,
                        IpAddress = ICAMAPI.ICAM_BytesToString(cam.Cameras[i].CamIp),
                        IsAvailable = true,
                        Model = "IndustrialCamera",
                        Name = ICAMAPI.ICAM_BytesToString(cam.Cameras[i].CamFriendlyName),
                        SerialNumber = ICAMAPI.ICAM_BytesToString(cam.Cameras[i].CamSerialNumber),
                        Type = CameraType.IndustrialCamera,
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
                    ICAMAPI.ICAM_SetCamBeScanner(devInfo.Id, BindingType == CameraBindingType.ScannerCamera ? 1 : 0);
                    ICAMAPI.ICAM_StartCamera(devInfo.Id);
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
                _imageCallbackThread = Task.Run(async () => {
                    while (!_cancellationTokenSource.IsCancellationRequested) {
                        var img = new ImageModelCpp();
                        try {
                            await _takeSlim.WaitAsync();
                            await Task.Delay(100);

                            if (this.Info is not null) {
                                var scanTime = DateTime.Now;
                                var localTime = new DateTimeOffset(scanTime).ToLocalTime();
                                var timestamp = localTime.ToUnixTimeMilliseconds();
                                int status = ICAMAPI.ICAM_FetchFrame(this.Info.Id, ref img, 300);
                                if (status == 0) {
                                    var bitmap = await GetBitmapAsync(img);
                                    var thumbnailImage = GenerateThumbnail(bitmap);
                                    if (img.BarcodeCount > 0 && BindingType != CameraBindingType.PanoramaCamera) {
                                        if (IsShowBarcodeBorder && thumbnailImage is not null && thumbnailImage.PixelFormat != PixelFormat.Format8bppIndexed &&
                                            img.CodeModels?.Any() == true) {
                                            //设置图像边框
                                            using var g = Graphics.FromImage(thumbnailImage);

                                            //画框
                                            foreach (var convertPoint in img.CodeModels.Select(ConvertPoint)) {
                                                var imageWidth = img.Width;
                                                var imageHeight = img.Height;
                                                var points = new Point[4];
                                                for (var j = 0; j < 4; ++j) {
                                                    points[j].X = (int)(convertPoint[j].X *
                                                                        ((float)(thumbnailImage.Size.Width) / (imageWidth > 0 ? imageWidth : 1)));
                                                    points[j].Y = (int)(convertPoint[j].Y *
                                                                        ((float)(thumbnailImage.Size.Height) / (imageHeight > 0 ? imageHeight : 1)));
                                                }
                                                g.DrawPolygon(new Pen(BarcodeBorderColor, BarcodeBorderSize), points);
                                            }
                                            if (img.CodeModels?.Any() == true) {
                                                //扫到条码
                                                foreach (var codeInfo in img.CodeModels) {
                                                    //过滤
                                                    var barCode = Encoding.ASCII.GetString(codeInfo.strCode).TrimEnd('\0');
                                                    if (!string.IsNullOrWhiteSpace(barCode)) {
                                                        var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                                                            BarCode = string.IsNullOrWhiteSpace(barCode) ? "NoRead" : barCode,
                                                            ScanTime = scanTime
                                                        });
                                                        if (validateData.IsValidationPassed || !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)) {
                                                            //返回条码

                                                            OnBarcodeRead(new BarcodeTriggeredEventArgs() {
                                                                Timestamp = timestamp,
                                                                Barcode = _barCodeFilterContainer.RegexReplace(validateData.IsValidationPassed ? barCode : _barCodeFilterContainer.FilterOutContent),
                                                                Image = bitmap,
                                                                ThumbImage = (Bitmap?)thumbnailImage,
                                                                CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                                                                ScanTime = scanTime,
                                                                AreaCoords = ConvertPoint(codeInfo),
                                                                FrameNo = _frameNo
                                                            });
                                                        }
                                                    }
                                                }

                                                _frameNo += 1;
                                            }
                                        }
                                    }
                                    if (IsRealtimeImageEnabled) {
                                        OnRealtimeImage(new RealtimeImageEventArgs() {
                                            Timestamp = timestamp,
                                            ThumbImage = (Bitmap?)thumbnailImage,
                                        });
                                    }
                                    await Task.Delay(5);
                                }
                                else {
                                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                        Exception = new Exception($"获取图像失败,状态码:{status}")
                                    });
                                }
                            }
                        }
                        catch (Exception e) {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = e
                            });
                        }
                        finally {
                            ICAMAPI.ICAM_ReleaseFrame(ref img);
                            _takeSlim.Release();
                        }
                    }
                });
            }
            OnCameraStarted(new CameraStartedEventArgs() {
                CameraInfo = this.Info
            });
            return new KeyValuePair<bool, string>(true, "启动成功!");
        }

        public Task<KeyValuePair<bool, string>> Stop() {
            if (Status == CameraStatus.Running &&
                this.Info is not null) {
                var icamStopCamera = ICAMAPI.ICAM_StopCamera(this.Info.Id);
                return Task.FromResult(icamStopCamera == 0 ? new KeyValuePair<bool, string>(true, "停止成功") : new KeyValuePair<bool, string>(true, $"停止失败,状态码:{icamStopCamera}"));
            }
            OnCameraStopped(new CameraStoppedEventArgs() {
                CameraInfo = this.Info
            });

            return Task.FromResult(new KeyValuePair<bool, string>(true, "设备未运行"));
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

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            Task.Run(async () => {
                await Task.Delay(TakePhotoDelay, cancellation);
                if (Status == CameraStatus.Running &&
                    this.Info is not null) {
                    try {
                        Bitmap? bitmap = null;
                        Bitmap? thumbnailImage = null;
                        var img = new ImageModelCpp();
                        await _takeSlim.WaitAsync(cancellation);
                        int status = ICAMAPI.ICAM_FetchFrame(this.Info.Id, ref img, 300);
                        if (status == 0) {
                            bitmap = await GetBitmapAsync(img);
                            thumbnailImage = GenerateThumbnail(bitmap);
                        }
                        else {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception($"截图失败,状态码:{status}")
                            });
                        }

                        OnPhotoTaken(new PhotoTakenEventArgs {
                            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                            CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                            Image = bitmap,
                            PhotoTime = DateTime.Now,
                            ThumbImage = thumbnailImage,
                            Barcode = barcode,
                            BarcodeTimestamp = barcodeTimestamp
                        });
                    }
                    catch (Exception e) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"截图失败:截取一帧图片异常,{e}")
                        });
                    }
                    finally {
                        _takeSlim.Release();
                    }
                }
            }, cancellation);
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            Task.Run(async () => {
                await Task.Delay(delay, cancellation);
                await TakePhotoAsync(barcode, barcodeTimestamp, cancellation);
            }, cancellation);
            return Task.CompletedTask;
        }

        public int TakePhotoDelay { get; set; }

        /// <summary>
        /// Ocr
        /// </summary>
        public IOcr Ocr { get; set; }

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

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnCameraInitialized(CameraInitializedEventArgs e) {
            await Task.Yield();
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

        private async Task<Bitmap?> GetBitmapAsync(ImageModelCpp img) {
            Bitmap? bitmap = null;
            try {
                await _semaphoreSlim.WaitAsync();
                var imageBytes = new byte[img.DataLen];
                Marshal.Copy(img.ImageData, imageBytes, 0, (int)img.DataLen);
                if (img.Type == ImageType.IMAGE_MONO) {
                    bitmap = NewBitmapFromGrayData(imageBytes, img.Width, img.Height);
                }
                else if (img.Type == ImageType.IMAGE_RGB24) {
                    bitmap = BytesToImg(imageBytes, img.Width, img.Height);
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
            finally {
                _semaphoreSlim.Release();
            }
            return bitmap;
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

        protected virtual async void OnRealtimeImage(RealtimeImageEventArgs e) {
            await Task.Yield();
            RealtimeImage?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeRead(BarcodeReadEventArgs e) {
            await Task.Yield();
            BarcodeRead?.Invoke(this, e);
        }

        protected virtual async void OnPhotoTaken(PhotoTakenEventArgs e) {
            await Task.Yield();
            PhotoTaken?.Invoke(this, e);
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
            Status = CameraStatus.Disconnected;
            CameraUnregistered?.Invoke(this, e);
        }
    }
}