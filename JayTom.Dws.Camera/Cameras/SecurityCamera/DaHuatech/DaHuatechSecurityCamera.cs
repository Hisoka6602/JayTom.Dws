using System;
using NetSDKCS;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using Image = System.Drawing.Image;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech {

    public class DaHuatechSecurityCamera : ISecurityCamera {
        private IntPtr _mLoginId = IntPtr.Zero;
        private NET_DEVICEINFO_Ex _mDeviceInfo;
        private static fDisConnectCallBack? _mDisConnectCallBack;
        private static fHaveReConnectCallBack? _mReConnectCallBack;
        private static fRealDataCallBackEx2? _mRealDataCallBackEx2;
        private static fSnapRevCallBack? _mSnapRevCallBack;
        private SemaphoreSlim _takePhotoSlim = new(1);
        private ConcurrentQueue<ImageMessageInfo> _imageMessageQueue = new();
        private SemaphoreSlim _snapRevPhotoSlim = new(1);
        private byte[] _imageBytes = Array.Empty<byte>();

        public void Dispose() {
            throw new NotImplementedException();
        }

        public CameraInfo? Info { get; private set; } = new();
        public SdkType SdkType => SdkType.SecurityCamera;
        public string SdkName => "NetSDKCS.dll";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.PanoramicCamera;

        public List<CameraInfo>? EnumerateCameras() {
            throw new NotImplementedException();
        }

        public event EventHandler<CameraExceptionEventArgs>? CameraExceptionOccurred;

        public event EventHandler<CameraConnectionEventArgs>? CameraDisconnected;

        public event EventHandler<CameraInitializedEventArgs>? CameraInitialized;

        public event EventHandler<CameraStartedEventArgs>? CameraStarted;

        public event EventHandler<CameraStoppedEventArgs>? CameraStopped;

        public event EventHandler<CameraUnregisteredEventArgs>? CameraUnregistered;

        public async Task<KeyValuePair<bool, string>> Initialize(object param) {
            await Task.Yield();
            try {
                _mDisConnectCallBack += delegate (IntPtr id, IntPtr dvrip, int port, IntPtr user) {
                };
                _mReConnectCallBack += delegate (IntPtr id, IntPtr dvrip, int port, IntPtr user) {
                };
                _mRealDataCallBackEx2 += delegate (IntPtr handle, uint type, IntPtr buffer, uint size, IntPtr nint, IntPtr user) { };
                _mSnapRevCallBack += async delegate (IntPtr id, IntPtr buf, uint len, uint type, uint serial, IntPtr user) {
                    try {
                        await _snapRevPhotoSlim.WaitAsync();
                        Image? imageBitmap = null;
                        if (type == 10) //.jpg
                        {
                            _imageBytes = new byte[len];
                            Marshal.Copy(buf, _imageBytes, 0, (int)len);
                            using var stream = new MemoryStream(_imageBytes);
                            imageBitmap = Image.FromStream(stream);
                        }
                        var tryDequeue = _imageMessageQueue.TryDequeue(out var imageMessageInfo);
                        if (tryDequeue && imageMessageInfo is not null) {
                            var image = imageBitmap?.GetThumbnailImage(imageBitmap.Width, imageBitmap.Height,
                                () => false, IntPtr.Zero);
                            var thumbnailImage = imageBitmap?.GetThumbnailImage(1024, 768, () => false, IntPtr.Zero);
                            OnPhotoTaken(new PhotoTakenEventArgs() {
                                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                                Barcode = imageMessageInfo.Barcode,
                                BarcodeTimestamp = imageMessageInfo.BarcodeTimestamp,
                                CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                                Image = (Bitmap?)image,
                                ThumbImage = (Bitmap?)thumbnailImage,
                                PhotoTime = DateTime.Now,
                            });
                            imageBitmap?.Dispose();
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
                };
                //初始化
                NETClient.Init(_mDisConnectCallBack, IntPtr.Zero, null);
                //自动取流回调
                NETClient.SetAutoReconnect(_mReConnectCallBack, IntPtr.Zero);
                //抓图回调
                NETClient.SetSnapRevCallBack(_mSnapRevCallBack, IntPtr.Zero);

                OnCameraInitialized(new CameraInitializedEventArgs() {
                    CameraInfo = Info
                });
                return new KeyValuePair<bool, string>(true, "初始成功!");
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            //连接
            await Task.Yield();
            if (IntPtr.Zero == _mLoginId) {
                string ipAddress = "192.168.31.108";
                ushort port = 37777;
                _mDeviceInfo = new NET_DEVICEINFO_Ex();
                _mLoginId = NETClient.LoginWithHighLevelSecurity(ipAddress, port, "admin", "Aa12345678",
                    EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref _mDeviceInfo);
                if (IntPtr.Zero == _mLoginId) {
                    var lastError = NETClient.GetLastError();
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(lastError)
                    });
                    return new KeyValuePair<bool, string>(false, lastError);
                }
                //获取信息
                Info = new CameraInfo {
                    SerialNumber = _mDeviceInfo.sSerialNumber,
                    Brand = "DaHuatech",
                    IpAddress = ipAddress,
                    Type = CameraType.VideoCamera,
                    ConnectionType = CameraConnectionType.Ethernet,
                };
                var serializeObject = JsonConvert.SerializeObject(_mDeviceInfo, Formatting.Indented);
                OnCameraStarted(new CameraStartedEventArgs() {
                    CameraInfo = Info
                });
                return new KeyValuePair<bool, string>(true, serializeObject);
            }
            else {
                return new KeyValuePair<bool, string>(true, JsonConvert.SerializeObject(Info, Formatting.Indented));
            }
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            //断开
            await Task.Yield();
            var result = NETClient.Logout(_mLoginId);
            if (!result) {
                var lastError = NETClient.GetLastError();
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception(lastError)
                });
                return new KeyValuePair<bool, string>(false, lastError);
            }
            _mLoginId = IntPtr.Zero;
            return new KeyValuePair<bool, string>(result, string.Empty);
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

        public async Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            await Task.Yield();

            #region 到本地图片

            /*
            var outParam = new NET_OUT_SNAP_PIC_TO_FILE_PARAM();
            try {
                await _takePhotoSlim.WaitAsync(cancellation);

                #region remote async snapshot 远程异步抓图

                if (!Directory.Exists($"{System.IO.Directory.GetCurrentDirectory()}\\Image")) {
                    Directory.CreateDirectory($"{System.IO.Directory.GetCurrentDirectory()}\\Image");
                }

                var width = 2560;
                var height = 1440;
                var stride = 3 * width;
                var imageSize = width * height * 3 + 4096; // 计算图像数据大小
                var inParam = new NET_IN_SNAP_PIC_TO_FILE_PARAM {
                    dwSize = (uint)Marshal.SizeOf(typeof(NET_IN_SNAP_PIC_TO_FILE_PARAM)),
                    stuParam = new NET_SNAP_PARAMS() {
                        Channel = 0,
                        Quality = 10,
                        mode = 0,
                        InterSnap = 0,
                        ImageSize = 255
                    }
                    //szFilePath = $"{System.IO.Directory.GetCurrentDirectory()}\\Image\\{barcode}.{barcodeTimestamp}.jpg"
                };
                outParam = new NET_OUT_SNAP_PIC_TO_FILE_PARAM {
                    dwSize = (uint)Marshal.SizeOf(typeof(NET_OUT_SNAP_PIC_TO_FILE_PARAM)),
                    dwPicBufLen = 1024000,
                    szPicBuf = Marshal.AllocHGlobal(1024000)
                };
                var ret = NETClient.SnapPictureToFile(_mLoginId, ref inParam, ref outParam, 1000);
                if (!ret) {
                    var lastError = NETClient.GetLastError();
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(lastError)
                    });
                    return;
                }

                var bitmap = Bitmap.FromHbitmap(outParam.szPicBuf);
                //var bitmap = new Bitmap(width, height, stride, PixelFormat.Format8bppIndexed, outParam.szPicBuf);

                bitmap.Save($"{System.IO.Directory.GetCurrentDirectory()}\\Image\\{barcode}{barcodeTimestamp}.jpg");

                #endregion remote async snapshot 远程异步抓图
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
            finally {
                if (outParam.szPicBuf != IntPtr.Zero) // 判断指针是否有效
                {
                    Marshal.FreeHGlobal(outParam.szPicBuf);
                }
                _takePhotoSlim.Release();
            }*/

            #endregion 到本地图片

            #region 到事件

            try {
                await _takePhotoSlim.WaitAsync(cancellation);
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellation);
                var asyncSnap = new NET_SNAP_PARAMS {
                    Channel = 0,
                    Quality = 6,
                    ImageSize = 2,
                    mode = 0,
                    InterSnap = 0
                };
                var ret = NETClient.SnapPictureEx(_mLoginId, asyncSnap, IntPtr.Zero);
                if (!ret) {
                    var lastError = NETClient.GetLastError();
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception(lastError)
                    });
                }
                //添加信息到队列
                _imageMessageQueue.Enqueue(new ImageMessageInfo() {
                    Barcode = barcode,
                    BarcodeTimestamp = barcodeTimestamp,
                });
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
            finally {
                _takePhotoSlim.Release();
            }

            #endregion 到事件
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
    }
}