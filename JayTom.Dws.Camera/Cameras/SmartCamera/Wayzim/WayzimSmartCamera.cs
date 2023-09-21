using System;
using CamSDK;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JayTom.Dws.Camera.FilterContainer;
using static JayTom.Dws.Camera.Cameras.SmartCamera.Irayple.DaHuaSmartCamera;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Wayzim {

    public class WayzimSmartCamera : ISmartCamera {
        private static SemaphoreSlim _readSlim = new(1);
        private static SemaphoreSlim _bindingSlim = new(1);

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        //过滤器
        private readonly BarCodeFilterContainer _barCodeFilterContainer = new();

        /// <summary>
        /// 固定端口
        /// </summary>
        public WayzimSmartCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.SmartCamera;
            if (this.Info.Name.Equals("t1")) {
                this.Info.Port = 51236;
            }
            else if (this.Info.Name.Equals("t2")) {
                this.Info.Port = 51237;
            }
            else if (this.Info.Name.Equals("t4")) {
                this.Info.Port = 51238;
            }
            else if (this.Info.Name.Equals("t3")) {
                this.Info.Port = 51239;
            }
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
            }
            return new KeyValuePair<bool, string>(true, "初始化成功");
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            try {
                await _bindingSlim.WaitAsync();
                await Task.Delay(50);
                if (Status == CameraStatus.Running) {
                    return new KeyValuePair<bool, string>(false, "设备已在运行中");
                }

                if (this.Info is not null) {
                    var errorMsg = string.Empty;
                    var recReaultInfo =
                        GWCameraService.RecReaultInfo(Info.Id, ReaultCallBack, null, ref errorMsg, this.Info.Port);
                    if (recReaultInfo) {
                        OnCameraInitialized(new CameraInitializedEventArgs() {
                            CameraInfo = this.Info,
                        });
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"{this.Info.Id}")
                        });
                    }
                    else {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"相机回调绑定失败:{JsonConvert.SerializeObject(this.Info)}")
                        });
                    }

                    return new KeyValuePair<bool, string>(recReaultInfo, errorMsg);
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
            await _readSlim.WaitAsync();
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception($"接收到数据:相机ID：{Info?.Id},相机端口:{Info.Port},相机序列号:{Info?.SerialNumber},相机IP:{infostruct.CodeInfo.CamIpAdr}")
            });
            Bitmap? bitmap = null;
            Image? thumbnailImage = null;
            var localTime = DateTimeOffset.Now.ToLocalTime();
            long timestamp = localTime.ToUnixTimeMilliseconds();
            //解析图片
            if (infostruct.ImageInfo is { Size: > 0, ImageType: ImageTypes.JPEG }) {
                bitmap = ConvertByteArrayToBitmap(infostruct.ImageInfo.ImageBytes);
                thumbnailImage = bitmap?.GetThumbnailImage(800, 600, () => false, IntPtr.Zero);
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

            await Task.Delay(5);
            _readSlim.Release();
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
                GWCameraService.Close();
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

        public bool IsRealtimeImageEnabled { get; } = false;

        public void StartRealTimeImage() {
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception("该SDK无实时图像函数")
            });
        }

        public void StopRealTimeImage() {
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception("该SDK无实时图像函数")
            });
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
    }
}