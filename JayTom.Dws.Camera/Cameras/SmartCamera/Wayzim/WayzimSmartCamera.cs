using System;
using CamSDK;
using System.Linq;
using System.Text;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Camera.FilterContainer;
using static JayTom.Dws.Camera.Cameras.SmartCamera.Irayple.DaHuaSmartCamera;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim {

    public class WayzimSmartCamera : ISmartCamera {
        private static SemaphoreSlim _bindingSlim = new(1);

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

        //相机对象
        private CameraDataService? _cameraDataService;

        private long _frameNo = 0;

        /// <summary>
        /// 固定端口
        /// </summary>
        public WayzimSmartCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.SmartCamera;
        }

        public WayzimSmartCamera() {
        }

        public void Dispose() {
            Stop().GetAwaiter().GetResult();
        }

        public CameraInfo? Info { get; private set; }
        public SdkType SdkType { get; private set; } = SdkType.SmartCameraSdk;
        public string SdkName => "CamSDK.dll";
        public bool IsOriginalImageOut { get; set; } = true;
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.ScannerCamera;

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            GWCameraService.SetWriteLog(s => { });
            var cameraInfoStructs = GWCameraService.GetCameraInfos();
            var cameraInfos = cameraInfoStructs?.Select(s => new CameraInfo() {
                Brand = "Wayzim",
                ConnectionType = CameraConnectionType.Ethernet,
                IpAddress = s.CamIpAdr,
                IsAvailable = true,
                Name = s.DeviceName,
                Model = "SmartCamera",
                SerialNumber = s.CamMacAdr.Replace(":", string.Empty),
                Id = s.DevIndex,
                SupportedBindingType = CameraBindingType.ScannerCamera |
                                       CameraBindingType.PanoramaCamera | CameraBindingType.OcrCamera
            })?.ToList();
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception($"相机数量:{cameraInfos?.Count}")
            });
            if (cameraInfos?.Any() == true) {
                foreach (var cameraInfo in cameraInfos) {
                    _devInfo.AddOrUpdate(cameraInfo.SerialNumber, cameraInfo, (k, v) => cameraInfo);
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
            if (Status is CameraStatus.Running or CameraStatus.Initialized) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }
            if (param is CameraInfo info) {
                this.Info = info;
                var tryGetValue = _devInfo.TryGetValue(info.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    this.Info = devInfo;
                }
                else {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("设备不存在或已离线,请重新枚举!")
                    });
                    return new KeyValuePair<bool, string>(false, "设备不存在或已离线,请重新枚举!");
                }
            }
            return new KeyValuePair<bool, string>(true, "初始化成功");
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            const int port = 51236;
            var errorMsg = "";
            try {
                await _bindingSlim.WaitAsync();
                await Task.Delay(50);
                if (Status == CameraStatus.Running) {
                    return new KeyValuePair<bool, string>(false, "设备已在运行中");
                }

                _cameraDataService = GWCameraService.GetCameraInstance(checked((int)(Info?.Id ?? 0)), ReaultCallBack, null, ref errorMsg, port);
                if (!errorMsg.Equals(string.Empty)) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"相机回调绑定失败:{errorMsg}")
                    });
                    return new KeyValuePair<bool, string>(false, errorMsg);
                }
                else {
                    OnCameraInitialized(new CameraInitializedEventArgs() {
                        CameraInfo = this.Info,
                    });
                    return new KeyValuePair<bool, string>(true, errorMsg);
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
            finally {
                _bindingSlim.Release();
            }
            return new KeyValuePair<bool, string>(false, "Info is null");
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="infostruct"></param>
        /// <param name="tag"></param>
        private void ReaultCallBack(ResultInfoStruct infostruct, object tag) {
            Bitmap? bitmap = null;
            var scanTime = DateTime.Now;
            var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
            try {
                var codeInfos = infostruct.CodeInfo.CodeInfos ?? [];
                var results = new List<(string Barcode, List<Point> AreaCoords)>(codeInfos.Count);
                foreach (var codeInfo in codeInfos) {
                    var validation = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo {
                        BarCode = string.IsNullOrWhiteSpace(codeInfo.Code) ? "NoRead" : codeInfo.Code,
                        ScanTime = scanTime
                    });
                    if (validation.IsValidationPassed ||
                        !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)) {
                        results.Add((
                            _barCodeFilterContainer.RegexReplace(
                                validation.IsValidationPassed
                                    ? codeInfo.Code
                                    : _barCodeFilterContainer.FilterOutContent),
                            ConvertPoint(codeInfo)));
                    }
                }

                var barcodeConsumerCount = BarcodeReadTriggered is null ? 0 : results.Count;
                var noReadConsumer = codeInfos.Count == 0 && NotBarcodeHitEvent is not null;
                var realtimeConsumer = IsRealtimeImageEnabled && RealtimeImage is not null;
                if (barcodeConsumerCount == 0 && !noReadConsumer && !realtimeConsumer) {
                    return;
                }
                if (infostruct.ImageInfo is not { Size: > 0, ImageType: ImageTypes.JPEG }) {
                    return;
                }

                bitmap = ConvertByteArrayToBitmap(infostruct.ImageInfo.ImageBytes);
                var thumbnailImage = GenerateThumbnail(bitmap);
                if (bitmap is null || thumbnailImage is null) {
                    bitmap?.Dispose();
                    bitmap = null;
                    return;
                }

                if (IsShowBarcodeBorder && results.Count > 0) {
                    int.TryParse(infostruct.CodeInfo.ResolutionX, out var imageWidth);
                    int.TryParse(infostruct.CodeInfo.ResolutionY, out var imageHeight);
                    using var graphics = Graphics.FromImage(thumbnailImage);
                    using var pen = new Pen(BarcodeBorderColor, BarcodeBorderSize);
                    foreach (var result in results) {
                        var points = new Point[result.AreaCoords.Count];
                        for (var index = 0; index < points.Length; index++) {
                            points[index] = new Point(
                                result.AreaCoords[index].X * thumbnailImage.Width / Math.Max(1, imageWidth),
                                result.AreaCoords[index].Y * thumbnailImage.Height / Math.Max(1, imageHeight));
                        }
                        if (points.Length >= 3) {
                            graphics.DrawPolygon(pen, points);
                        }
                    }
                }

                var primaryConsumerCount = barcodeConsumerCount + (noReadConsumer ? 1 : 0);
                var realtimeThumbnail = realtimeConsumer && primaryConsumerCount > 0
                    ? new Bitmap(thumbnailImage)
                    : thumbnailImage;
                if (noReadConsumer) {
                    OnNotBarcodeHitEvent(new BarcodeReadEventArgs {
                        Timestamp = timestamp,
                        Barcode = "NoRead",
                        Image = bitmap,
                        ThumbImage = thumbnailImage,
                        CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                        ScanTime = scanTime,
                        FrameNo = _frameNo
                    });
                }
                for (var index = 0; index < barcodeConsumerCount; index++) {
                    var isLast = index == barcodeConsumerCount - 1;
                    OnBarcodeReadTriggered(new BarcodeTriggeredEventArgs {
                        Timestamp = timestamp,
                        Barcode = results[index].Barcode,
                        Image = isLast ? bitmap : new Bitmap(bitmap),
                        ThumbImage = isLast ? thumbnailImage : new Bitmap(thumbnailImage),
                        CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                        ScanTime = scanTime,
                        AreaCoords = results[index].AreaCoords,
                        FrameNo = _frameNo
                    });
                }
                if (primaryConsumerCount == 0) {
                    bitmap.Dispose();
                }
                bitmap = null;
                if (realtimeConsumer) {
                    OnRealtimeImage(new RealtimeImageEventArgs {
                        Timestamp = timestamp,
                        ThumbImage = realtimeThumbnail
                    });
                }
                else if (primaryConsumerCount == 0) {
                    thumbnailImage.Dispose();
                }
            }
            catch (Exception exception) {
                bitmap?.Dispose();
                OnCameraExceptionOccurred(new CameraExceptionEventArgs { Exception = exception });
            }
            finally {
                Interlocked.Increment(ref _frameNo);
                infostruct.CodeInfo = default;
            }
        }

        private Bitmap? ConvertByteArrayToBitmap(byte[] imageData) {
            try {
                using var stream = new MemoryStream(imageData, false);
                using var image = Image.FromStream(stream, true);
                return new Bitmap(image);
            }
            catch (Exception) {
                return null;
            }
        }

        /*private async Task<Bitmap?> ConvertByteArrayToBitmap(byte[] imageData) {
            await Task.Yield();
            Bitmap? bitmap = null;

            using var ms = new MemoryStream(imageData);
            try {
                bitmap = new Bitmap(ms);
            }
            catch (Exception ex) {
                bitmap = null;
            }
            finally {
                Array.Clear(imageData, 0, imageData.Length);
            }

            return bitmap;
        }*/

        private List<Point> ConvertPoint(CodeInfo info) {
            var points = new List<Point>();
            for (var i = 0; i < info.PtCorner.Length; i += 2) {
                var x = info.PtCorner[i];
                var y = info.PtCorner[i + 1];
                points.Add(new Point(x, y));
            }

            return points;
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await Task.Yield();
            try {
                _cameraDataService?.Dispose();
                OnCameraDisconnected(new CameraConnectionEventArgs() {
                    CameraInfo = Info
                });
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception("该SDK无参数设置函数")
            });
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
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception("该SDK无拍照函数")
            });
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception("该SDK无拍照函数")
            });
            return Task.CompletedTask;
        }

        public int TakePhotoDelay { get; set; }

        /// <summary>
        /// Ocr
        /// </summary>
        public IOcr? Ocr { get; set; }

        public int BarcodeBorderSize { get; set; } = 5;
        public bool IsHideNoRead { get; set; }
        public Color BarcodeBorderColor { get; set; } = Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;
        public bool IsUseTriggerMode { get; set; } = true;
        public TriggerMode TriggerMode { get; set; } = TriggerMode.Hardware;
        public int SourceLine { get; set; }
        public bool IsMergeBarCodes { get; set; }
        public string MultiBarcodeDelimiter { get; set; }

        public void SoftwareTriggerOnce() {
            //
        }

        public event EventHandler<BarcodeTriggeredEventArgs>? BarcodeReadTriggered;

        public event EventHandler<BarcodeReadEventArgs>? FilteredBarcodeReturned;

        public event EventHandler<BarcodeReadEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<OcrResult>? OcrContentRecognized;

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

        protected virtual void OnCameraDisconnected(CameraConnectionEventArgs e) {
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e) {
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual void OnCameraStopped(CameraStoppedEventArgs e) {
            Status = CameraStatus.Disconnected;
            CameraStopped?.Invoke(this, e);
        }

        protected virtual void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
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

        protected virtual void OnBarcodeReadTriggered(BarcodeTriggeredEventArgs e) {
            var handler = BarcodeReadTriggered;
            if (handler is null) {
                e.Image?.Dispose();
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }

        protected virtual void OnNotBarcodeHitEvent(BarcodeReadEventArgs e) {
            var handler = NotBarcodeHitEvent;
            if (handler is null) {
                e.Image?.Dispose();
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        public static Image? GenerateThumbnail1(Image? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }
    }
}
