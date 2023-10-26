using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Cameras.VolumeCamera.Irayple {

    public class DaHuaVolumeCamera : IVolumeCamera {
        private IntPtr? _handle;

        public async void Dispose() {
            await Stop();
            if (_handle is null || _handle == IntPtr.Zero) {
                return;
            }
            Volume3DSdk.vslbVolume3DFini(_handle.Value);
            Volume3DSdk.vslbVolume3DDestroy(_handle.Value);
            _handle = null;
        }

        public CameraInfo? Info { get; }
        public SdkType SdkType { get; }
        public string SdkName { get; }
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; }
        public CameraBindingType BindingType { get; set; }

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
            try {
                _handle = Volume3DSdk.vslbVolume3DCreate();
                if (_handle is null || _handle == IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "创建句柄失败");
                }
                var ret = Volume3DSdk.vslbVolume3DInit(_handle.Value, null);
                if (ret != 0) {
                    Volume3DSdk.vslbVolume3DDestroy(_handle.Value);
                    return new KeyValuePair<bool, string>(false, "初始化失败");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
            return new KeyValuePair<bool, string>(true, $"初始化成功");
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            await Task.Yield();
            try {
                if (_handle is null || _handle == IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "相机句柄无效");
                }

                var runEx = Volume3DSdk.vslbVolume3DRunEx(_handle.Value, resultPtr => {
                    object obj = Marshal.PtrToStructure(resultPtr, typeof(Volume3DSdk.SVolumeResult)) ?? new object();
                    Volume3DSdk.SVolumeResult ret = (Volume3DSdk.SVolumeResult)obj;
                    if (ret.state == 1) {
                        OnVolumeCaptured(new VolumeCapturedEventArgs() {
                            Timestamp = DateTime.Now,
                            Length = ret.length,
                            Volume = ret.volume,
                            Width = ret.width,
                            Height = ret.height,
                        });
                    }
                    //获取体积
                    //获取图像
                });
                if (runEx != 0) {
                    return new KeyValuePair<bool, string>(false, "启动失败!");
                }
                return new KeyValuePair<bool, string>(false, "启动成功!");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, $"{e}");
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await Task.Yield();
            if (_handle is null || _handle == IntPtr.Zero) {
                return new KeyValuePair<bool, string>(false, "相机句柄无效");
            }
            else {
                Volume3DSdk.vslbVolume3DStop(_handle.Value);
                return new KeyValuePair<bool, string>(true, "停止成功");
            }
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public bool IsRealtimeImageEnabled { get; }

        public void StartRealTimeImage() {
            throw new NotImplementedException();
        }

        public void StopRealTimeImage() {
            throw new NotImplementedException();
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            throw new NotImplementedException();
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            throw new NotImplementedException();
        }

        public int TakePhotoDelay { get; set; }

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        protected async void OnVolumeCaptured(VolumeCapturedEventArgs e) {
            await Task.Yield();
            VolumeCaptured?.Invoke(this, e);
        }
    }
}