using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Management;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera.Cameras.VolumeCamera.Dimension {

    public class DimensionVolumeCamera : IVolumeCamera {
        private DimensionVolumeSdk? _dimensionVolumeSdk = null;
        private SemaphoreSlim _volumelim = new(1);
        private static MeasurementTriggerMode _measurementTriggerMode = MeasurementTriggerMode.Continuous;

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        public void Dispose() {
            _dimensionVolumeSdk?.Dispose();
        }

        public CameraInfo? Info { get; private set; }
        public SdkType SdkType { get; private set; } = SdkType.VolumeCameraSdk;
        public string SdkName => "DimensionVolume";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.VolumeCamera;

        public DimensionVolumeCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.ThreeDCamera;
            if (_dimensionVolumeSdk is null) {
                _dimensionVolumeSdk = new();
                _dimensionVolumeSdk.VolumeCaptured += async delegate (object? sender, DimensionVolumeInfo info) {
                    try {
                        await _volumelim.WaitAsync();
                        var thumbnailImage = GenerateThumbnail(info.Image, 640, 480);
                        OnVolumeCaptured(new VolumeCapturedEventArgs() {
                            Length = info.Length,
                            Width = info.Width,
                            Height = info.Height,
                            Image = info.Image,
                            Thumbnail = thumbnailImage,
                            Volume = info.Length * info.Width * info.Height,
                            Timestamp = DateTime.Now,
                            CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty
                        });
                    }
                    catch (Exception e) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    }
                    finally {
                        _volumelim.Release();
                    }
                };
            }
        }

        public DimensionVolumeCamera() {
        }

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            var cameras = new List<CameraInfo>();

            try {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera'");
                var devices = searcher.Get();
                var i = 0;
                foreach (var device in devices) {
                    if (device["Caption"]?.ToString()?.Contains("Orbbec") == true &&
                        device["Caption"]?.ToString()?.Contains("3D Camera") == true &&
                        device["Status"]?.ToString()?.Equals("OK") == true) {
                        //取Guid
                        var guid = device["ClassGuid"]?.ToString();
                        if (!string.IsNullOrEmpty(guid)) {
                            var cameraInfo = new CameraInfo() {
                                Brand = "量方",
                                Model = device["Caption"]?.ToString() ?? string.Empty,
                                SerialNumber = guid,
                                Name = device["Caption"]?.ToString() ?? string.Empty,
                                Type = CameraType.ThreeDCamera,
                                ConnectionType = CameraConnectionType.Usb,
                                Id = i,
                                SupportedBindingType = CameraBindingType.VolumeCamera
                            };
                            _devInfo.AddOrUpdate(cameraInfo.SerialNumber, cameraInfo, (k, v) => cameraInfo);
                            cameras.Add(cameraInfo);
                        }
                    }

                    i++;
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }

            return cameras;
        }

        public event EventHandler<CameraExceptionEventArgs>? CameraExceptionOccurred;

        public event EventHandler<CameraConnectionEventArgs>? CameraDisconnected;

        public event EventHandler<CameraInitializedEventArgs>? CameraInitialized;

        public event EventHandler<CameraStartedEventArgs>? CameraStarted;

        public event EventHandler<CameraStoppedEventArgs>? CameraStopped;

        public event EventHandler<CameraUnregisteredEventArgs>? CameraUnregistered;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        public async Task<KeyValuePair<bool, string>> Initialize(object param) {
            if (param is CameraInfo cameraInfo) {
                this.Info = cameraInfo;
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    var (key, value) = await _dimensionVolumeSdk.Initialize();
                    Status = key ? CameraStatus.Initialized : CameraStatus.Uninitialized;

                    return new KeyValuePair<bool, string>(key, key ? "初始化成功" : "初始化失败");
                }
                else {
                    return new KeyValuePair<bool, string>(false, "设备不在线!");
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
            if (Status == CameraStatus.Initialized) {
                return new KeyValuePair<bool, string>(true, "不支持实时");
                _dimensionVolumeSdk.StartVolumeCapture();
                OnCameraStarted(new CameraStartedEventArgs() {
                    CameraInfo = this.Info
                });
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            return new KeyValuePair<bool, string>(false, "未初始化");
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await _dimensionVolumeSdk.StopVolumeCapture();
            OnCameraStopped(new CameraStoppedEventArgs() {
                CameraInfo = this.Info
            });
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public void SetParameters(Dictionary<string, object> parameters) {
        }

        public bool IsRealtimeImageEnabled => false;

        public void StartRealTimeImage() {
        }

        public void StopRealTimeImage() {
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public int TakePhotoDelay { get; set; }

        public MeasurementTriggerMode MeasurementTriggerMode {
            get => _measurementTriggerMode;
            set => _measurementTriggerMode = value;
        }

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        public async Task TriggerMeasurementPhotoAsync(string barcode, long barcodeTimestamp, int delay, CancellationToken cancellation = default) {
            if (_dimensionVolumeSdk is not null) {
                if (MeasurementTriggerMode == MeasurementTriggerMode.Single) {
                    await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellation);
                    await _dimensionVolumeSdk.TriggerMeasurementPhotoAsync(cancellation);
                }
            }
        }

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnCameraStarted(CameraStartedEventArgs e) {
            await Task.Yield();
            CameraStarted?.Invoke(this, e);
        }

        protected virtual async void OnCameraStopped(CameraStoppedEventArgs e) {
            await Task.Yield();
            CameraStopped?.Invoke(this, e);
        }

        protected virtual async void OnVolumeCaptured(VolumeCapturedEventArgs e) {
            await Task.Yield();
            VolumeCaptured?.Invoke(this, e);
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