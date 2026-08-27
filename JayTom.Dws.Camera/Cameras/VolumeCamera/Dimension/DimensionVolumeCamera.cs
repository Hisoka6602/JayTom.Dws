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
                _dimensionVolumeSdk.VolumeCaptured += delegate (object? sender, DimensionVolumeInfo info) {
                    try {
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
                        info.Image?.Dispose();
                        NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
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

        public async Task<KeyValuePair<bool, string>> Initialize(CameraInfo param, CancellationToken cancellationToken = default) {
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

        public async Task<KeyValuePair<bool, string>> Start(CancellationToken cancellationToken = default) {
            await Task.Yield();
            if (Status == CameraStatus.Initialized) {
                return new KeyValuePair<bool, string>(true, "不支持实时");
                _dimensionVolumeSdk.StartVolumeCapture();
                OnCameraStarted(new CameraStartedEventArgs() {
                    CameraInfo = this.Info,
                    Camera = this
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

        public async Task ApplySettingsAsync(CameraRuntimeSettings settings, CancellationToken cancellationToken = default) {
        }

        public bool IsRealtimeImageEnabled => false;

        public void StartRealTimeImage() {
        }

        public void StopRealTimeImage() {
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, TimeSpan delay, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public int TakePhotoDelay { get; set; }

        public MeasurementTriggerMode MeasurementTriggerMode {
            get => _measurementTriggerMode;
            set => _measurementTriggerMode = value;
        }

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        public async Task TriggerMeasurementPhotoAsync(string barcode, long packageTimestampMilliseconds, int delay, CancellationToken cancellation = default) {
            if (_dimensionVolumeSdk is not null) {
                if (MeasurementTriggerMode == MeasurementTriggerMode.Single) {
                    await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellation);
                    await _dimensionVolumeSdk.TriggerMeasurementPhotoAsync(cancellation);
                }
            }
        }

        protected virtual void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e) {
            CameraStarted?.Invoke(this, e);
        }

        protected virtual void OnCameraStopped(CameraStoppedEventArgs e) {
            CameraStopped?.Invoke(this, e);
        }

        protected virtual void OnVolumeCaptured(VolumeCapturedEventArgs e) {
            var handler = VolumeCaptured;
            if (handler is null) {
                e.Image?.Dispose();
                e.Thumbnail?.Dispose();
                return;
            }
            handler.Invoke(this, e);
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }
    }
}
