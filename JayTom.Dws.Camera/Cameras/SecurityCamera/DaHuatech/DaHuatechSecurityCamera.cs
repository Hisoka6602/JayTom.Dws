using System;
using NetSDKCS;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using Image = System.Drawing.Image;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech {

    public class DaHuatechSecurityCamera : ISecurityCamera {
        private BaseDaHuatech _baseDaHuatech = BaseDaHuatech.CreateInstance();
        private SemaphoreSlim _snapRevPhotoSlim = new(1);
        private ConcurrentQueue<ImageMessageInfo> _imageMessageQueue = new();
        private SemaphoreSlim _takePhotoSlim = new(1);

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        public DaHuatechSecurityCamera(CameraInfo info) {
            this.Info = info;
        }

        public DaHuatechSecurityCamera() {
        }

        public async void Dispose() {
            await Stop();
            OnCameraDisconnected(new CameraConnectionEventArgs() {
                CameraInfo = this.Info
            });
            OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                CameraInfo = this.Info
            });
        }

        public CameraInfo? Info { get; private set; } = new();
        public SdkType SdkType => SdkType.SecurityCamera;
        public string SdkName => "NetSDKCS.dll";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.PanoramicCamera;
        public string CameraConnectionParameters { get; set; } = string.Empty;
        public int TakePhotoDelay { get; set; }

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            _devInfo.Clear();
            var cameraInfos = new List<CameraInfo>();
            var devices = await BaseDaHuatech.EnumDevices();
            if (devices?.Any() == true) {
                foreach (var cameraInfo in devices.Select(deviceNetInfoExe => new CameraInfo() {
                    Brand = "Dahua",
                    ConnectionType = CameraConnectionType.Ethernet,
                    IpAddress = deviceNetInfoExe.szIP,
                    Name = deviceNetInfoExe.szDevName,
                    Model = deviceNetInfoExe.szDetailType,
                    Port = deviceNetInfoExe.nPort,
                    SerialNumber = deviceNetInfoExe.szSerialNo,
                    Type = CameraType.VideoCamera,
                    Version = deviceNetInfoExe.szDevSoftVersion,
                    //IsAvailable = (s.Value.byInitStatus & 0x1) != 1
                })) {
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

        public async Task<KeyValuePair<bool, string>> Initialize(object param) {
            await Task.Yield();
            if (param is CameraInfo cameraInfo) {
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    this.Info = devInfo;
                    //注册各种事件
                    _baseDaHuatech.RegisterImageCallback(devInfo.SerialNumber, async imageBitmap => {
                        try {
                            await _snapRevPhotoSlim.WaitAsync();
                            var tryDequeue = _imageMessageQueue.TryDequeue(out var imageMessageInfo);
                            if (tryDequeue && imageMessageInfo is not null) {
                                var thumbnailImage = imageBitmap?.GetThumbnailImage(1024, 768, () => false, IntPtr.Zero);
                                OnPhotoTaken(new PhotoTakenEventArgs() {
                                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                                    Barcode = imageMessageInfo.Barcode,
                                    BarcodeTimestamp = imageMessageInfo.BarcodeTimestamp,
                                    CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                                    Image = imageBitmap,
                                    ThumbImage = (Bitmap?)thumbnailImage,
                                    PhotoTime = DateTime.Now,
                                });
                            }
                        }
                        catch (Exception e) {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = e
                            });
                        }
                        finally {
                            _snapRevPhotoSlim.Release();
                        }
                    });
                    OnCameraInitialized(new CameraInitializedEventArgs() {
                        CameraInfo = Info
                    });
                    return new KeyValuePair<bool, string>(false, "初始化成功!");
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
            //获取登录账号密码
            //登录设备
            try {
                var parameters = JsonConvert.DeserializeObject<SecurityCameraConnectionParameters>(CameraConnectionParameters);
                if (parameters is not null && Info is not null) {
                    var (key, value) = await _baseDaHuatech.LogIn(Info.SerialNumber,
                        parameters.Username, parameters.Password);

                    if (key) {
                        OnCameraStarted(new CameraStartedEventArgs() {
                            CameraInfo = Info
                        });
                    }
                    else {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception(value)
                        });
                    }
                    return new KeyValuePair<bool, string>(key, value);
                }
                else {
                    return new KeyValuePair<bool, string>(false, "登录参数错误");
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            //断开
            await Task.Yield();
            if (!string.IsNullOrEmpty(this.Info?.SerialNumber)) {
                var (key, value) = await _baseDaHuatech.LogOut(this.Info.SerialNumber);
                if (!key) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(value)
                    });
                }
                return new KeyValuePair<bool, string>(key, value);
            }
            return new KeyValuePair<bool, string>(false, "设备未连接");
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        public event EventHandler<RealPreviewEventArgs>? RealPreview;

        public Task<KeyValuePair<bool, string>> SaveStream(string filePath, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Zoom(double zoomFactor, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> ControlPtz(double panAngle, double tiltAngle, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> SetStepSize(int stepSize, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> SetFocalLength(double focalLength, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> SetAperture(double aperture, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> StartPreview(CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public void StopPreview(CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            Task.Run(async () => {
                await Task.Delay(TakePhotoDelay, cancellation);
                if (Status == CameraStatus.Running) {
                    try {
                        await Task.Delay(TakePhotoDelay, cancellation);
                        await _takePhotoSlim.WaitAsync(cancellation);
                        await Task.Delay(200, cancellation);
                        if (!string.IsNullOrEmpty(Info?.SerialNumber)) {
                            var (key, value) = await _baseDaHuatech.GetRealtimeImage(Info.SerialNumber);
                            if (!key) {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception(value)
                                });
                            }
                            _imageMessageQueue.Enqueue(new ImageMessageInfo() {
                                Barcode = barcode,
                                BarcodeTimestamp = barcodeTimestamp,
                            });
                        }
                    }
                    catch (Exception e) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = e
                        });
                    }
                    finally {
                        _takePhotoSlim.Release();
                    }
                }
            }, cancellation);
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            Task.Run(async () => {
                await Task.Delay(delay, cancellation);
                if (Status == CameraStatus.Running) {
                    try {
                        await Task.Delay(TakePhotoDelay, cancellation);
                        await _takePhotoSlim.WaitAsync(cancellation);
                        await Task.Delay(200, cancellation);
                        if (!string.IsNullOrEmpty(Info?.SerialNumber)) {
                            var (key, value) = await _baseDaHuatech.GetRealtimeImage(Info.SerialNumber);
                            if (!key) {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception(value)
                                });
                            }
                            _imageMessageQueue.Enqueue(new ImageMessageInfo() {
                                Barcode = barcode,
                                BarcodeTimestamp = barcodeTimestamp,
                            });
                        }
                    }
                    catch (Exception e) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = e
                        });
                    }
                    finally {
                        _takePhotoSlim.Release();
                    }
                }
            }, cancellation);
            return Task.CompletedTask;
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

        public class ImageMessageInfo {
            public string Barcode { get; set; } = string.Empty;

            public long BarcodeTimestamp { get; set; }
        }

        protected virtual async void OnPhotoTaken(PhotoTakenEventArgs e) {
            await Task.Yield();
            PhotoTaken?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(CameraConnectionEventArgs e) {
            await Task.Yield();
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            await Task.Yield();
            CameraUnregistered?.Invoke(this, e);
        }
    }
}