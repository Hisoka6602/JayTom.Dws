using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.Attributes.CameraAttributes;

namespace JayTom.Dws.Camera.Cameras.VolumeCamera.Irayple {

    public class DaHuaVolumeCamera : IVolumeCamera {
        private IntPtr? _handle;

        private readonly Volume3DSdk.VslbVolumeResultCB _resultCb;
        private MeasurementTriggerMode _measurementTriggerMode = MeasurementTriggerMode.Continuous;

        public void Dispose() {
            Stop().GetAwaiter().GetResult();
            if (_handle is null || _handle == IntPtr.Zero) {
                return;
            }
            Volume3DSdk.vslbVolume3DFini(_handle.Value);
            Volume3DSdk.vslbVolume3DDestroy(_handle.Value);
            _handle = null;
            OnCameraDisconnected(new CameraConnectionEventArgs() {
                CameraInfo = this.Info
            });
        }

        public DaHuaVolumeCamera() {
            _resultCb = (l, w, h, v) => {
                OnVolumeCaptured(new VolumeCapturedEventArgs() {
                    Timestamp = DateTime.Now,
                    Length = l,
                    Volume = v,
                    Width = w,
                    Height = h,
                });
            };
        }

        public DaHuaVolumeCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.ThreeDCamera;
            _resultCb = (l, w, h, v) => {
                OnVolumeCaptured(new VolumeCapturedEventArgs() {
                    Timestamp = DateTime.Now,
                    Length = l,
                    Volume = v,
                    Width = w,
                    Height = h,
                });
            };
        }

        public CameraInfo? Info { get; }
        public SdkType SdkType => SdkType.VolumeCameraSdk;
        public string SdkName => "Volume3DSdk";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.VolumeCamera;

        public Task<List<CameraInfo>?> EnumerateCameras() {
            return Task.FromResult(new List<CameraInfo>())!;
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
            if (_handle is not null && _handle != IntPtr.Zero) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }
            try {
                if (_handle is null || _handle == IntPtr.Zero) {
                    _handle = Volume3DSdk.vslbVolume3DCreate();
                    if (_handle == IntPtr.Zero) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("创建句柄失败")
                        });
                        return new KeyValuePair<bool, string>(false, "创建句柄失败");
                    }
                    var ret = Volume3DSdk.vslbVolume3DInit(_handle.Value, null);
                    if (ret != 0) {
                        Volume3DSdk.vslbVolume3DDestroy(_handle.Value);
                        return new KeyValuePair<bool, string>(false, $"初始化失败:{ret}");
                    }
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
            OnCameraInitialized(new CameraInitializedEventArgs() {
                CameraInfo = this.Info
            });
            return new KeyValuePair<bool, string>(true, $"初始化成功");
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            await Task.Yield();
            try {
                if (Status == CameraStatus.Running) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("相机运行中")
                    });
                    return new KeyValuePair<bool, string>(false, "相机运行中");
                }
                if (_handle is null || _handle == IntPtr.Zero) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("相机句柄无效")
                    });
                    return new KeyValuePair<bool, string>(false, "相机句柄无效");
                }

                var runEx = Volume3DSdk.vslbVolume3DRun(_handle.Value, _resultCb);
                if (runEx != 0) {
                    return new KeyValuePair<bool, string>(false, "启动失败!");
                }
                OnCameraStarted(new CameraStartedEventArgs() {
                    CameraInfo = this.Info,
                    Camera = this
                });
                return new KeyValuePair<bool, string>(true, "启动成功!");
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await Task.Yield();
            if (_handle is null || _handle == IntPtr.Zero) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("相机句柄无效")
                });
                return new KeyValuePair<bool, string>(false, "相机句柄无效");
            }
            else {
                Volume3DSdk.vslbVolume3DStop(_handle.Value);
                OnCameraStopped(new CameraStoppedEventArgs() {
                    CameraInfo = this.Info
                });
                return new KeyValuePair<bool, string>(true, "停止成功");
            }
        }

        public void SetParameters(Dictionary<string, object> parameters) {
        }

        public bool IsRealtimeImageEnabled { get; } = false;

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

        public Task TriggerMeasurementPhotoAsync(string barcode, long barcodeTimestamp, int delay, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        protected void OnVolumeCaptured(VolumeCapturedEventArgs e) {
            VolumeCaptured?.Invoke(this, e);
        }

        protected virtual void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e) {
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual void OnCameraStopped(CameraStoppedEventArgs e) {
            Status = CameraStatus.Paused;
            CameraStopped?.Invoke(this, e);
        }

        protected virtual void OnCameraDisconnected(CameraConnectionEventArgs e) {
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
        }
    }
}
