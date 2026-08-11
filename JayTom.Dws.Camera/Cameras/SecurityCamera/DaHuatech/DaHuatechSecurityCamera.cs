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
using JayTom.Dws.Camera.Nvr;
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
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech {

    public class DaHuatechSecurityCamera : ISecurityCamera {
        private BaseDaHuatech _baseDaHuatech = BaseDaHuatech.CreateInstance();
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

        public void Dispose() {
            Stop().GetAwaiter().GetResult();
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
                    Type = deviceNetInfoExe.szDeviceType.Contains("IPC", StringComparison.InvariantCultureIgnoreCase) ? CameraType.VideoCamera : CameraType.NvrDevice,
                    Version = deviceNetInfoExe.szDevSoftVersion,
                    CameraNvrInfo = deviceNetInfoExe.szDeviceType.Contains("NVR", StringComparison.InvariantCultureIgnoreCase) ? new CameraNvrInfo() {
                        ChannelCount = deviceNetInfoExe.wRemoteVideoInputCh
                    } : new CameraNvrInfo(),
                    //IsAvailable = (s.Value.byInitStatus & 0x1) != 1
                    SupportedBindingType = CameraBindingType.ScannerCamera |
                                            CameraBindingType.PanoramaCamera | CameraBindingType.OcrCamera
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
                        _barCodeReader.BarcodeRead += (sender, result) => HandleBarcodeResult(result);

                        await _barCodeReader.InitializeAsync();
                    }

                    _baseDaHuatech.RegisterImageCallback(devInfo.SerialNumber, imageBitmap => {
                        try {
                            var tryDequeue = _imageMessageQueue.TryDequeue(out var imageMessageInfo);
                            if (tryDequeue && imageMessageInfo is not null) {
                                var thumbnailImage = GenerateThumbnail(imageBitmap);
                                OnPhotoTaken(new PhotoTakenEventArgs() {
                                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                                    Barcode = imageMessageInfo.Barcode,
                                    PackageTimestampMilliseconds = imageMessageInfo.PackageTimestampMilliseconds,
                                    CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                                    Image = imageBitmap,
                                    ThumbImage = (Bitmap?)thumbnailImage,
                                    PhotoTime = DateTime.Now,
                                });
                            }
                            else {
                                imageBitmap.Dispose();
                            }
                        }
                        catch (Exception e) {
                            imageBitmap.Dispose();
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = e
                            });
                        }
                    });

                    _baseDaHuatech.RegisterRealtimeFrameCallback(devInfo.SerialNumber, async imageBitmap => {
                        try {
                            if (BindingType == CameraBindingType.ScannerCamera) {
                                //推送图片到解码器
                                _barCodeReader?.EnqueueFrame(imageBitmap);
                            }
                            else {
                                var thumbnailImage = GenerateThumbnail(imageBitmap);
                                imageBitmap.Dispose();
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
                    return new KeyValuePair<bool, string>(true, "初始化成功!");
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
                        parameters.Username, parameters.Password, parameters.PlayChannelNumber);

                    if (key) {
                        OnCameraStarted(new CameraStartedEventArgs() {
                            CameraInfo = Info,
                            Camera = this
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

            await Task.Yield(); ;
            if (!string.IsNullOrEmpty(this.Info?.SerialNumber)) {
                await _baseDaHuatech.StopRealtimePlay(this.Info.SerialNumber);
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

        public void SetParameters(Dictionary<string, object> parameters) {
            if (_barCodeReader is not null) {
                foreach (var parameter in parameters) {
                    switch (parameter.Key) {
                        case "BarcodeReaderParameter": {
                                //读码器参数
                                var (key, value) = _barCodeReader.ApplySettingsAsync(
                                    (BarcodeReaderSettings)parameter.Value).GetAwaiter().GetResult();
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
            if (Info is not null) {
                var (key, value) = _baseDaHuatech.StartRealtimePlay(Info.SerialNumber).GetAwaiter().GetResult();
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

        public void StopRealTimeImage() {
            if (Info is not null) {
                var (key, value) = _baseDaHuatech.StopRealtimePlay(Info.SerialNumber).GetAwaiter().GetResult();
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

        protected virtual void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            Status = CameraStatus.Disconnected;
            CameraExceptionOccurred?.Invoke(this, e);
        }

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

        public async Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation = default) {
            await Task.Delay(TakePhotoDelay, cancellation);
            await CapturePhotoAsync(barcode, packageTimestampMilliseconds, cancellation);
        }

        public async Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, TimeSpan delay, CancellationToken cancellation = default) {
            await Task.Delay(delay, cancellation);
            await CapturePhotoAsync(barcode, packageTimestampMilliseconds, cancellation);
        }

        private async Task CapturePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation) {
            if (Status != CameraStatus.Running || string.IsNullOrEmpty(Info?.SerialNumber)) {
                return;
            }

            var lockTaken = false;
            try {
                await _takePhotoSlim.WaitAsync(cancellation);
                lockTaken = true;
                _imageMessageQueue.Enqueue(new CameraImageMessageInfo {
                    Barcode = barcode,
                    PackageTimestampMilliseconds = packageTimestampMilliseconds
                });
                var (success, message) = await _baseDaHuatech.GetRealtimeImage(Info.SerialNumber);
                if (!success) {
                    _imageMessageQueue.TryDequeue(out _);
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                        Exception = new Exception(message)
                    });
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = e
                });
            }
            finally {
                if (lockTaken) {
                    _takePhotoSlim.Release();
                }
            }
        }

        private void HandleBarcodeResult(BarcodeResult result) {
            var image = result.Image;
            if (image is null) {
                return;
            }

            try {
                var scanTime = result.ScanTime;
                var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
                var results = new List<(string Barcode, List<Point>? AreaCoords)>();
                foreach (var barcodeInfo in result.BarCodes ?? []) {
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
                    foreach (var item in results) {
                        if (item.AreaCoords is not { Count: 4 }) {
                            continue;
                        }
                        var points = new Point[item.AreaCoords.Count];
                        for (var index = 0; index < points.Length; index++) {
                            points[index] = new Point(
                                item.AreaCoords[index].X * thumbnail.Width / Math.Max(1, image.Width),
                                item.AreaCoords[index].Y * thumbnail.Height / Math.Max(1, image.Height));
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
                        CameraSerialNumber = Info?.SerialNumber ?? string.Empty,
                        Image = isLast ? image : new Bitmap(image),
                        ScanTime = scanTime,
                        Timestamp = timestamp,
                        ThumbImage = isLast ? thumbnail : new Bitmap(thumbnail),
                        AreaCoords = results[index].AreaCoords
                    });
                }
                if (barcodeConsumerCount == 0) {
                    image.Dispose();
                }
                if (realtimeConsumer) {
                    _ = OnRealtimeImageAsync(new RealtimeImageEventArgs {
                        ThumbImage = realtimeThumbnail,
                        Timestamp = timestamp
                    });
                }
            }
            catch (Exception exception) {
                image.Dispose();
                OnCameraExceptionOccurred(new CameraExceptionEventArgs { Exception = exception });
            }
        }

        protected virtual void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e) {
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
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

        protected virtual void OnCameraDisconnected(CameraConnectionEventArgs e) {
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual Task OnRealtimeImageAsync(RealtimeImageEventArgs e) {
            var handler = RealtimeImage;
            if (handler is null) {
                e.ThumbImage?.Dispose();
                return Task.CompletedTask;
            }
            handler.Invoke(this, e);
            return Task.CompletedTask;
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
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
            var handler = BarcodeRead;
            if (handler is null) {
                e.Image?.Dispose();
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }

        protected virtual void OnFilteredBarcodeReturned(BarcodeReadEventArgs e) {
            FilteredBarcodeReturned?.Invoke(this, e);
        }
    }
}
