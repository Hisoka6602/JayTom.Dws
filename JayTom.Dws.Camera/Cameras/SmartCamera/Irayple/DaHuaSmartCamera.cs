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
using JayTom.Dws.Abstractions.Threading;
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
        /// <summary>串行保护同一设备的启动和停止。</summary>
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        /// <summary>隔离不同采集会话的迟到回调和排队帧。</summary>
        private readonly CameraCaptureSession _captureSession = new();
        /// <summary>华睿没有触发序号时，以连续回调静默 200ms 作为电平触发帧组的收束条件。</summary>
        private static readonly TimeSpan TriggerFrameQuietPeriod = TimeSpan.FromMilliseconds(200);
        /// <summary>合并电平触发期间连续帧中的 NoRead 候选。</summary>
        private TriggerBurstNoReadGate? _noReadGate;
        /// <summary>记录相机实际返回的硬件触发激活方式是否为高低电平。</summary>
        private bool _isLevelTrigger;
        /// <summary>保存可解除的码流回调，避免反复启动时叠加订阅。</summary>
        private EventHandler<GrabbedEventArgs>? _imageGrabbedHandler;
        /// <summary>保存设备打开回调，释放设备时解除引用。</summary>
        private EventHandler<EventArgs>? _cameraOpenedHandler;
        /// <summary>保存设备断线回调，释放设备时解除引用。</summary>
        private EventHandler<EventArgs>? _connectionLostHandler;

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
            TaskCleanup.Observe(DisposeCoreAsync(), exception => OnCameraExceptionOccurred(
                new CameraExceptionEventArgs { Exception = exception }));
        }

        /// <summary>异步停止相机后发布注销事件。</summary>
        private async Task DisposeCoreAsync() {
            var stopped = await Stop();
            if (!stopped.Key) {
                throw new InvalidOperationException(stopped.Value);
            }
            if (_device is not null) {
                _device.CameraOpened -= _cameraOpenedHandler;
                _device.ConnectionLost -= _connectionLostHandler;
                _device = null;
            }
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

        public async Task<KeyValuePair<bool, string>> Initialize(CameraInfo param, CancellationToken cancellationToken = default) {
            await Task.Yield();
            if (_device is not null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }
            if (param is CameraInfo info) {
                this.Info = info;
                //实例化对象
                var tryGetValue = _devInfo.TryGetValue(info.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    _device = Enumerator.GetDeviceByIndex(checked((int)devInfo.Id));
                    if (_device != null) {
                        //注册事件
                        //相机打开事件
                        _cameraOpenedHandler = delegate (object? sender, EventArgs args) {
                            //设置参数
                            //设置触发
                            ConfigureTrigger(_device.TriggerSet);
                            ReadTriggerActivation(_device.ParameterCollection);
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
                        };
                        _device.CameraOpened += _cameraOpenedHandler;
                        //相机断连事件
                        _connectionLostHandler = delegate (object? sender, EventArgs args) {
                            _captureSession.Stop();
                            _noReadGate?.Dispose();
                            _noReadGate = null;
                            OnCameraDisconnected(new CameraConnectionEventArgs { CameraInfo = Info });
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception($"摄像头:{Info?.SerialNumber}丢失/断开")
                            });
                        };
                        _device.ConnectionLost += _connectionLostHandler;

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

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken cancellationToken = default) {
            await _lifecycleGate.WaitAsync(cancellationToken);
            try {
                //打开设备
                if (_device is not null) {
                    if (Status == CameraStatus.Running && _device.IsGrabbing) {
                        return new KeyValuePair<bool, string>(true, "设备已在运行中!");
                    }

                    var open = _device.IsOpen || _device.Open();
                    if (open) {
                        EnsureFrameDispatcher();
                        var generation = _captureSession.Start();
                        if (_isLevelTrigger) {
                            EnsureNoReadGate();
                        }
                        //码流回调事件
                        if (_imageGrabbedHandler is not null) {
                            _device.StreamGrabber.ImageGrabbed -= _imageGrabbedHandler;
                        }
                        _imageGrabbedHandler = delegate (object? sender, GrabbedEventArgs args) {
                            if (_captureSession.IsCurrent(generation)) {
                                QueueCapturedFrame(args.GrabResult, DateTime.Now, generation);
                            }
                        };
                        _device.StreamGrabber.ImageGrabbed += _imageGrabbedHandler;
                        if (!_device.GrabUsingGrabLoopThread()) {
                            _captureSession.Stop();
                            _device.StreamGrabber.ImageGrabbed -= _imageGrabbedHandler;
                            _imageGrabbedHandler = null;
                            _device.Close();
                            _noReadGate?.Dispose();
                            _noReadGate = null;
                            _frameDispatcher?.Dispose();
                            _frameDispatcher = null;
                            return new KeyValuePair<bool, string>(false, "开启码流失败");
                        }
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
            catch (Exception exception) {
                _captureSession.Stop();
                _noReadGate?.Dispose();
                _noReadGate = null;
                if (_device is not null && _imageGrabbedHandler is not null) {
                    _device.StreamGrabber.ImageGrabbed -= _imageGrabbedHandler;
                    _imageGrabbedHandler = null;
                }
                _frameDispatcher?.Dispose();
                _frameDispatcher = null;
                OnCameraExceptionOccurred(new CameraExceptionEventArgs { Exception = exception });
                return new KeyValuePair<bool, string>(false, exception.Message);
            }
            finally {
                _lifecycleGate.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await _lifecycleGate.WaitAsync();
            try {
                _captureSession.Stop();
                _noReadGate?.Dispose();
                _noReadGate = null;
                if (_device is not null) {
                    if (_imageGrabbedHandler is not null) {
                        _device.StreamGrabber.ImageGrabbed -= _imageGrabbedHandler;
                        _imageGrabbedHandler = null;
                    }
                    var stopped = !_device.IsGrabbing || _device.ShutdownGrab();
                    _frameDispatcher?.Dispose();
                    _frameDispatcher = null;
                    var closed = !_device.IsOpen || _device.Close();
                    if (!stopped || !closed) {
                        Status = CameraStatus.Failure;
                        return new KeyValuePair<bool, string>(false, "停止华睿相机采集或关闭设备失败");
                    }
                    OnCameraStopped(new CameraStoppedEventArgs() {
                        CameraInfo = this.Info
                    });
                }
                return new KeyValuePair<bool, string>(true, "设备停止成功!");
            }
            finally {
                _lifecycleGate.Release();
            }
        }

        public async Task ApplySettingsAsync(CameraRuntimeSettings settings, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        /// <summary>按照配置开启软件或硬件触发，连续采集时关闭触发。</summary>
        private void ConfigureTrigger(ITrigger trigger) {
            var configured = IsUseTriggerMode
                ? trigger.Open(TriggerMode == TriggerMode.Software ? TriggerSourceEnum.Software : TriggerSourceEnum.Line1)
                : trigger.Close();
            if (!configured) {
                throw new InvalidOperationException("华睿相机触发参数设置失败");
            }
        }

        /// <summary>读取相机当前触发激活方式，决定是否需要把连续图像合并为一个触发帧组。</summary>
        private void ReadTriggerActivation(IParameterCollection parameters) {
            _isLevelTrigger = false;
            if (!IsUseTriggerMode || TriggerMode != TriggerMode.Hardware) {
                return;
            }
            try {
                using var activation = parameters[ParametrizeNameSet.TriggerActivation];
                var value = activation.GetValue()?.ToString();
                _isLevelTrigger = IsLevelTriggerActivation(value);
                NLog.LogManager.GetCurrentClassLogger().Info(
                    $"华睿触发激活方式:相机={Info?.SerialNumber},TriggerActivation={value ?? "未知"}," +
                    $"连续帧汇总={_isLevelTrigger}");
            }
            catch (Exception exception) {
                NLog.LogManager.GetCurrentClassLogger().Warn(
                    exception,
                    $"读取华睿触发激活方式失败:相机={Info?.SerialNumber};按单帧触发处理");
            }
        }

        /// <summary>判断厂商触发激活值是否表示高电平或低电平持续触发。</summary>
        private static bool IsLevelTriggerActivation(string? value) =>
            string.Equals(value, "LevelHigh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "LevelLow", StringComparison.OrdinalIgnoreCase);

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
                frame => {
                    if (_captureSession.IsCurrent(frame.Generation)) {
                        GrabResultDecodeCore(frame.RawData, frame.ScanTime, frame.FrameNo, frame.Generation);
                    }
                },
                (_, exception) => OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = new Exception("后台处理华睿扫码帧异常", exception)
                }));
        }

        /// <summary>创建华睿电平触发帧组门控器，并在对应采集会话有效时发布汇总 NoRead。</summary>
        private void EnsureNoReadGate() {
            _noReadGate ??= new TriggerBurstNoReadGate(
                TriggerFrameQuietPeriod,
                PublishBufferedNoRead,
                exception => OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = new Exception("发布华睿触发帧组NoRead失败", exception)
                }));
        }

        /// <summary>发布已经确认整轮没有原始条码结果的华睿 NoRead。</summary>
        private void PublishBufferedNoRead(int generation, BarcodeReadEventArgs args) {
            if (!_captureSession.IsCurrent(generation) || !_isLevelTrigger ||
                !IsUseTriggerMode || TriggerMode != TriggerMode.Hardware || IsHideNoRead ||
                NotBarcodeHitEvent is null) {
                ReleaseEventImages(args);
                return;
            }
            OnNotBarcodeHitEvent(args);
        }

        /// <summary>在 SDK 回调入口立即克隆帧，并以原始观测时间无等待入队。</summary>
        private void QueueCapturedFrame(IGrabbedRawData rawData, DateTime scanTime, int generation) {
            if (!_captureSession.IsCurrent(generation)) {
                return;
            }
            try {
                var frame = new DaHuaCapturedFrame(
                    rawData.Clone(),
                    scanTime,
                    Interlocked.Increment(ref _frameNo), generation);
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
            GrabResultDecodeCore(grabbedRawData, scanTime, frameNo, 0);
        }

        /// <summary>解析一帧华睿 SDK 数据，并按实际触发方式决定立即或汇总发布 NoRead。</summary>
        private void GrabResultDecodeCore(
            IGrabbedRawData grabbedRawData,
            DateTime scanTime,
            long frameNo,
            int generation) {
            Bitmap? bitmap = null;
            try {
                var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
                var barcodeInfo = new List<DaHuaBarcodeInfo>();
                var hasSdkBarcode = false;
                var hasDecodeResult = false;
                var chunkData = grabbedRawData.ChunkData;
                for (var i = 0; i < chunkData.ChunkCount; i++) {
                    uint chunkNumber = 0;
                    var vecChunkInfos = new List<string>();
                    chunkData.GetChunkDataByIndex((uint)i, ref chunkNumber, ref vecChunkInfos);
                    var sdkCodeCount = ParseChunkData(chunkNumber, vecChunkInfos, scanTime, barcodeInfo);
                    hasSdkBarcode |= sdkCodeCount > 0;
                    hasDecodeResult |= sdkCodeCount >= 0;
                }

                var canReportNoRead = !IsHideNoRead && IsUseTriggerMode &&
                                      TriggerMode == TriggerMode.Hardware && NotBarcodeHitEvent is not null;
                var aggregateNoRead = canReportNoRead && _isLevelTrigger;
                var noReadGate = aggregateNoRead ? _noReadGate : null;
                if (aggregateNoRead && noReadGate is not null && hasDecodeResult && hasSdkBarcode) {
                    noReadGate!.Observe(generation, true, null);
                }
                var noReadConsumer = hasDecodeResult && !hasSdkBarcode && canReportNoRead &&
                                     (!aggregateNoRead || noReadGate is not null);
                var barcodeConsumerCount = BarcodeReadTriggered is null ? 0 : barcodeInfo.Count;
                if (hasSdkBarcode && barcodeInfo.Count == 0) {
                    FilteredBarcodeReturned?.Invoke(this, new BarcodeReadEventArgs {
                        Barcode = "filtered", IsFiltered = true,
                        CameraSerialNumber = Info?.SerialNumber ?? string.Empty,
                        ScanTime = scanTime, Timestamp = timestamp, FrameNo = frameNo
                    });
                }
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
                    var candidate = new BarcodeReadEventArgs {
                            Timestamp = timestamp,
                            Barcode = "NoRead",
                            Image = bitmap,
                            ThumbImage = thumbnailImage,
                            CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                            ScanTime = scanTime,
                            FrameNo = frameNo
                    };
                    if (aggregateNoRead) {
                        noReadGate!.Observe(generation, false, candidate);
                    }
                    else {
                        OnNotBarcodeHitEvent(candidate);
                    }
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
        private int ParseChunkData(
            uint chunkType,
            List<string> values,
            DateTime scanTime,
            List<DaHuaBarcodeInfo> output) {
            var parsed = new Dictionary<int, DaHuaBarcodeInfo>();
            // 未携带解码计数字段的纯图像不能当作一次读码失败。
            var codeCount = -1;
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
            return codeCount;
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
            Status = CameraStatus.Paused;
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

        /// <summary>释放未发布事件持有的原图和缩略图。</summary>
        private static void ReleaseEventImages(BarcodeReadEventArgs args) {
            args.Image?.Dispose();
            if (!ReferenceEquals(args.Image, args.ThumbImage)) {
                args.ThumbImage?.Dispose();
            }
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
