using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using MVIDCodeReaderNet;
using MvCodeReaderSDKNet;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision {

    public class HikvisionSmartCamera : ISmartCamera {
        private MvCodeReader.MV_CODEREADER_DEVICE_INFO_LIST _mStDeviceList = new();
        private MvCodeReader? _mvCodeReader;
        private byte[] _bufForDriver = new byte[1024 * 1024 * 20];
        private MvCodeReader.MV_CODEREADER_DEVICE_INFO Structure;
        public CameraInfo Info { get; private set; }

        public SdkType SdkType => SdkType.SmartCameraSdk;
        public string SdkName => "MvCodeReaderSDK.Net";

        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; } = CameraBindingType.ScannerCamera;

        public List<CameraInfo>? EnumerateCameras() {
            //枚举相机
            var cameraInfos = new List<CameraInfo>();
            _mStDeviceList = new MvCodeReader.MV_CODEREADER_DEVICE_INFO_LIST();
            var nRet = MvCodeReader.MV_CODEREADER_EnumDevices_NET(ref _mStDeviceList, MvCodeReader.MV_CODEREADER_GIGE_DEVICE);
            if (nRet != 0) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"相机枚举异常:{nRet:X}")
                });
                return cameraInfos;
            }

            for (var i = 0; i < _mStDeviceList.nDeviceNum; i++) {
                var stDevInfo = (MvCodeReader.MV_CODEREADER_DEVICE_INFO)(Marshal.PtrToStructure(_mStDeviceList.pDeviceInfo[i], typeof(MvCodeReader.MV_CODEREADER_DEVICE_INFO)) ?? new MvCodeReader.MV_CODEREADER_DEVICE_INFO());
                if (stDevInfo.nTLayerType == MvCodeReader.MV_CODEREADER_GIGE_DEVICE) {
                    //网口相机
                    var buffer = Marshal.UnsafeAddrOfPinnedArrayElement(stDevInfo.SpecialInfo.stGigEInfo ?? Array.Empty<byte>(), 0);
                    var stGigEDeviceInfo = (MvCodeReader.MV_CODEREADER_GIGE_DEVICE_INFO)(Marshal.PtrToStructure(buffer, typeof(MvCodeReader.MV_CODEREADER_GIGE_DEVICE_INFO)) ?? new MvCodeReader.MV_CODEREADER_GIGE_DEVICE_INFO());

                    cameraInfos.Add(new CameraInfo() {
                        Brand = stGigEDeviceInfo.chManufacturerName ?? string.Empty,
                        IpAddress = ConvertUintToIpAddress(stGigEDeviceInfo.nNetExport).ToString(),
                        Model = stGigEDeviceInfo.chModelName ?? string.Empty,
                        Version = stGigEDeviceInfo.chDeviceVersion ?? string.Empty,
                        SerialNumber = stGigEDeviceInfo.chSerialNumber ?? string.Empty,//还有一个设备序列号nDeviceNumber不想知道是干吗用的
                        Name = stGigEDeviceInfo.chUserDefinedName ?? string.Empty,
                        Type = CameraType.IndustrialCamera,
                        ConnectionType = stDevInfo.nTLayerType == MvCodeReader.MV_CODEREADER_GIGE_DEVICE ?
                            CameraConnectionType.Ethernet :
                            (stDevInfo.nTLayerType == MvCodeReader.MV_CODEREADER_USB_DEVICE ? CameraConnectionType.Usb : CameraConnectionType.Unknown),
                        Id = i
                    });
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
            if (_mvCodeReader != null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }

            if (param is CameraInfo cameraInfo) {
                if (cameraInfo.Id >= MvCodeReader.MV_CODEREADER_MAX_XML_SYMBOLIC_NUM) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("初始化失败:Id大于最大设备支持个数!")
                    });
                    return new KeyValuePair<bool, string>(false, "Id大于最大设备支持个数!");
                }

                if (_mStDeviceList.pDeviceInfo[cameraInfo.Id] == nint.Zero) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("初始化失败:Id不存在或已断开!")
                    });
                    return new KeyValuePair<bool, string>(false, "Id不存在或已断开!");
                }

                var deviceInfo = _mStDeviceList.pDeviceInfo[cameraInfo.Id];

                Structure =
                (MvCodeReader.MV_CODEREADER_DEVICE_INFO)(Marshal.PtrToStructure(deviceInfo,
                    typeof(MvCodeReader.MV_CODEREADER_DEVICE_INFO)) ?? new MvCodeReader.MV_CODEREADER_DEVICE_INFO());
                //创建对象，打开设备
                _mvCodeReader ??= new MvCodeReader();
                //创建句柄
                int nRet = _mvCodeReader.MV_CODEREADER_CreateHandle_NET(ref Structure);
                if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:创建句柄失败,{nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"创建句柄失败,{nRet:X}!");
                }
                //打开设备
                nRet = _mvCodeReader.MV_CODEREADER_OpenDevice_NET();
                if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                    _mvCodeReader.MV_CODEREADER_DestroyHandle_NET();
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:打开设备失败,{nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"打开设备失败,{nRet:X}!");
                }
                //设置采集模式
                nRet = _mvCodeReader.MV_CODEREADER_SetEnumValue_NET("TriggerMode", (uint)MvCodeReader.MV_CODEREADER_TRIGGER_MODE.MV_CODEREADER_TRIGGER_MODE_OFF);
                if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                    _mvCodeReader.MV_CODEREADER_DestroyHandle_NET();
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:设置采集模式失败,{nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"设置采集模式失败,{nRet:X}!");
                }
                //获取参数ExposureTime
                //获取参数Gain
                //获取参数AcquisitionFrameRate
                //注册回调函数
                OnCameraInitialized(new CameraInitializedEventArgs() {
                    CameraInfo = this.Info
                });
                return new KeyValuePair<bool, string>(true, "初始化成功");
            }

            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Start(object param) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> Stop() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public int BarcodeBorderSize { get; set; }
        public Color BarcodeBorderColor { get; set; }
        public bool IsShowBarcodeBorder { get; set; }

        public event EventHandler<BarcodeTriggeredEventArgs>? BarcodeReadTriggered;

        private static IPAddress ConvertUintToIpAddress(uint ipAddressValue) {
            var addressBytes = BitConverter.GetBytes(ipAddressValue);
            Array.Reverse(addressBytes);

            return new IPAddress(addressBytes);
        }

        private async Task BarcodeCallbackThread(CancellationToken token) {
            var nRet = MvCodeReader.MV_CODEREADER_OK;
            var pData = IntPtr.Zero;
            var stFrameInfoEx2 = new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2();
            var pstFrameInfoEx2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)));
            Marshal.StructureToPtr(stFrameInfoEx2, pstFrameInfoEx2, false);
            while (!token.IsCancellationRequested) {
                nRet = _mvCodeReader?.MV_CODEREADER_GetOneFrameTimeoutEx2_NET(ref pData, pstFrameInfoEx2, 1000) ?? 0;
                if (nRet == MvCodeReader.MV_CODEREADER_OK) {
                    stFrameInfoEx2 = (MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)(Marshal.PtrToStructure(pstFrameInfoEx2, typeof(MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)) ?? new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2());
                    if (0 >= stFrameInfoEx2.nFrameLen) {
                        continue;
                    }

                    Bitmap? bmp;
                    // 绘制图像
                    Marshal.Copy(pData, _bufForDriver, 0, (int)stFrameInfoEx2.nFrameLen);
                    if (stFrameInfoEx2.enPixelType == MvCodeReader.MvCodeReaderGvspPixelType.PixelType_CodeReader_Gvsp_Mono8) {
                        var pImage = Marshal.UnsafeAddrOfPinnedArrayElement(_bufForDriver, 0);
                        bmp = new Bitmap(stFrameInfoEx2.nWidth, stFrameInfoEx2.nHeight, stFrameInfoEx2.nWidth, PixelFormat.Format8bppIndexed, pImage);
                        var cp = bmp.Palette;
                        for (int i = 0; i < 256; i++) {
                            cp.Entries[i] = Color.FromArgb(i, i, i);
                        }
                        bmp.Palette = cp;
                    }
                    else if (stFrameInfoEx2.enPixelType == MvCodeReader.MvCodeReaderGvspPixelType.PixelType_CodeReader_Gvsp_Jpeg) {
                        GC.Collect();
                        using var ms = new MemoryStream();
                        ms.Write(_bufForDriver, 0, (int)stFrameInfoEx2.nFrameLen);
                        bmp = new Bitmap(ms);
                    }
                    var stBcrResultEx2 = (MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2)(Marshal.PtrToStructure(stFrameInfoEx2.UnparsedBcrList.pstCodeListEx2, typeof(MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2)) ?? new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2());
                    //返回条码
                    if (stBcrResultEx2.nCodeNum > 0) {
                        //识别到条码调用
                        OnBarcodeReadTriggered();
                    }
                }
            }
        }

        private async Task ProcessImageAsync() {
            //帧时间戳
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var nRet = MvCodeReader.MV_CODEREADER_OK;
            var pData = IntPtr.Zero;
            var stFrameInfoEx2 = new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2();
        }

        private async Task<Bitmap?> GetBitmapAsync() {
            var pstFrameInfoEx2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)));
            return null;
        }

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnCameraInitialized(CameraInitializedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeReadTriggered(BarcodeTriggeredEventArgs e) {
            await Task.Yield();
            BarcodeReadTriggered?.Invoke(this, e);
        }
    }
}