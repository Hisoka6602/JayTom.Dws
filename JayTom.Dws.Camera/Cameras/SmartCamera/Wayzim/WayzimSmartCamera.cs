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
        private readonly BarCodeFilterContainer _barCodeFilterContainer = new();

        //相机对象
        private CameraDataService? _cameraDataService;

        /// <summary>
        /// 固定端口
        /// </summary>
        public WayzimSmartCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.SmartCamera;
        }

        public WayzimSmartCamera() {
        }

        public async void Dispose() {
            await Stop();
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

                _cameraDataService = GWCameraService.GetCameraInstance(Info?.Id ?? 0, ReaultCallBack, null, ref errorMsg, port);
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
        private async void ReaultCallBack(ResultInfoStruct infostruct, object tag) {
            Bitmap? bitmap = null;
            Image? thumbnailImage = null;
            var localTime = DateTimeOffset.Now.ToLocalTime();
            var timestamp = localTime.ToUnixTimeMilliseconds();
            //解析图片
            if (infostruct.ImageInfo is { Size: > 0, ImageType: ImageTypes.JPEG }) {
                bitmap = await ConvertByteArrayToBitmapAsync(infostruct.ImageInfo.ImageBytes);
                thumbnailImage = this.GenerateThumbnail(bitmap);
                //画边框
                if (IsShowBarcodeBorder && thumbnailImage is not null && bitmap is not null &&
                    thumbnailImage.PixelFormat != PixelFormat.Format8bppIndexed &&
                    infostruct.CodeInfo.CodeInfos?.Any() == true) {
                    using var g = Graphics.FromImage(thumbnailImage);
                    foreach (var convertPoint in infostruct.CodeInfo.CodeInfos.Select(ConvertPoint)) {
                        int.TryParse(infostruct.CodeInfo.ResolutionX, out var imageWidth);
                        int.TryParse(infostruct.CodeInfo.ResolutionY, out var imageHeight);
                        var points = new Point[4];
                        for (var j = 0; j < 4; ++j) {
                            points[j].X = (int)(convertPoint[j].X *
                                                ((float)(thumbnailImage.Size.Width) / (imageWidth > 0 ? imageWidth : 1)));
                            points[j].Y = (int)(convertPoint[j].Y *
                                                ((float)(thumbnailImage.Size.Height) / (imageHeight > 0 ? imageHeight : 1)));
                        }
                        g.DrawPolygon(new Pen(BarcodeBorderColor, BarcodeBorderSize), points);
                    }
                }
            }
            if (infostruct.CodeInfo.CodeInfos?.Any() == true) {
                //扫到条码

                foreach (var codeInfo in infostruct.CodeInfo.CodeInfos) {
                    //过滤
                    var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                        BarCode = string.IsNullOrWhiteSpace(codeInfo.Code) ? "NoRead" : codeInfo.Code,
                        ScanTime = DateTime.Now
                    });
                    if (validateData) {
                        //返回条码
                        OnBarcodeReadTriggered(new BarcodeTriggeredEventArgs() {
                            Timestamp = timestamp,
                            Barcode = string.IsNullOrWhiteSpace(codeInfo.Code) ? "NoRead" : codeInfo.Code,
                            Image = bitmap,
                            ThumbImage = (Bitmap?)thumbnailImage,
                            CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                            ScanTime = DateTime.Now,
                            AreaCoords = ConvertPoint(codeInfo)
                        });
                    }
                }
            }
            else {
                //未扫到条码
                OnNotBarcodeHitEvent(new BarcodeReadEventArgs() {
                    Timestamp = timestamp,
                    Barcode = "NoRead",
                    Image = bitmap,
                    ThumbImage = (Bitmap?)thumbnailImage,
                    CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                    ScanTime = DateTime.Now
                });
            }
            if (IsRealtimeImageEnabled) {
                OnRealtimeImage(new RealtimeImageEventArgs() {
                    Timestamp = timestamp,
                    ThumbImage = (Bitmap?)thumbnailImage,
                });
            }
            await Task.Delay(5);
            infostruct.CodeInfo = default;
        }

        private Bitmap? ConvertByteArrayToBitmap(byte[] imageData) {
            Image img;
            using (var ms = new MemoryStream()) {
                ms.Write(imageData, 0, imageData.Length);
                ms.Seek(0, SeekOrigin.Begin);
                try {
                    img = Image.FromStream(ms, true);
                }
                catch (Exception ex) { img = null; }
            }
            return (Bitmap?)img;
        }

        private async Task<Bitmap?> ConvertByteArrayToBitmapAsync(byte[] imageData) {
            Bitmap? bitmap = null;
            using var ms = new MemoryStream();
            await ms.WriteAsync(imageData, 0, imageData.Length);
            ms.Seek(0, SeekOrigin.Begin);

            try {
                var image = await Task.FromResult(Image.FromStream(ms, true));
                bitmap = (Bitmap?)image;
            }
            catch (Exception ex) {
                bitmap = null;
            }
            finally {
                Array.Clear(imageData, 0, imageData.Length);
            }

            return bitmap;
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
        public IOcr Ocr { get; set; }

        public int BarcodeBorderSize { get; set; } = 5;
        public Color BarcodeBorderColor { get; set; } = Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;
        public bool IsUseTriggerMode { get; set; } = true;
        public TriggerMode TriggerMode { get; set; } = TriggerMode.Hardware;

        public void SoftwareTriggerOnce() {
            //
        }

        public event EventHandler<BarcodeTriggeredEventArgs>? BarcodeReadTriggered;

        public event EventHandler<BarcodeReadEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<OcrContentRecognizedEventArgs>? OcrContentRecognized;

        public void SetScanCodeFilterParams(ScanCodeFilterParams @params) {
            _barCodeFilterContainer.Pattern = @params.RegularExpression;
            _barCodeFilterContainer.MaxSize = @params.DuplicateBarcodeFilterCount;
            _barCodeFilterContainer.ExpirationTime = TimeSpan.FromMilliseconds(@params.ScanInterval);
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

        protected virtual async void OnRealtimeImage(RealtimeImageEventArgs e) {
            await Task.Yield();
            RealtimeImage?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeReadTriggered(BarcodeTriggeredEventArgs e) {
            await Task.Yield();
            BarcodeReadTriggered?.Invoke(this, e);
        }

        protected virtual async void OnNotBarcodeHitEvent(BarcodeReadEventArgs e) {
            await Task.Yield();
            NotBarcodeHitEvent?.Invoke(this, e);
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

        public static Image? GenerateThumbnail1(Image? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            if (sourceImage is null) {
                return null;
            }
            var thumbnail = new Bitmap(thumbnailWidth, thumbnailHeight);

            using (var graphics = Graphics.FromImage(thumbnail)) {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Low;

                var scaleX = (float)thumbnailWidth / sourceImage.Width;
                var scaleY = (float)thumbnailHeight / sourceImage.Height;
                var scale = Math.Min(scaleX, scaleY);

                var scaledWidth = (int)(sourceImage.Width * scale);
                var scaledHeight = (int)(sourceImage.Height * scale);

                var startX = (thumbnailWidth - scaledWidth) / 2;
                var startY = (thumbnailHeight - scaledHeight) / 2;

                graphics.DrawImage(sourceImage, startX, startY, scaledWidth, scaledHeight);
            }

            return thumbnail;
        }
    }
}