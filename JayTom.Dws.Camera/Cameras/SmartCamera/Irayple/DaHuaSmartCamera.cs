using System;
using System.Linq;
using System.Text;
using ThridLibray;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using MVIDCodeReaderNet;
using System.Diagnostics;
using MvCodeReaderSDKNet;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Camera.FilterContainer;
using JayTom.Dws.Camera.Concurrency;
using static MVIDCodeReaderNet.MVIDCodeReader;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Irayple {

    public class DaHuaSmartCamera : ISmartCamera {

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

        private long _frameNo = 0;
        /// <summary>脱离华睿 SDK 回调执行块解析、图像处理和事件发布的无损顺序调度器。</summary>
        private LosslessOrderedDispatcher<DaHuaCapturedFrame>? _frameDispatcher;

        /// <summary>
        /// 摄像头对象
        /// </summary>
        private IDevice? _device;

        /// <summary>
        /// 原图宽度
        /// </summary>
        private int _originalWidth = 1;

        /// <summary>
        /// 原图高度
        /// </summary>
        private int _originalHeight = 1;

        /// <summary>匹配相机块数据中的条码数量。</summary>
        private static readonly Regex ChunkCountPattern = new(
            @"(?:BarCodeNum|QRNum)\s+Value:(\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary>匹配相机块数据中的条码文本。</summary>
        private static readonly Regex ChunkCodePattern = new(
            @"(?:Code|QR)(\d+)_CodeData\s+Value:(.+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary>匹配相机块数据中的条码顶点。</summary>
        private static readonly Regex ChunkPointPattern = new(
            @"(?:Code|QR)(\d+)_Point(\d+)_(X|Y)\s+Value:(\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public DaHuaSmartCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.SmartCamera;
        }

        public DaHuaSmartCamera() {
        }

        public void Dispose() {
            Stop().GetAwaiter().GetResult();
            OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                CameraInfo = this.Info
            });
        }

        public CameraInfo? Info { get; set; }
        public SdkType SdkType { get; } = SdkType.SmartCameraSdk;
        public string SdkName { get; } = "ThridLibray.dll";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; }

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            _devInfo.Clear();
            var cameraInfos = new List<CameraInfo>();
            var devices = Enumerator.EnumerateDevices();
            var infos = devices.Select(s => new CameraInfo() {
                Id = s.Index,
                Brand = s.Vendor,
                SerialNumber = s.SerialNumber,
                Name = s.Name,
                Model = s.Model,
                Version = s.Version,
                ConnectionType = CameraConnectionType.Ethernet,
                SupportedBindingType = CameraBindingType.ScannerCamera |
                                       CameraBindingType.PanoramaCamera | CameraBindingType.OcrCamera
            })?.ToList();

            if (infos?.Any() == true) {
                foreach (var cameraInfo in infos.Where(cameraInfo => cameraInfo.Brand.Contains("Technology") &&
                                                                     (cameraInfo.Model.StartsWith("DH-MV") || cameraInfo.Model.StartsWith("S")))) {
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
            if (param is CameraInfo info) {
                this.Info = info;
                //实例化对象
                var tryGetValue = _devInfo.TryGetValue(info.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    _device = Enumerator.GetDeviceByIndex(checked((int)devInfo.Id));
                    if (_device != null) {
                        //注册事件
                        //相机打开事件
                        _device.CameraOpened += delegate (object? sender, EventArgs args) {
                            //设置参数
                            //设置触发
                            _device.TriggerSet.Open(TriggerSourceEnum.Line1);
                            //设置图像格式
                            using (var p = _device.ParameterCollection[ParametrizeNameSet.ImagePixelFormat]) {
                                p.SetValue("Mono8");
                            }
                            using (var p = _device.ParameterCollection[ParametrizeNameSet.ImageHeight]) {
                                int.TryParse(p.GetValue().ToString(), out _originalHeight);
                            }
                            using (var p = _device.ParameterCollection[ParametrizeNameSet.ImageWidth]) {
                                int.TryParse(p.GetValue().ToString(), out _originalWidth);
                            }
                            //设置曝光
                            using (IFloatParameter p = _device.ParameterCollection[ParametrizeNameSet.ExposureTime]) {
                                //p.SetValue(1000);
                            }
                            //设置增益
                            using (IFloatParameter p = _device.ParameterCollection[ParametrizeNameSet.GainRaw]) {
                                //p.SetValue(1.0);
                            }
                            // 保留少量驱动缓冲以降低延迟和非托管内存占用。
                            _device.StreamGrabber.SetBufferCount(3);
                            //开启码流
                            var loopThread = _device.GrabUsingGrabLoopThread();
                            if (!loopThread) {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception("开启码流失败!")
                                });
                            }
                        };
                        //相机断连事件
                        _device.ConnectionLost += delegate (object? sender, EventArgs args) {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception($"摄像头:{Info?.SerialNumber}丢失/断开")
                            });
                        };

                        OnCameraInitialized(new CameraInitializedEventArgs() {
                            CameraInfo = this.Info
                        });
                        return new KeyValuePair<bool, string>(true, "初始化成功");
                    }
                    else {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("获取设备对象失败!")
                        });
                        return new KeyValuePair<bool, string>(false, "获取设备对象失败!");
                    }
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
            //打开设备
            if (_device is not null) {
                if (_device.IsOpen) {
                    return new KeyValuePair<bool, string>(true, "设备已在运行中!");
                }

                var open = _device.Open();
                if (open) {
                    EnsureFrameDispatcher();
                    //码流回调事件
                    _device.StreamGrabber.ImageGrabbed += delegate (object? sender, GrabbedEventArgs args) {
                        QueueCapturedFrame(args.GrabResult, DateTime.Now);
                    };
                    OnCameraStarted(new CameraStartedEventArgs() {
                        CameraInfo = this.Info,
                        Camera = this
                    });
                    return new KeyValuePair<bool, string>(true, "设备启动成功!");
                }
                else {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("设备启动失败!")
                    });
                    return new KeyValuePair<bool, string>(false, "设备启动失败!");
                }
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("设备未初始化!")
                });
                return new KeyValuePair<bool, string>(false, "设备未初始化!");
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await Task.Yield();
            if (_device is not null) {
                _device?.ShutdownGrab();
                _frameDispatcher?.Dispose();
                _frameDispatcher = null;
                _device?.Close();
                //_device?.Dispose();
                _device = null;
                OnCameraDisconnected(new CameraConnectionEventArgs() {
                    CameraInfo = this.Info
                });
            }
            return new KeyValuePair<bool, string>(true, "设备停止成功!");
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public bool IsRealtimeImageEnabled { get; private set; }

        public void StartRealTimeImage() {
            //先设置自由拉流模式,再开放实时
            IsRealtimeImageEnabled = true;
        }

        public void StopRealTimeImage() {
            IsRealtimeImageEnabled = false;
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation = default) {
            cancellation.ThrowIfCancellationRequested();
            _device?.TriggerSet?.ExecuteSoftwareTrigger();
            return Task.CompletedTask;
        }

        public async Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, TimeSpan delay, CancellationToken cancellation = default) {
            await Task.Delay(delay, cancellation).ConfigureAwait(false);
            await TakePhotoAsync(barcode, packageTimestampMilliseconds, cancellation).ConfigureAwait(false);
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
            _device?.TriggerSet?.ExecuteSoftwareTrigger();
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

        /// <summary>确保华睿 SDK 回调后的重处理运行在独立长驻线程上。</summary>
        private void EnsureFrameDispatcher() {
            _frameDispatcher ??= new LosslessOrderedDispatcher<DaHuaCapturedFrame>(
                frame => GrabResultDecode(frame.RawData, frame.ScanTime, frame.FrameNo),
                (_, exception) => OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = new Exception("后台处理华睿扫码帧异常", exception)
                }));
        }

        /// <summary>在 SDK 回调入口立即克隆帧，并以原始观测时间无等待入队。</summary>
        private void QueueCapturedFrame(IGrabbedRawData rawData, DateTime scanTime) {
            try {
                EnsureFrameDispatcher();
                var frame = new DaHuaCapturedFrame(
                    rawData.Clone(),
                    scanTime,
                    Interlocked.Increment(ref _frameNo));
                if (_frameDispatcher?.TryEnqueue(frame) != true) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                        Exception = new InvalidOperationException("华睿相机帧处理器已经停止。")
                    });
                }
            }
            catch (Exception exception) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = new Exception("克隆华睿扫码帧异常", exception)
                });
            }
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="grabbedRawData"></param>
        private void GrabResultDecode(IGrabbedRawData grabbedRawData, DateTime scanTime, long frameNo) {
            Bitmap? bitmap = null;
            try {
                var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
                var barcodeInfo = new List<DaHuaBarcodeInfo>();
                var chunkData = grabbedRawData.ChunkData;
                for (var i = 0; i < chunkData.ChunkCount; i++) {
                    uint chunkNumber = 0;
                    var vecChunkInfos = new List<string>();
                    chunkData.GetChunkDataByIndex((uint)i, ref chunkNumber, ref vecChunkInfos);
                    ParseChunkData(chunkNumber, vecChunkInfos, scanTime, barcodeInfo);
                }

                var noReadConsumer = barcodeInfo.Count == 0 && IsUseTriggerMode &&
                                     TriggerMode == TriggerMode.Hardware && NotBarcodeHitEvent is not null;
                var barcodeConsumerCount = BarcodeReadTriggered is null ? 0 : barcodeInfo.Count;
                var realtimeConsumer = IsRealtimeImageEnabled && RealtimeImage is not null;
                if (!noReadConsumer && barcodeConsumerCount == 0 && !realtimeConsumer) {
                    return;
                }

                bitmap = grabbedRawData.ToBitmap(true);
                var thumbnailImage = GenerateThumbnail(bitmap);
                if (bitmap is null || thumbnailImage is null) {
                    bitmap?.Dispose();
                    bitmap = null;
                    return;
                }

                if (IsShowBarcodeBorder && barcodeInfo.Count > 0) {
                    using var graphics = Graphics.FromImage(thumbnailImage);
                    using var pen = new Pen(BarcodeBorderColor, BarcodeBorderSize);
                    foreach (var barcode in barcodeInfo) {
                        var points = new Point[barcode.BarcodeRegionCoordinates.Count];
                        for (var pointIndex = 0; pointIndex < points.Length; pointIndex++) {
                            var point = barcode.BarcodeRegionCoordinates[pointIndex];
                            points[pointIndex] = new Point(
                                point.X * thumbnailImage.Width / Math.Max(1, _originalWidth),
                                point.Y * thumbnailImage.Height / Math.Max(1, _originalHeight));
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
                            FrameNo = frameNo
                        });
                }
                for (var index = 0; index < barcodeConsumerCount; index++) {
                    var isLast = index == barcodeConsumerCount - 1;
                    var barcode = barcodeInfo[index];
                    OnBarcodeReadTriggered(new BarcodeTriggeredEventArgs {
                        Timestamp = timestamp,
                        Barcode = _barCodeFilterContainer.RegexReplace(
                            string.IsNullOrWhiteSpace(barcode.BarCode) ? "NoRead" : barcode.BarCode),
                        Image = isLast ? bitmap : new Bitmap(bitmap),
                        ThumbImage = isLast ? thumbnailImage : new Bitmap(thumbnailImage),
                        CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                        ScanTime = scanTime,
                        AreaCoords = barcode.BarcodeRegionCoordinates,
                        FrameNo = frameNo
                    });
                }

                if (primaryConsumerCount == 0) {
                    bitmap.Dispose();
                }
                bitmap = null;
                if (realtimeConsumer) {
                    OnRealtimeImage(new RealtimeImageEventArgs {
                        ThumbImage = realtimeThumbnail,
                        Timestamp = timestamp
                    });
                }
                else if (primaryConsumerCount == 0) {
                    thumbnailImage.Dispose();
                }
            }
            catch (Exception e) {
                bitmap?.Dispose();
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
        }

        /// <summary>
        /// 单遍解析一个相机块，避免对每个字段反复扫描全部文本。
        /// </summary>
        private void ParseChunkData(
            uint chunkType,
            List<string> values,
            DateTime scanTime,
            List<DaHuaBarcodeInfo> output) {
            var parsed = new Dictionary<int, DaHuaBarcodeInfo>();
            var codeCount = 0;
            foreach (var value in values) {
                var countMatch = ChunkCountPattern.Match(value);
                if (countMatch.Success) {
                    int.TryParse(countMatch.Groups[1].Value, out codeCount);
                    continue;
                }

                var codeMatch = ChunkCodePattern.Match(value);
                if (codeMatch.Success && int.TryParse(codeMatch.Groups[1].Value, out var codeIndex)) {
                    GetOrCreateBarcode(parsed, codeIndex, chunkType).BarCode = codeMatch.Groups[2].Value;
                    continue;
                }

                var pointMatch = ChunkPointPattern.Match(value);
                if (!pointMatch.Success ||
                    !int.TryParse(pointMatch.Groups[1].Value, out var pointCodeIndex) ||
                    !int.TryParse(pointMatch.Groups[2].Value, out var pointIndex) ||
                    !int.TryParse(pointMatch.Groups[4].Value, out var coordinate) ||
                    pointIndex is < 0 or > 3) {
                    continue;
                }

                var info = GetOrCreateBarcode(parsed, pointCodeIndex, chunkType);
                var point = info.BarcodeRegionCoordinates[pointIndex];
                if (pointMatch.Groups[3].Value == "X") {
                    point.X = coordinate;
                }
                else {
                    point.Y = coordinate;
                }
                info.BarcodeRegionCoordinates[pointIndex] = point;
            }

            for (var index = 0; index < codeCount; index++) {
                if (!parsed.TryGetValue(index, out var barcode)) {
                    continue;
                }
                var validation = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo {
                    BarCode = string.IsNullOrWhiteSpace(barcode.BarCode) ? "NoRead" : barcode.BarCode,
                    ScanTime = scanTime
                });
                if (validation.IsValidationPassed ||
                    !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)) {
                    barcode.BarCode = validation.IsValidationPassed
                        ? barcode.BarCode
                        : _barCodeFilterContainer.FilterOutContent;
                    output.Add(barcode);
                }
            }
        }

        /// <summary>
        /// 获取或创建指定序号的条码解析结果。
        /// </summary>
        private static DaHuaBarcodeInfo GetOrCreateBarcode(
            Dictionary<int, DaHuaBarcodeInfo> parsed,
            int index,
            uint chunkType) {
            if (parsed.TryGetValue(index, out var existing)) {
                return existing;
            }

            var created = new DaHuaBarcodeInfo {
                BarcodeType = chunkType == 0x80000000 ? CodeType.BarCode : CodeType.QrCode,
                BarcodeRegionCoordinates = [new Point(), new Point(), new Point(), new Point()]
            };
            parsed.Add(index, created);
            return created;
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

        public class DaHuaBarcodeInfo {

            /// <summary>
            /// 条码种类
            /// </summary>
            public CodeType BarcodeType { get; set; }

            /// <summary>
            /// 条码
            /// </summary>
            public string BarCode { get; set; } = string.Empty;

            /// <summary>
            /// 条码坐标
            /// </summary>
            public List<Point> BarcodeRegionCoordinates { get; set; } = new();
        }

        public enum CodeType {

            /// <summary>
            /// 条码
            /// </summary>
            BarCode,

            /// <summary>
            /// 二维码
            /// </summary>
            QrCode,
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

        protected virtual void OnBarcodeReadTriggered(BarcodeTriggeredEventArgs e) {
            var handler = BarcodeReadTriggered;
            if (handler is null) {
                e.Image?.Dispose();
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }

        protected virtual void OnRealtimeImage(RealtimeImageEventArgs e) {
            var handler = RealtimeImage;
            if (handler is null) {
                e.ThumbImage?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }

        public static Image? GenerateThumbnail(Image? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }
    }
}
