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

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Irayple {

    public class DaHuaSmartCamera : ISmartCamera {

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        //过滤器
        private readonly BarCodeFilterContainer _barCodeFilterContainer = new();

        private long _frameNo = 0;

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

        public DaHuaSmartCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.SmartCamera;
        }

        public DaHuaSmartCamera() {
        }

        public async void Dispose() {
            await Stop();
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
                    _device = Enumerator.GetDeviceByIndex(devInfo.Id);
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
                            //设置缓存个数为8(默认值为16)
                            _device.StreamGrabber.SetBufferCount(8);
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
                    //码流回调事件
                    _device.StreamGrabber.ImageGrabbed += async delegate (object? sender, GrabbedEventArgs args) {
                        await Task.Yield();
                        //解码
                        GrabResultDecode(args.GrabResult);
                    };
                    OnCameraStarted(new CameraStartedEventArgs() {
                        CameraInfo = this.Info
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
                _device?.Close();
                //_device?.Dispose();
                _device = null;
                OnCameraDisconnected(new CameraConnectionEventArgs() {
                    CameraInfo = this.Info
                });
            }
            return new KeyValuePair<bool, string>(false, "设备停止成功!");
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

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            Task.Run(() => {
                _device?.TriggerSet?.ExecuteSoftwareTrigger();
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
        public Color BarcodeBorderColor { get; set; } = Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;
        public bool IsUseTriggerMode { get; set; } = true;
        public TriggerMode TriggerMode { get; set; } = TriggerMode.Hardware;
        public int SourceLine { get; set; }

        public async void SoftwareTriggerOnce() {
            await Task.Yield();
            _device?.TriggerSet?.ExecuteSoftwareTrigger();
        }

        public event EventHandler<BarcodeTriggeredEventArgs>? BarcodeReadTriggered;

        public event EventHandler<BarcodeReadEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<OcrResult>? OcrContentRecognized;

        public void SetScanCodeFilterParams(ScanCodeFilterParams @params) {
            _barCodeFilterContainer.Pattern = @params.RegularExpression;
            _barCodeFilterContainer.MaxSize = @params.DuplicateBarcodeFilterCount;
            _barCodeFilterContainer.ExpirationTime = TimeSpan.FromMilliseconds(@params.ScanInterval);
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="grabbedRawData"></param>
        private async void GrabResultDecode(IGrabbedRawData grabbedRawData) {
            await Task.Yield();
            try {
                var scanTime = DateTime.Now;
                var localTime = new DateTimeOffset(scanTime).ToLocalTime();
                var timestamp = localTime.ToUnixTimeMilliseconds();
                var barcodeInfo = new ConcurrentQueue<DaHuaBarcodeInfo>();
                ConcurrentDictionary<uint, List<string>> chunkDataInfos = new();
                var chunkData = grabbedRawData.ChunkData;
                for (var i = 0; i < chunkData.ChunkCount; i++) {
                    uint chunkId = 0;
                    // 一维码 0x80000000 == chunkId || 二维码  0x80000001 == chunkId
                    var vecChunkInfos = new List<string>();
                    chunkData.GetChunkDataByIndex((uint)i, ref chunkId, ref vecChunkInfos);
                    chunkDataInfos.TryAdd(chunkId, vecChunkInfos);
                }

                //图片
                var bitmap = grabbedRawData.ToBitmap(true);
                var thumbnailImage = this.GenerateThumbnail(bitmap);
                if (chunkDataInfos.Any()) {
                    foreach (var dataInfo in chunkDataInfos) {
                        // 一维码 0x80000000 == chunkId || 二维码  0x80000001 == chunkId

                        int.TryParse(Regex.Match(dataInfo.Value.FirstOrDefault(v => Regex.IsMatch(v,
                                @"(?:BarCodeNum|QRNum)\s+Value:(\d+)")) ?? string.Empty,
                            @"(?:BarCodeNum|QRNum)\s+Value:(\d+)")?.Groups[1]?.Value, out var codeCount);

                        var codeList = dataInfo.Value.Where(w =>
                                Regex.IsMatch(w, @"(?:Code|QR)(\d+)_CodeData\s+Value:(.+)") &&
                                int.Parse(Regex.Match(w, @"\d+").Value) < codeCount)
                            .Select(code =>
                                Regex.Match(code, @"(?:Code|QR)(\d+)_CodeData\s+Value:(.+)")?.Groups[0]?.Value)
                            .ToList();

                        var pointList = dataInfo.Value.Where(w =>
                                Regex.IsMatch(w, @"(?:Code|QR)\d+_Point\d+_(\w+)\s+Value:(\d+)") &&
                                int.Parse(Regex.Match(w, @"\d+").Value) < codeCount)
                            .ToList();
                        foreach (int i in Enumerable.Range(0, codeCount)) {
                            var daHuaBarcodeInfo = new DaHuaBarcodeInfo {
                                BarcodeType = dataInfo.Key == 0x80000000 ? CodeType.BarCode : CodeType.QrCode,
                                BarCode = codeList?
                                    .Select(input => Regex.Match(input ?? string.Empty,
                                        @$"(?:Code|QR){i}_CodeData\s+Value:(.+)"))
                                    .FirstOrDefault(match => match.Success)?.Groups[1].Value ?? string.Empty
                            };
                            daHuaBarcodeInfo.BarcodeRegionCoordinates.AddRange(
                                Enumerable.Range(0, 4)
                                    .Select(j => {
                                        int.TryParse(pointList?
                                            .Select(input =>
                                                Regex.Match(input, $"(?:Code|QR){i}_Point{j}_X\\s+Value:(\\d+)"))
                                            .FirstOrDefault(match => match.Success)?.Groups[1].Value, out var x);

                                        int.TryParse(pointList?
                                            .Select(input =>
                                                Regex.Match(input, $"(?:Code|QR){i}_Point{j}_Y\\s+Value:(\\d+)"))
                                            .FirstOrDefault(match => match.Success)?.Groups[1].Value, out var y);

                                        return new Point(x, y);
                                    })
                            );
                            //过滤
                            var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                                BarCode = string.IsNullOrWhiteSpace(daHuaBarcodeInfo.BarCode)
                                    ? "NoRead"
                                    : daHuaBarcodeInfo.BarCode,
                                ScanTime = scanTime
                            });
                            if (validateData) {
                                barcodeInfo.Enqueue(daHuaBarcodeInfo);
                            }
                        }
                    }

                    //画边框
                    if (IsShowBarcodeBorder && thumbnailImage is not null && bitmap is not null &&
                        thumbnailImage.PixelFormat != PixelFormat.Format8bppIndexed &&
                        barcodeInfo?.Any() == true) {
                        using var g = Graphics.FromImage(thumbnailImage);
                        foreach (var huaBarcodeInfo in barcodeInfo) {
                            var points = new Point[4];
                            for (var j = 0; j < 4; ++j) {
                                points[j].X = (int)(huaBarcodeInfo.BarcodeRegionCoordinates[j].X *
                                                    ((float)(thumbnailImage.Size.Width) /
                                                     (_originalWidth <= 0 ? 1 : _originalWidth)));
                                points[j].Y = (int)(huaBarcodeInfo.BarcodeRegionCoordinates[j].Y *
                                                    ((float)(thumbnailImage.Size.Height) /
                                                     (_originalHeight <= 0 ? 1 : _originalHeight)));
                            }

                            g.DrawPolygon(new Pen(BarcodeBorderColor, BarcodeBorderSize), points);
                        }
                    }
                }

                if (barcodeInfo?.Any() != true) {
                    //返回触发但没有条码
                    if (IsUseTriggerMode && TriggerMode == TriggerMode.Hardware) {
                        OnNotBarcodeHitEvent(new BarcodeReadEventArgs() {
                            Timestamp = timestamp,
                            Barcode = "NoRead",
                            Image = bitmap,
                            ThumbImage = (Bitmap?)thumbnailImage,
                            CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                            ScanTime = scanTime,
                            FrameNo = _frameNo
                        });
                    }
                }
                else {
                    while (!barcodeInfo.IsEmpty) {
                        //返回条码
                        if (barcodeInfo.TryDequeue(out var barcode)) {
                            OnBarcodeReadTriggered(new BarcodeTriggeredEventArgs() {
                                Timestamp = timestamp,
                                Barcode = string.IsNullOrWhiteSpace(barcode.BarCode) ? "NoRead" : barcode.BarCode,
                                Image = bitmap,
                                ThumbImage = (Bitmap?)thumbnailImage,
                                CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                                ScanTime = scanTime,
                                AreaCoords = barcode.BarcodeRegionCoordinates,
                                FrameNo = _frameNo
                            });
                        }

                        await Task.Delay(1);
                    }
                }

                if (IsRealtimeImageEnabled) {
                    OnRealtimeImage(new RealtimeImageEventArgs() {
                        ThumbImage = (Bitmap?)thumbnailImage,
                        Timestamp = timestamp
                    });
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
            finally {
                _frameNo += 1;
            }
        }

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(CameraConnectionEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
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

        protected virtual async void OnCameraStopped(CameraStoppedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Disconnected;
            CameraStopped?.Invoke(this, e);
        }

        protected virtual async void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            await Task.Yield();
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

        protected virtual async void OnNotBarcodeHitEvent(BarcodeReadEventArgs e) {
            await Task.Yield();
            NotBarcodeHitEvent?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeReadTriggered(BarcodeTriggeredEventArgs e) {
            await Task.Yield();
            BarcodeReadTriggered?.Invoke(this, e);
        }

        protected virtual async void OnRealtimeImage(RealtimeImageEventArgs e) {
            await Task.Yield();
            RealtimeImage?.Invoke(this, e);
        }

        public static Image? GenerateThumbnail(Image? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            if (sourceImage is null) {
                return null;
            }
            // 创建目标缩略图的空白画布
            var thumbnail = new Bitmap(thumbnailWidth, thumbnailHeight);

            using var graphics = Graphics.FromImage(thumbnail);
            // 设置绘图质量参数
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.SmoothingMode = SmoothingMode.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.Low;

            // 计算缩放比例
            var scaleX = (float)thumbnailWidth / sourceImage.Width;
            var scaleY = (float)thumbnailHeight / sourceImage.Height;
            var scale = Math.Min(scaleX, scaleY);

            // 计算缩放后的宽度和高度
            var scaledWidth = (int)(sourceImage.Width * scale);
            var scaledHeight = (int)(sourceImage.Height * scale);

            // 计算在画布上居中绘制的起始位置
            var startX = (thumbnailWidth - scaledWidth) / 2;
            var startY = (thumbnailHeight - scaledHeight) / 2;

            // 绘制缩略图
            graphics.DrawImage(sourceImage, startX, startY, scaledWidth, scaledHeight);

            return thumbnail;
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
    }
}