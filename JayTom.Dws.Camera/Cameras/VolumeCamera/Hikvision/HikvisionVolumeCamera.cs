using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using MvVolmeasure.NET;
using MvCodeReaderSDKNet;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Cameras.VolumeCamera.Hikvision {

    public class HikvisionVolumeCamera : IVolumeCamera {
        private static MvVolmeasure.NET.MvVolmeasure.VOLM_DEVICE_INFO_LIST _mStDeviceList = new();
        private MvVolmeasure.NET.MvVolmeasure? _mCsVolMeasure;
        private static Task? _volumeThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private byte[] _bufForDriver = new byte[1024 * 1024 * 20];

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        public HikvisionVolumeCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.VolumeCamera;
        }

        public HikvisionVolumeCamera() {
        }

        public async void Dispose() {
            //释放
            await Stop();
            _mCsVolMeasure?.DeInit();
            _mCsVolMeasure = null;
            OnCameraDisconnected(new CameraConnectionEventArgs() {
                CameraInfo = this.Info
            });
            OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                CameraInfo = this.Info
            });
            this.Info = null;
        }

        public CameraInfo? Info { get; private set; }
        public SdkType SdkType { get; private set; } = SdkType.VolumeCameraSdk;
        public string SdkName { get; } = "VolMeasure.Net";
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.VolumeCamera;

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            var cameras = new List<CameraInfo>();
            uint nType = MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_DEVICE | MvVolmeasure.NET.MvVolmeasure.MV_VOLM_USB_DEVICE;
            int nRet = MvVolmeasure.NET.MvVolmeasure.EnumStereoCamEx(nType, ref _mStDeviceList);
            if (0 != nRet) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"海康体积相机枚举失败:{nRet:X}")
                });

                return cameras;
            }

            for (var i = 0; i < _mStDeviceList.nDeviceNum; i++) {
                var device = (MvVolmeasure.NET.MvVolmeasure.VOLM_DEVICE_INFO)(Marshal.PtrToStructure(_mStDeviceList.pDeviceInfo[i], typeof(MvVolmeasure.NET.MvVolmeasure.VOLM_DEVICE_INFO)) ?? new MvVolmeasure.NET.MvVolmeasure.VOLM_DEVICE_INFO());
                if (device.nReserved != null && (uint)(VOLM_CAMERA_TYPE.VOLM_CAMERA_3D) == device.nReserved[0]) {
                    var buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo ?? Array.Empty<byte>(), 0);
                    var gigeInfo = (MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_NET_INFO)(Marshal.PtrToStructure(buffer, typeof(MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_NET_INFO)) ?? new MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_NET_INFO());

                    var cameraInfo = new CameraInfo() {
                        Brand = gigeInfo.chManufacturerName ?? string.Empty,
                        IpAddress = ConvertUintToIpAddress(gigeInfo.nCurrentIp).ToString(),
                        Model = gigeInfo.chModelName ?? string.Empty,
                        Version = gigeInfo.chDeviceVersion ?? string.Empty,
                        SerialNumber =
                            gigeInfo.chSerialNumber ?? string.Empty, //还有一个设备序列号nDeviceNumber不想知道是干吗用的
                        Name = gigeInfo.chUserDefinedName ?? string.Empty,
                        Type = CameraType.VolumeCamera,
                        ConnectionType = device.nTLayerType == MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_DEVICE
                            ? CameraConnectionType.Ethernet
                            : (device.nTLayerType == MvVolmeasure.NET.MvVolmeasure.MV_VOLM_USB_DEVICE
                                ? CameraConnectionType.Usb
                                : CameraConnectionType.Unknown),
                        Id = i,
                    };
                    _devInfo.AddOrUpdate(cameraInfo.SerialNumber, cameraInfo, (k, v) => cameraInfo);
                    cameras.Add(cameraInfo);
                }
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
            await Task.Yield();
            if (_mCsVolMeasure != null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }

            if (param is CameraInfo cameraInfo) {
                this.Info = cameraInfo;
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    if (cameraInfo.Id >= 256) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("初始化失败:Id大于最大设备支持个数!")
                        });
                        return new KeyValuePair<bool, string>(false, "Id大于最大设备支持个数!");
                    }

                    if (_mStDeviceList.pDeviceInfo[devInfo.Id] == nint.Zero) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("初始化失败:Id不存在或已断开!")
                        });
                        return new KeyValuePair<bool, string>(false, "Id不存在或已断开!");
                    }
                    var deviceInfo = _mStDeviceList.pDeviceInfo[devInfo.Id];

                    //初始化对象
                    var strSerial = string.Empty;
                    var device = (MvVolmeasure.NET.MvVolmeasure.VOLM_DEVICE_INFO)(Marshal.PtrToStructure(deviceInfo, typeof(MvVolmeasure.NET.MvVolmeasure.VOLM_DEVICE_INFO)) ?? new MvVolmeasure.NET.MvVolmeasure.VOLM_DEVICE_INFO());
                    _mCsVolMeasure ??= new MvVolmeasure.NET.MvVolmeasure();
                    if (MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_DEVICE == device.nTLayerType) {
                        var buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo ?? Array.Empty<byte>(), 0);
                        var gigeInfo = (MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_NET_INFO)(Marshal.PtrToStructure(buffer, typeof(MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_NET_INFO)) ?? new MvVolmeasure.NET.MvVolmeasure.MV_VOLM_GIGE_NET_INFO());
                        strSerial = gigeInfo.chSerialNumber;
                    }
                    else if (MvVolmeasure.NET.MvVolmeasure.MV_VOLM_USB_DEVICE == device.nTLayerType) {
                        var buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo ?? Array.Empty<byte>(), 0);
                        var usbInfoTmp = (MvVolmeasure.NET.MvVolmeasure.VOLM_USB3_DEVICE_INFO)(Marshal.PtrToStructure(buffer, typeof(MvVolmeasure.NET.MvVolmeasure.VOLM_USB3_DEVICE_INFO)) ?? new MvVolmeasure.NET.MvVolmeasure.VOLM_USB3_DEVICE_INFO());
                        strSerial = usbInfoTmp.chSerialNumber;
                    }
                    //通过mac地址创建句柄连接相机
                    var nRet = _mCsVolMeasure.CreateHandleBySerial(strSerial);
                    if (ERROR_DEFINE.MV_VOLM_OK != (ERROR_DEFINE)nRet) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"创建连接句柄失败:{nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"创建连接句柄失败:{nRet:X}");
                    }
                    OnCameraInitialized(new CameraInitializedEventArgs() {
                        CameraInfo = this.Info
                    });
                    return new KeyValuePair<bool, string>(false, $"初始化成功");
                    // 通过mac地址创建句柄连接相机
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
            //设置工作方式
            /*boxWorkMode.Items.Add("0. 不支持");
            boxWorkMode.Items.Add("1. 线激光相机本地体积，测量常规体积");
            boxWorkMode.Items.Add("5. 线激光相机本地体积，测量积分体积");
            boxWorkMode.Items.Add("7. 130W双目相机本地体积（深度图）");
            boxWorkMode.Items.Add("8. 160W双目相机本地体积（深度图）");
            boxWorkMode.Items.Add("9. 349线激光相机直接出体积");
            boxWorkMode.Items.Add("10. 双目相机直接出体积");
            boxWorkMode.Items.Add("11. 双目相机本地体积（原始图)");
            boxWorkMode.Items.Add("12. 线激光相机本地体积（原始图，积分体积）");
            boxWorkMode.Items.Add("13. 双目MV-DS1307-05E相机本地体积，(仅支持轮询，不支持回调)");*/

            /*switch (boxWorkMode.SelectedIndex) {
                case 0:
                    m_nCameraType = 0;
                    break;

                case 1:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_LSL;
                    break;

                case 2:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_LSL_MEASURE;
                    break;

                case 3:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_BINOSTEREO_VOLUME;
                    break;

                case 4:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_BINOSTEREO_VOLUME_160W;
                    break;

                case 5:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_LSL_MEASURE_349;
                    break;

                case 6:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_BINOSTEREO_VOLUME_DIRECT;
                    break;

                case 7:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_BINOSTEREO_MONO8_VOLUME;
                    break;

                case 8:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_LSL_BASE_MONO8_VOLUME;
                    break;

                case 9:
                    m_nCameraType = (int)CAMERATYPE_DEFINE.CAMERA_TYPE_BINOSTEREO_OB_VOLUME;
                    break;

                default:
                    break;
            }*/

            var nRet = _mCsVolMeasure?.SetAlgorithmType((int)CAMERATYPE_DEFINE.CAMERA_TYPE_LSL_MEASURE_349) ?? -1;
            if (ERROR_DEFINE.MV_VOLM_OK != (ERROR_DEFINE)nRet) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"设置获取模式失败:{nRet:X}")
                });

                return new KeyValuePair<bool, string>(false, $"设置获取模式失败:{nRet:X}");
            }
            //设置开启/关闭图像
            //_mCsVolMeasure?.SetVolAPIOutputImgEnable(true);
            //开始工作
            nRet = _mCsVolMeasure?.Start() ?? -1;
            if (ERROR_DEFINE.MV_VOLM_OK != (ERROR_DEFINE)nRet) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"开始工作失败:{nRet:X}")
                });

                return new KeyValuePair<bool, string>(false, $"开始工作失败:{nRet:X}");
            }
            //开启体积线程
            _cancellationTokenSource = new CancellationTokenSource();
            _volumeThread = Task.Factory.StartNew(async () => {
                await VolumeThread(_cancellationTokenSource.Token);
            }, _cancellationTokenSource.Token);
            OnCameraStarted(new CameraStartedEventArgs() {
                CameraInfo = this.Info
            });
            return new KeyValuePair<bool, string>(true, $"体积相机启动成功");
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            if (Status != CameraStatus.Uninitialized) {
                _cancellationTokenSource?.Cancel();
                await Task.Delay(200);
                if (_volumeThread != null) {
                    await _volumeThread;
                    _volumeThread?.Dispose();
                }

                _volumeThread = null;
                //停止SDK
                _mCsVolMeasure?.Stop();
                OnCameraStopped(new CameraStoppedEventArgs() {
                    CameraInfo = this.Info
                });
                System.GC.Collect();
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            return new KeyValuePair<bool, string>(false, "设备未初始化!");
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public bool IsRealtimeImageEnabled { get; private set; } = true;

        public void StartRealTimeImage() {
            IsRealtimeImageEnabled = true;
        }

        public void StopRealTimeImage() {
            IsRealtimeImageEnabled = false;
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            return Task.CompletedTask;
        }

        public int TakePhotoDelay { get; set; }

        public event EventHandler<VolumeCapturedEventArgs>? VolumeCaptured;

        public async Task VolumeThread(CancellationToken token) {
            await Task.Yield();
            var stResultInfo = new MvVolmeasure.NET.VOLM_RESULT_INFO();

            stResultInfo.stImage.pData = (IntPtr)Marshal.AllocHGlobal(_bufForDriver.Length);
            stResultInfo.nVolumeFlag = 0;
            stResultInfo.nImgFlag = 0;
            while (!token.IsCancellationRequested) {
                Bitmap? bitmap = null;
                Bitmap? thumbnailImage = null;
                var localTime = DateTimeOffset.Now.ToLocalTime();
                var timestamp = localTime.ToUnixTimeMilliseconds();
                var nRet = _mCsVolMeasure?.GetResult(ref stResultInfo) ?? -1;
                if (ERROR_DEFINE.MV_VOLM_OK == (ERROR_DEFINE)nRet) {
                    //检测图像标记位
                    if (1 == stResultInfo.nImgFlag) {
                        //实时画面
                        //用户自定义，处理图像信息，图像位于结构体stResultInfo.stImage
                        var volmFrameInfo = stResultInfo.stImage;
                        bitmap = await GetBitmapAsync(volmFrameInfo.pData, _bufForDriver, volmFrameInfo);
                        thumbnailImage = GenerateThumbnail(bitmap);
                    }
                    //判断体积标记位，是否有体积信息
                    if (1 == stResultInfo.nVolumeFlag) {
                        //在界面显示体积信息
                        var volumeCapturedEventArgs = new VolumeCapturedEventArgs() {
                            Length = Math.Round(stResultInfo.stVolumeInfo.length, 2),
                            Width = Math.Round(stResultInfo.stVolumeInfo.width, 2),
                            Height = Math.Round(stResultInfo.stVolumeInfo.height, 2),
                            Volume = Math.Round(stResultInfo.stVolumeInfo.volume, 2),
                            Image = bitmap,
                            Thumbnail = thumbnailImage,
                            Timestamp = DateTime.Now
                        };
                        OnVolumeCaptured(volumeCapturedEventArgs);
                    }

                    if (IsRealtimeImageEnabled) {
                        OnRealtimeImage(new RealtimeImageEventArgs() {
                            ThumbImage = thumbnailImage,
                            Timestamp = timestamp
                        });
                    }
                }

                await Task.Delay(50, token);
            }
            Marshal.FreeHGlobal(stResultInfo.stImage.pData);
        }

        private static IPAddress ConvertUintToIpAddress(uint ipAddressValue) {
            var addressBytes = BitConverter.GetBytes(ipAddressValue);
            Array.Reverse(addressBytes);

            return new IPAddress(addressBytes);
        }

        private async Task<Bitmap?> GetBitmapAsync(nint pData, byte[] imageBuffBytes,
            VOLM_FRAME_INFO volmFrameInfo) {
            await Task.Yield();
            Bitmap? bmp = null;
            // 绘制图像
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception($"volmFrameInfo.enPixelType:{volmFrameInfo.enPixelType}")
            });
            Marshal.Copy(pData, imageBuffBytes, 0, (int)volmFrameInfo.nFrameLen);
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception($"图像赋值完成")
            });
            switch ((MvVolmeasure.NET.CAMERATYPE_DEFINE)volmFrameInfo.enPixelType) {
                case MvVolmeasure.NET.CAMERATYPE_DEFINE.CAMERA_TYPE_BINOSTEREO_MONO8_VOLUME: {
                        var pImage = Marshal.UnsafeAddrOfPinnedArrayElement(imageBuffBytes, 0);
                        bmp = new Bitmap(volmFrameInfo.nWidth, volmFrameInfo.nHeight, volmFrameInfo.nWidth, PixelFormat.Format8bppIndexed, pImage);
                        var cp = bmp.Palette;
                        for (var i = 0; i < 256; i++) {
                            cp.Entries[i] = Color.FromArgb(i, i, i);
                        }
                        bmp.Palette = cp;
                        break;
                    }
                default: {
                        using var ms = new MemoryStream();
                        ms.Write(imageBuffBytes, 0, (int)volmFrameInfo.nFrameLen);
                        bmp = new Bitmap(ms);
                        break;
                    }
            }
            if (!IsOriginalImageOut) {
                bmp = (Bitmap?)GenerateThumbnail(bmp);
            }

            return bmp;
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

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(CameraConnectionEventArgs e) {
            Status = CameraStatus.Disconnected;
            await Task.Yield();

            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            await Task.Yield();
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnCameraStarted(CameraStartedEventArgs e) {
            Status = CameraStatus.Running;
            await Task.Yield();
            CameraStarted?.Invoke(this, e);
        }

        protected virtual async void OnCameraStopped(CameraStoppedEventArgs e) {
            Status = CameraStatus.Paused;
            await Task.Yield();
            CameraStopped?.Invoke(this, e);
        }

        protected virtual async void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            Status = CameraStatus.Uninitialized;
            await Task.Yield();
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual async void OnRealtimeImage(RealtimeImageEventArgs e) {
            await Task.Yield();
            RealtimeImage?.Invoke(this, e);
        }

        protected virtual async void OnVolumeCaptured(VolumeCapturedEventArgs e) {
            await Task.Yield();
            VolumeCaptured?.Invoke(this, e);
        }
    }
}