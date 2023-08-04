using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using MVIDCodeReaderNet;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision {

    public class HikvisionIndustrialCamera : IIndustrialCamera {
        private int _nRet = MVIDCodeReader.MVID_CR_OK;
        private MVIDCodeReader.MVID_CAMERA_INFO_LIST _stDevList = new();
        private MVIDCodeReader.MVID_CAM_OUTPUT_INFO _stOutput = new();
        private MVIDCodeReader? _myCodeReader;
        private byte[] ImageBuffer = null;

        /// <summary>
        /// 相机信息
        /// </summary>
        public MVIDCodeReader.MVID_CAMERA_INFO Structure;

        public CameraInfo Info { get; } = new();
        public bool IsOriginalImageOut { get; set; }
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; } = CameraBindingType.ScannerCamera;

        public List<CameraInfo>? EnumerateCameras() {
            var cameraInfos = new List<CameraInfo>();
            _nRet = MVIDCodeReader.MVID_CR_CAM_EnumDevices_NET(ref _stDevList);
            if (MVIDCodeReader.MVID_CR_OK != _nRet) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"相机枚举异常:{_nRet:X}")
                });
                return cameraInfos;
            }

            for (int i = 0; i < _stDevList.nDeviceNum; i++) {
                var stDevInfo = (MVIDCodeReader.MVID_CAMERA_INFO)(Marshal.PtrToStructure(_stDevList.pstCamInfo[i], typeof(MVIDCodeReader.MVID_CAMERA_INFO)) ?? new MVIDCodeReader.MVID_CAMERA_INFO());
                cameraInfos.Add(new CameraInfo() {
                    Brand = stDevInfo.chManufacturerName ?? string.Empty,
                    IpAddress = ConvertUintToIpAddress(stDevInfo.nNetExport).ToString(),
                    Model = stDevInfo.chModelName ?? string.Empty,
                    Version = stDevInfo.chDeviceVersion ?? string.Empty,
                    SerialNumber = stDevInfo.chSerialNumber ?? string.Empty,//还有一个设备序列号nDeviceNumber不想知道是干吗用的
                    Name = stDevInfo.chUserDefinedName ?? string.Empty,
                    Type = CameraType.IndustrialCamera,
                    ConnectionType = stDevInfo.nCamType == MVIDCodeReader.MVID_GIGE_CAM ?
                        CameraConnectionType.Ethernet :
                        (stDevInfo.nCamType == MVIDCodeReader.MVID_USB_CAM ? CameraConnectionType.Usb : CameraConnectionType.Unknown),
                    Id = i
                });
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
            //初始化
            await Task.Yield();
            if (_myCodeReader != null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }
            if (param is CameraInfo cameraInfo) {
                if (cameraInfo.Id >= MVIDCodeReader.MVID_MAX_CAM_NUM) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("初始化失败:Id大于最大设备支持个数!")
                    });
                    return new KeyValuePair<bool, string>(false, "Id大于最大设备支持个数!");
                }

                if (_stDevList.pstCamInfo[cameraInfo.Id] == nint.Zero) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception("初始化失败:Id不存在或已断开!")
                    });
                    return new KeyValuePair<bool, string>(false, "Id不存在或已断开!");
                }

                Structure = (MVIDCodeReader.MVID_CAMERA_INFO)(Marshal.PtrToStructure(_stDevList.pstCamInfo[cameraInfo.Id], typeof(MVIDCodeReader.MVID_CAMERA_INFO)) ?? new MVIDCodeReader.MVID_CAMERA_INFO());
                //创建句柄
                _nRet = _myCodeReader?.MVID_CR_CreateHandle_NET(MVIDCodeReader.MVID_BCR | MVIDCodeReader.MVID_TDCR) ?? 0;
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:创建句柄失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"创建句柄失败,{_nRet:X}!");
                }
                //绑定设备
                _nRet = _myCodeReader?.MVID_CR_CAM_BindDevice_NET(_stDevList.pstCamInfo[cameraInfo.Id]) ?? 0;
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:绑定设备失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"绑定设备失败,{_nRet:X}!");
                }
                //注册回调函数
                _nRet = _myCodeReader?.MVID_CR_CAM_RegisterImageCallBack_NET(ImageCallbackFunc, IntPtr.Zero) ?? 0;
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:注册回调函数失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"注册回调函数失败,{_nRet:X}!");
                }
                //获取相机属性值
                int nWidth, nHeight = 0;
                var nIntValue = new MVIDCodeReader.MVID_CAM_INTVALUE_EX();
                _nRet = _myCodeReader?.MVID_CR_CAM_GetIntValue_NET("Width", ref nIntValue) ?? 0;
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:获取相机属性值[Width]失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"获取相机属性值[Width]失败,{_nRet:X}!");
                }
                nWidth = (int)nIntValue.nCurValue;
                _nRet = _myCodeReader?.MVID_CR_CAM_GetIntValue_NET("Height", ref nIntValue) ?? 0;
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:获取相机属性值[Height]失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"获取相机属性值[Height]失败,{_nRet:X}!");
                }
                nHeight = (int)nIntValue.nCurValue;
                ImageBuffer = new byte[nWidth * nHeight * 3 + 4096];
                //设置缓存节点
                _nRet = _myCodeReader?.MVID_CR_CAM_SetImageNodeNum_NET(10) ?? 0;
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:设置缓存节点失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"设置缓存节点失败,{_nRet:X}!");
                }
                //设置图像输出模式
                _myCodeReader?.MVID_CR_CAM_SetImageOutPutMode_NET(MVIDCodeReader.MVID_IMAGE_OUTPUT_MODE.MVID_OUTPUT_RAW);
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:设置图像输出模式失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"设置图像输出模式失败,{_nRet:X}!");
                }
                //获取帧率
                OnCameraInitialized(new CameraInitializedEventArgs() {
                    CameraInfo = this.Info
                });
                return new KeyValuePair<bool, string>(true, "初始化成功");
            }
            else {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("初始化传参类型错误!")
                });
                return new KeyValuePair<bool, string>(false, "初始化传参类型错误!");
            }
        }

        //条码回调事件
        private async void ImageCallbackFunc(IntPtr pstOutput, IntPtr puser) {
            if (Status == CameraStatus.Running && IntPtr.Zero != pstOutput) {
                _stOutput = new MVIDCodeReader.MVID_CAM_OUTPUT_INFO();
                await ProcessImageAsync(_stOutput, pstOutput);
            }
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            await Task.Yield();
            _nRet = _myCodeReader?.MVID_CR_CAM_StartGrabbing_NET() ?? 0;
            if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"启动失败:{_nRet:X}")
                });
                return new KeyValuePair<bool, string>(false, $"启动失败:{_nRet:X}!");
            }
            OnCameraStarted(new CameraStartedEventArgs() {
                CameraInfo = this.Info
            });
            return new KeyValuePair<bool, string>(true, $"启动成功!");
        }

        public Task<KeyValuePair<bool, string>> Stop() {
            //停止
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
        public bool IsRealtimeImageEnabled { get; set; }

        public event EventHandler<BarcodeReadEventArgs>? BarcodeRead;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        private async Task ProcessImageAsync(MVIDCodeReader.MVID_CAM_OUTPUT_INFO stOutput, IntPtr ptr) {
            //帧时间戳
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (MVIDCodeReader.MVID_IMAGE_TYPE.MVID_IMAGE_BMP != _stOutput.stImage.enImageType) {
                stOutput = (MVIDCodeReader.MVID_CAM_OUTPUT_INFO)(Marshal.PtrToStructure(ptr,
                    typeof(MVIDCodeReader.MVID_CAM_OUTPUT_INFO)) ?? new MVIDCodeReader.MVID_CAM_OUTPUT_INFO());
                var bitmap = await GetBitmapAsync(_stOutput, ptr);
                if (0 != _stOutput.stCodeList.nCodeNum) {
                    for (var i = 0; i < stOutput.stCodeList.nCodeNum; ++i) {
                        var mvidCodeInfo = _stOutput.stCodeList.stCodeInfo[i];
                        OnBarcodeRead(new BarcodeReadEventArgs() {
                            Barcode = mvidCodeInfo.strCode,
                            Timestamp = timestamp,
                            CameraSerialNumber = this.Structure.chSerialNumber,
                            ScanTime = DateTime.Now,
                            ThumbImage = bitmap?.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                PixelFormat.Format32bppArgb),
                            Image = bitmap
                        });
                        await Task.Delay(1);
                    }
                }

                if (IsRealtimeImageEnabled) {
                    OnRealtimeImage(new RealtimeImageEventArgs() {
                        Image = bitmap,
                        Timestamp = timestamp,
                        ThumbImage = bitmap?.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            PixelFormat.Format32bppArgb),
                    });
                }
                //显示图像
            }
        }

        private async Task<Bitmap?> GetBitmapAsync(MVIDCodeReader.MVID_CAM_OUTPUT_INFO _stOutput, IntPtr ptr) {
            /*await Task.Yield();
            Bitmap? bitmap = null;
            var handle = GCHandle.Alloc(ImageBuffer, GCHandleType.Pinned);
            Marshal.Copy(_stOutput.stImage.pImageBuf, ImageBuffer, 0, (int)_stOutput.stImage.nImageLen);
            var pImage = handle.AddrOfPinnedObject();
            if (MVIDCodeReader.MVID_IMAGE_TYPE.MVID_IMAGE_MONO8 == _stOutput.stImage.enImageType) {
                bitmap = new Bitmap(_stOutput.stImage.nWidth, _stOutput.stImage.nHeight, _stOutput.stImage.nWidth,
                    PixelFormat.Format8bppIndexed, pImage);

                var cp = bitmap.Palette;
                for (int i = 0; i < 256; i++) {
                    cp.Entries[i] = Color.FromArgb(i, i, i);
                }

                bitmap.Palette = cp;
            }
            else {
                bitmap = new Bitmap(_stOutput.stImage.nWidth, _stOutput.stImage.nHeight, _stOutput.stImage.nWidth * 3,
                    PixelFormat.Format24bppRgb, pImage);
            }
            if (handle.IsAllocated) {
                try {
                    handle.Free();
                }
                catch {
                    // ignored
                }
            }

            if (_stOutput.stCodeList.nCodeNum > 0) {
                if (IsOriginalImageOut) {
                    return (Bitmap?)bitmap?.GetThumbnailImage(bitmap?.Width ?? 1280, bitmap?.Height ?? 960, () => { return false; }, IntPtr.Zero);
                }
                return (Bitmap?)bitmap?.GetThumbnailImage(1280, 960, () => { return false; }, IntPtr.Zero);
            }

            return bitmap;*/
            await Task.Yield();
            Bitmap? bitmap = null;

            var handle = GCHandle.Alloc(ImageBuffer, GCHandleType.Pinned);
            var pImage = handle.AddrOfPinnedObject();

            int thumbnailWidth, thumbnailHeight;

            if (MVIDCodeReader.MVID_IMAGE_TYPE.MVID_IMAGE_MONO8 == _stOutput.stImage.enImageType) {
                bitmap = new Bitmap(_stOutput.stImage.nWidth, _stOutput.stImage.nHeight, _stOutput.stImage.nWidth,
                    PixelFormat.Format8bppIndexed, pImage);

                var cp = bitmap.Palette;
                for (var i = 0; i < 256; i++) {
                    cp.Entries[i] = Color.FromArgb(i, i, i);
                }

                bitmap.Palette = cp;

                thumbnailWidth = bitmap.Width;
                thumbnailHeight = bitmap.Height;
            }
            else {
                bitmap = new Bitmap(_stOutput.stImage.nWidth, _stOutput.stImage.nHeight, _stOutput.stImage.nWidth * 3,
                    PixelFormat.Format24bppRgb, pImage);

                thumbnailWidth = 1280;
                thumbnailHeight = 960;
            }

            if (handle.IsAllocated) {
                handle.Free();
            }

            if (_stOutput.stCodeList.nCodeNum > 0) {
                if (IsOriginalImageOut) {
                    return (Bitmap?)bitmap?.GetThumbnailImage(thumbnailWidth, thumbnailHeight, () => false, IntPtr.Zero);
                }
                return (Bitmap?)bitmap?.GetThumbnailImage(1280, 960, () => false, IntPtr.Zero);
            }

            return bitmap;
        }

        private static IPAddress ConvertUintToIpAddress(uint ipAddressValue) {
            var addressBytes = BitConverter.GetBytes(ipAddressValue);
            Array.Reverse(addressBytes);

            return new IPAddress(addressBytes);
        }

        protected virtual async void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            await Task.Yield();
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual async void OnBarcodeRead(BarcodeReadEventArgs e) {
            await Task.Yield();
            BarcodeRead?.Invoke(this, e);
        }

        protected virtual async void OnRealtimeImage(RealtimeImageEventArgs e) {
            await Task.Yield();
            RealtimeImage?.Invoke(this, e);
        }

        protected virtual async void OnCameraStarted(CameraStartedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual async void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }
    }
}