using System;
using NetSDKCS;
using System.Net;
using System.Linq;
using System.Text;
using Dynamsoft.DBR;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using Image = System.Drawing.Image;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.BarCodeReader;
using JayTom.Dws.Camera.FilterContainer;
using static System.Net.Mime.MediaTypeNames;

namespace JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech {

    public class DaHuatechSecurityCamera : ISecurityCamera {
        private BaseDaHuatech _baseDaHuatech = BaseDaHuatech.CreateInstance();
        private SemaphoreSlim _snapRevPhotoSlim = new(1);
        private ConcurrentQueue<CameraImageMessageInfo> _imageMessageQueue = new();
        private SemaphoreSlim _takePhotoSlim = new(1);
        private IBarCodeReader? _barCodeReader;

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

        public DaHuatechSecurityCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.VideoCamera;
        }

        public DaHuatechSecurityCamera() {
        }

        public async void Dispose() {
            await Stop();
            OnCameraDisconnected(new CameraConnectionEventArgs() {
                CameraInfo = this.Info
            });
            OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                CameraInfo = this.Info
            });
        }

        public CameraInfo? Info { get; private set; } = new();
        public SdkType SdkType => SdkType.SecurityCamera;
        public string SdkName => "NetSDKCS.dll";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.PanoramaCamera;
        public string CameraConnectionParameters { get; set; } = string.Empty;
        public int TakePhotoDelay { get; set; }

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            _devInfo.Clear();
            var cameraInfos = new List<CameraInfo>();
            var devices = await BaseDaHuatech.EnumDevices();
            if (devices?.Any() == true) {
                foreach (var cameraInfo in devices.Select(deviceNetInfoExe => new CameraInfo() {
                    Brand = "Dahua",
                    ConnectionType = CameraConnectionType.Ethernet,
                    IpAddress = deviceNetInfoExe.szIP,
                    Name = deviceNetInfoExe.szDevName,
                    Model = deviceNetInfoExe.szDetailType,
                    Port = deviceNetInfoExe.nPort,
                    SerialNumber = deviceNetInfoExe.szSerialNo,
                    Type = CameraType.VideoCamera,
                    Version = deviceNetInfoExe.szDevSoftVersion,
                    //IsAvailable = (s.Value.byInitStatus & 0x1) != 1
                })) {
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
            BindingType = CameraBindingType.ScannerCamera;
            if (param is CameraInfo cameraInfo) {
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    this.Info = devInfo;
                    //注册各种事件
                    if (BindingType == CameraBindingType.ScannerCamera) {
                        _barCodeReader ??= new DynamsoftBarCodeReader();
                        _barCodeReader.BarcodeRead += async (sender, result) => {
                            //读码回调
                            var scanTime = DateTime.Now;
                            var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
                            Bitmap? generateThumbnail = null;
                            generateThumbnail = GenerateThumbnail(result.Image);
                            if (result.BarCodes?.Any() == true) {
                                List<Point>? points = null;
                                if (generateThumbnail is not null) {
                                    //设置图像边框
                                    using var g = Graphics.FromImage(generateThumbnail);

                                    foreach (var barcodeInfo in result?.BarCodes ?? new List<BarcodeInfo>()) {
                                        points = barcodeInfo.BarcodeRegion;
                                        if (points is not null && points.Count == 4 &&
                                            generateThumbnail is not null &&
                                            result?.Image is { Width: > 0, Height: > 0 }) {
                                            var stPointList = new Point[4];
                                            for (var i = 0; i < 4; i++) {
                                                stPointList[i].X = (int)(points[i].X *
                                                                         ((float)generateThumbnail.Width / result.Image.Width));
                                                stPointList[i].Y = (int)(points[i].Y *
                                                                         ((float)generateThumbnail.Height / result.Image.Height));
                                            }
                                            g.DrawPolygon(new System.Drawing.Pen(BarcodeBorderColor, BarcodeBorderSize), stPointList);
                                        }
                                    }
                                }
                                foreach (var barcodeInfo in from barcodeInfo in result?.BarCodes ?? new List<BarcodeInfo>()
                                                            let validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                                                                BarCode = barcodeInfo.Barcode ?? "NoRead",
                                                                ScanTime = DateTime.Now
                                                            })
                                                            where validateData.IsValidationPassed || !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)
                                                            select new { BarcodeInfo = barcodeInfo, IsValid = validateData.IsValidationPassed }) {
                                    OnBarcodeRead(new BarcodeReadEventArgs() {
                                        Barcode = _barCodeFilterContainer.RegexReplace((barcodeInfo.IsValid ? barcodeInfo.BarcodeInfo.Barcode : _barCodeFilterContainer.FilterOutContent) ?? "NoRead"),
                                        CameraSerialNumber = this.Info.SerialNumber,
                                        Image = result?.Image,
                                        ScanTime = scanTime,
                                        Timestamp = timestamp,
                                        ThumbImage = generateThumbnail,
                                        AreaCoords = points,
                                    });
                                }
                            }
                            if (IsRealtimeImageEnabled) {
                                await OnRealtimeImageAsync(new RealtimeImageEventArgs() {
                                    ThumbImage = generateThumbnail,
                                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                                });
                            }
                        };

                        await _barCodeReader.Initialize();
                    }

                    _baseDaHuatech.RegisterImageCallback(devInfo.SerialNumber, async imageBitmap => {
                        try {
                            await _snapRevPhotoSlim.WaitAsync();
                            var tryDequeue = _imageMessageQueue.TryDequeue(out var imageMessageInfo);
                            if (tryDequeue && imageMessageInfo is not null) {
                                var thumbnailImage = GenerateThumbnail(imageBitmap);
                                OnPhotoTaken(new PhotoTakenEventArgs() {
                                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                                    Barcode = imageMessageInfo.Barcode,
                                    BarcodeTimestamp = imageMessageInfo.BarcodeTimestamp,
                                    CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                                    Image = imageBitmap,
                                    ThumbImage = (Bitmap?)thumbnailImage,
                                    PhotoTime = DateTime.Now,
                                });
                            }
                        }
                        catch (Exception e) {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = e
                            });
                        }
                        finally {
                            _snapRevPhotoSlim.Release();
                        }
                    });

                    _baseDaHuatech.RegisterRealtimeFrameCallback(devInfo.SerialNumber, async imageBitmap => {
                        try {
                            if (BindingType == CameraBindingType.ScannerCamera) {
                                //推送图片到解码器
                                _barCodeReader?.EnqueueFrame(imageBitmap);
                            }
                            else {
                                // 直接生成缩略图
                                var thumbnailImage = GenerateThumbnail(imageBitmap);

                                // 使用异步方法触发事件
                                await OnRealtimeImageAsync(new RealtimeImageEventArgs() {
                                    ThumbImage = thumbnailImage
                                });
                            }
                        }
                        catch (Exception e) {
                        }
                    });

                    OnCameraInitialized(new CameraInitializedEventArgs() {
                        CameraInfo = Info
                    });
                    return new KeyValuePair<bool, string>(false, "初始化成功!");
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
            //获取登录账号密码
            //登录设备
            try {
                var parameters = JsonConvert.DeserializeObject<SecurityCameraConnectionParameters>(CameraConnectionParameters);
                if (parameters is not null && Info is not null) {
                    var (key, value) = await _baseDaHuatech.LogIn(Info.SerialNumber,
                        parameters.Username, parameters.Password, parameters.PlayChannelId);

                    if (key) {
                        OnCameraStarted(new CameraStartedEventArgs() {
                            CameraInfo = Info
                        });
                    }
                    else {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception(value)
                        });
                    }
                    return new KeyValuePair<bool, string>(key, value);
                }
                else {
                    return new KeyValuePair<bool, string>(false, "登录参数错误");
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            //断开
            await Task.Yield();
            if (!string.IsNullOrEmpty(this.Info?.SerialNumber)) {
                var (key, value) = await _baseDaHuatech.LogOut(this.Info.SerialNumber);
                if (!key) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(value)
                    });
                }
                _barCodeReader?.Dispose();
                return new KeyValuePair<bool, string>(key, value);
            }
            return new KeyValuePair<bool, string>(false, "设备未连接");
        }

        public async void SetParameters(Dictionary<string, object> parameters) {
            if (_barCodeReader is not null) {
                foreach (var parameter in parameters) {
                    switch (parameter.Key) {
                        case "BarcodeReaderParameter": {
                                //读码器参数
                                var (key, value) = await _barCodeReader.SetBarcodeReaderParameter(
                                    (Dictionary<BarcodeReaderParameter, object>)parameter.Value);
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

        public async void StartRealTimeImage() {
            if (Info is not null) {
                var (key, value) = await _baseDaHuatech.StartRealtimePlay(Info.SerialNumber);
                if (key) {
                    IsRealtimeImageEnabled = true;
                }
                else {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(value)
                    });
                }
            }
        }

        public async void StopRealTimeImage() {
            if (Info is not null) {
                var (key, value) = await _baseDaHuatech.StopRealtimePlay(Info.SerialNumber);
                if (key) {
                    IsRealtimeImageEnabled = false;
                }
                else {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(value)
                    });
                }
            }
        }

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Disconnected;
            CameraExceptionOccurred?.Invoke(this, e);
        }

        public event EventHandler<RealPreviewEventArgs>? RealPreview;

        public Task<KeyValuePair<bool, string>> SaveStream(string filePath, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Zoom(double zoomFactor, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> ControlPtz(double panAngle, double tiltAngle, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> SetStepSize(int stepSize, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> SetFocalLength(double focalLength, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> SetAperture(double aperture, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public event EventHandler<RemotePlaybackEventArgs>? RemotePlaybackRealtimeImage;

        public void StartRemotePlayback(int playbackSpeed) {
            throw new NotImplementedException();
        }

        public void StopRemotePlayback() {
            throw new NotImplementedException();
        }

        public void PauseRemotePlayback() {
            throw new NotImplementedException();
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            Task.Run(async () => {
                await Task.Delay(TakePhotoDelay, cancellation);
                if (Status == CameraStatus.Running) {
                    try {
                        await Task.Delay(TakePhotoDelay, cancellation);
                        await _takePhotoSlim.WaitAsync(cancellation);
                        await Task.Delay(600, cancellation);
                        if (!string.IsNullOrEmpty(Info?.SerialNumber)) {
                            var (key, value) = await _baseDaHuatech.GetRealtimeImage(Info.SerialNumber);
                            if (!key) {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception(value)
                                });
                            }
                            _imageMessageQueue.Enqueue(new CameraImageMessageInfo() {
                                Barcode = barcode,
                                BarcodeTimestamp = barcodeTimestamp,
                            });
                        }
                    }
                    catch (Exception e) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = e
                        });
                    }
                    finally {
                        _takePhotoSlim.Release();
                    }
                }
            }, cancellation);
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            Task.Run(async () => {
                await Task.Delay(delay, cancellation);
                if (Status == CameraStatus.Running) {
                    try {
                        await Task.Delay(TakePhotoDelay, cancellation);
                        await _takePhotoSlim.WaitAsync(cancellation);
                        await Task.Delay(200, cancellation);
                        if (!string.IsNullOrEmpty(Info?.SerialNumber)) {
                            var (key, value) = await _baseDaHuatech.GetRealtimeImage(Info.SerialNumber);
                            if (!key) {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception(value)
                                });
                            }
                            _imageMessageQueue.Enqueue(new CameraImageMessageInfo() {
                                Barcode = barcode,
                                BarcodeTimestamp = barcodeTimestamp,
                            });
                        }
                    }
                    catch (Exception e) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = e
                        });
                    }
                    finally {
                        _takePhotoSlim.Release();
                    }
                }
            }, cancellation);
            return Task.CompletedTask;
        }

        protected virtual async void OnCameraInitialized(CameraInitializedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnCameraStarted(CameraStartedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual async void OnPhotoTaken(PhotoTakenEventArgs e) {
            await Task.Yield();
            PhotoTaken?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(CameraConnectionEventArgs e) {
            await Task.Yield();
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            await Task.Yield();
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual Task OnRealtimeImageAsync(RealtimeImageEventArgs e) {
            RealtimeImage?.Invoke(this, e);
            return Task.CompletedTask;
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
                    var sourcePtr = (byte*)sourceData.Scan0;
                    var thumbnailPtr = (byte*)thumbnailData.Scan0;

                    var sourceBytesPerPixel = 4;
                    var thumbnailBytesPerPixel = 4;

                    var scaleX = (float)thumbnailWidth / sourceImage.Width;
                    var scaleY = (float)thumbnailHeight / sourceImage.Height;

                    var sourceWidth = sourceImage.Width;

                    // 使用 Parallel.For 进行并行处理
                    Parallel.For(0, thumbnailHeight, y => {
                        for (var x = 0; x < thumbnailWidth; x++) {
                            var sourceX = (int)(x / scaleX);
                            var sourceY = (int)(y / scaleY);

                            var sourceIndex = (sourceY * sourceWidth + sourceX) * sourceBytesPerPixel;
                            var thumbnailIndex = (y * thumbnailWidth + x) * thumbnailBytesPerPixel;

                            thumbnailPtr[thumbnailIndex] = sourcePtr[sourceIndex];
                            thumbnailPtr[thumbnailIndex + 1] = sourcePtr[sourceIndex + 1];
                            thumbnailPtr[thumbnailIndex + 2] = sourcePtr[sourceIndex + 2];
                            thumbnailPtr[thumbnailIndex + 3] = sourcePtr[sourceIndex + 3];
                        }
                    });
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

        public IOcr? Ocr { get; set; }
        public int BarcodeBorderSize { get; set; } = 5;
        public Color BarcodeBorderColor { get; set; } = System.Drawing.Color.LawnGreen;
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

        protected virtual void OnBarcodeRead(BarcodeReadEventArgs e) {
            BarcodeRead?.Invoke(this, e);
        }

        protected virtual void OnFilteredBarcodeReturned(BarcodeReadEventArgs e) {
            FilteredBarcodeReturned?.Invoke(this, e);
        }
    }
}