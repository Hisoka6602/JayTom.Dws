using System;
using NetSDKCS;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using Microsoft.Win32;
using MVIDCodeReaderNet;
using System.Reflection;
using System.Diagnostics;
using MvCodeReaderSDKNet;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Point = System.Drawing.Point;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.FilterContainer;
using Rectangle = System.Drawing.Rectangle;
using static System.Net.Mime.MediaTypeNames;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision {

    public class HikvisionIndustrialCamera : IIndustrialCamera {
        private int _nRet = MVIDCodeReader.MVID_CR_OK;

        /// <summary>
        /// 设备列表(sdk)
        /// </summary>
        private static MVIDCodeReader.MVID_CAMERA_INFO_LIST _sdkDevList = new();

        private SemaphoreSlim _semaphoreSlim = new(1, 1);

        //private MVIDCodeReader.MVID_CAM_OUTPUT_INFO _stOutput = new();
        private MVIDCodeReader? _myCodeReader;

        private SemaphoreSlim _takePhotoSlim = new(1);
        private SemaphoreSlim _barCodeSlim = new(1);

        private byte[] _imageBuffer = null;
        private MVIDCodeReader.cbOutputdelegate? _imageCallback = null;

        private MVIDCodeReader.cbImageBufferdelegate? _readImageCallback = null;
        private double FrameRate { get; set; }

        //过滤器
        private readonly BarCodeFilterContainer _barCodeFilterContainer = new();

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        public HikvisionIndustrialCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.IndustrialCamera;
        }

        public HikvisionIndustrialCamera() {
        }

        /// <summary>
        /// 相机信息
        /// </summary>
        public MVIDCodeReader.MVID_CAMERA_INFO Structure;

        public CameraInfo? Info { get; private set; } = new();
        public SdkType SdkType => SdkType.IndustrialCameraSdk;
        public string SdkName => "MVIDCodeReader.Net";
        public bool IsOriginalImageOut { get; set; } = true;
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.ScannerCamera;

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            _devInfo.Clear();
            var cameraInfos = new List<CameraInfo>();
            var nRet = MVIDCodeReader.MVID_CR_CAM_EnumDevices_NET(ref _sdkDevList);
            if (MVIDCodeReader.MVID_CR_OK != nRet) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"相机枚举异常:{nRet:X}")
                });
                return cameraInfos;
            }

            for (var i = 0; i < _sdkDevList.nDeviceNum; i++) {
                var stDevInfo = (MVIDCodeReader.MVID_CAMERA_INFO)(Marshal.PtrToStructure(_sdkDevList.pstCamInfo[i], typeof(MVIDCodeReader.MVID_CAMERA_INFO)) ?? new MVIDCodeReader.MVID_CAMERA_INFO());
                //添加到队列
                if (!string.IsNullOrEmpty(stDevInfo.chSerialNumber)) {
                    var cameraInfo = new CameraInfo() {
                        Brand = stDevInfo.chManufacturerName ?? string.Empty,
                        IpAddress = ConvertUintToIpAddress(stDevInfo.nCurrentIp).ToString(),
                        Model = stDevInfo.chModelName ?? string.Empty,
                        Version = stDevInfo.chDeviceVersion ?? string.Empty,
                        SerialNumber = stDevInfo.chSerialNumber ?? string.Empty, //还有一个设备序列号nDeviceNumber不想知道是干吗用的
                        Name = stDevInfo.chUserDefinedName ?? string.Empty,
                        Type = CameraType.IndustrialCamera,
                        ConnectionType = stDevInfo.nCamType == MVIDCodeReader.MVID_GIGE_CAM
                            ? CameraConnectionType.Ethernet
                            : (stDevInfo.nCamType == MVIDCodeReader.MVID_USB_CAM
                                ? CameraConnectionType.Usb
                                : CameraConnectionType.Unknown),
                        Id = i
                    };
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
            //初始化
            await Task.Yield();
            if (_myCodeReader != null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }
            if (param is CameraInfo cameraInfo) {
                this.Info = cameraInfo;
                //取出对应Id
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    cameraInfo.Id = devInfo.Id;
                    if (devInfo.Id >= MVIDCodeReader.MVID_MAX_CAM_NUM) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("初始化失败:Id大于最大设备支持个数!")
                        });
                        return new KeyValuePair<bool, string>(false, "Id大于最大设备支持个数!");
                    }
                    if (_sdkDevList.pstCamInfo[devInfo.Id] == nint.Zero) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("初始化失败:Id不存在或已断开!")
                        });
                        return new KeyValuePair<bool, string>(false, "Id不存在或已断开!");
                    }

                    var pstCamInfo = _sdkDevList.pstCamInfo[devInfo.Id];
                    Structure = (MVIDCodeReader.MVID_CAMERA_INFO)(Marshal.PtrToStructure(pstCamInfo, typeof(MVIDCodeReader.MVID_CAMERA_INFO)) ?? new MVIDCodeReader.MVID_CAMERA_INFO());
                    _myCodeReader ??= new MVIDCodeReader();
                    //创建句柄
                    _nRet = _myCodeReader?.MVID_CR_CreateHandle_NET(MVIDCodeReader.MVID_BCR | MVIDCodeReader.MVID_TDCR) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:创建句柄失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"创建句柄失败,{_nRet:X}!");
                    }
                    //绑定设备
                    _nRet = _myCodeReader?.MVID_CR_CAM_BindDevice_NET(pstCamInfo) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:绑定设备失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"绑定设备失败,{_nRet:X}!");
                    }

                    //获取相机属性值
                    var nIntValue = new MVIDCodeReader.MVID_CAM_INTVALUE_EX();
                    _nRet = _myCodeReader?.MVID_CR_CAM_GetIntValue_NET("Width", ref nIntValue) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:获取相机属性值[Width]失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"获取相机属性值[Width]失败,{_nRet:X}!");
                    }
                    var nWidth = (int)nIntValue.nCurValue;
                    _nRet = _myCodeReader?.MVID_CR_CAM_GetIntValue_NET("Height", ref nIntValue) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:获取相机属性值[Height]失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"获取相机属性值[Height]失败,{_nRet:X}!");
                    }
                    var nHeight = (int)nIntValue.nCurValue;
                    _imageBuffer = new byte[nWidth * nHeight * 3 + 4096];
                    //设置缓存节点
                    _nRet = _myCodeReader?.MVID_CR_CAM_SetImageNodeNum_NET(10) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:设置缓存节点失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"设置缓存节点失败,{_nRet:X}!");
                    }
                    /// 仅二维码识别：MVID_TDCR | en:Recognize Two-Dimension code only: MVID_TDCR
                    /// 一维码 + 二维码 识别：MVID_BCR | MVID_TDCR | en:Recognize Barcode + Two-Dimension code: MVID_BCR | MVID_TDCR
                    _nRet = _myCodeReader?.MVID_CR_Algorithm_SetIntValue_NET("BCR_Ability", MVIDCodeReader.MVID_BCR | MVIDCodeReader.MVID_TDCR) ?? 0;
                    if (MVIDCodeReader.MVID_CR_OK != _nRet) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:设置读码类型失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"设置读码类型失败,{_nRet:X}!");
                    }

                    /*//设置图像输出模式
                    //MVIDCodeReader.MVID_IMAGE_OUTPUT_MODE.MVID_OUTPUT_RAW
                    _myCodeReader?.MVID_CR_CAM_SetImageOutPutMode_NET(MVIDCodeReader.MVID_IMAGE_OUTPUT_MODE.MVID_OUTPUT_NORMAL);
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:设置图像输出模式失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"设置图像输出模式失败,{_nRet:X}!");
                    }*/
                    //获取帧率
                    //FrameRate

                    OnCameraInitialized(new CameraInitializedEventArgs() {
                        CameraInfo = this.Info
                    });
                    return new KeyValuePair<bool, string>(true, "初始化成功");
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

        //条码回调事件
        public async void ImageCallbackFunc(IntPtr pstOutput, IntPtr puser) {
            if (Status == CameraStatus.Running && IntPtr.Zero != pstOutput) {
                var stOutput = new MVIDCodeReader.MVID_CAM_OUTPUT_INFO();
                await ProcessImageAsync(stOutput, pstOutput);
            }
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            await Task.Yield();
            //设置属性
            //设置图像输出模式
            if (BindingType is CameraBindingType.ScannerCamera) {
                _myCodeReader?.MVID_CR_CAM_SetImageOutPutMode_NET(MVIDCodeReader.MVID_IMAGE_OUTPUT_MODE.MVID_OUTPUT_NORMAL);//测试完需要改回来
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:设置图像输出模式失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"设置图像输出模式失败,{_nRet:X}!");
                }
            }
            else {
                _myCodeReader?.MVID_CR_CAM_SetImageOutPutMode_NET(MVIDCodeReader.MVID_IMAGE_OUTPUT_MODE.MVID_OUTPUT_NORMAL);
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"初始化失败:设置图像输出模式失败,{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"设置图像输出模式失败,{_nRet:X}!");
                }
            }
            //注册回调函数
            if (BindingType is CameraBindingType.ScannerCamera) {
                if (_imageCallback is null) {
                    _imageCallback = ImageCallbackFunc;
                    _nRet = _myCodeReader?.MVID_CR_CAM_RegisterImageCallBack_NET(_imageCallback, IntPtr.Zero) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:注册扫码回调函数失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"注册扫码回调函数失败,{_nRet:X}!");
                    }
                }
            }
            else if (BindingType is CameraBindingType.VideoCamera) {
                //注册不包含解码信息的回调
                if (_readImageCallback is null) {
                    _readImageCallback = (ref MVIDCodeReader.MVID_IMAGE_INFO output, IntPtr user) => { };
                    _nRet = _myCodeReader?.MVID_CR_CAM_RegisterImageBufferCallBack_NET(_readImageCallback, IntPtr.Zero) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:注册实时图像回调函数失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"注册实时图像回调函数失败,{_nRet:X}!");
                    }
                }
            }

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
            return new KeyValuePair<bool, string>(true, $"启动成功,{_nRet:X}");
        }

        public async Task<KeyValuePair<bool, string>> Stop() {
            await Task.Yield();
            if (Status == CameraStatus.Running) {
                var nRet = _myCodeReader?.MVID_CR_CAM_StopGrabbing_NET() ?? 0;
                if (MVIDCodeReader.MVID_CR_OK != nRet) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"停止识别失败:{_nRet:X}")
                    });
                    return new KeyValuePair<bool, string>(false, $"停止识别失败:{_nRet:X}");
                }

                Status = CameraStatus.Paused;
            }
            System.GC.Collect();
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public async void Dispose() {
            if (Status != CameraStatus.Uninitialized) {
                Status = CameraStatus.Paused;
                var nRet = _myCodeReader?.MVID_CR_CAM_StopGrabbing_NET() ?? 0;
                if (MVIDCodeReader.MVID_CR_OK != nRet) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"停止相机失败:{_nRet:X}")
                    });
                }

                await Task.Delay(500);
                nRet = _myCodeReader?.MVID_CR_DestroyHandle_NET() ?? 0;
                if (MVIDCodeReader.MVID_CR_OK != nRet) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"释放句柄失败:{_nRet:X}")
                    });
                }
                OnCameraDisconnected(new CameraConnectionEventArgs() {
                    CameraInfo = this.Info
                });
                OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                    CameraInfo = this.Info
                });
                _imageCallback = null;
                _readImageCallback = null;
                _myCodeReader = null;

                this.Info = null;
            }
            System.GC.Collect();
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            //设置限定读码之类的参数
            throw new NotImplementedException();
        }

        public int BarcodeBorderSize { get; set; } = 5;
        public System.Drawing.Color BarcodeBorderColor { get; set; } = System.Drawing.Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;
        public bool IsRealtimeImageEnabled { get; private set; }
        public int TakePhotoDelay { get; set; }

        public event EventHandler<BarcodeReadEventArgs>? BarcodeRead;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        public void StartRealTimeImage() {
            IsRealtimeImageEnabled = true;
        }

        public void StopRealTimeImage() {
            IsRealtimeImageEnabled = false;
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            //提交拍照请求
            Task.Run(async () => {
                await Task.Delay(TakePhotoDelay, cancellation);
                if (Status == CameraStatus.Running) {
                    try {
                        await _takePhotoSlim.WaitAsync(cancellation);
                        var pFrameInfo = new MVIDCodeReader.MVID_IMAGE_INFO();
                        _nRet = _myCodeReader?.MVID_CR_CAM_GetImageBuffer_NET(ref pFrameInfo, 300) ?? -1;
                        if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception($"截图失败:截取一帧图片失败,{_nRet:X}")
                            });
                            return;
                        }
                        var image = await ConvertPointerToImage(pFrameInfo);
                        var thumbnailImage = image?.GetThumbnailImage(800, 600, () => false, IntPtr.Zero);
                        await Task.Delay(100, cancellation);
                        OnPhotoTaken(new PhotoTakenEventArgs {
                            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                            CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                            Image = image,
                            PhotoTime = DateTime.Now,
                            ThumbImage = (Bitmap?)thumbnailImage,
                            Barcode = barcode,
                            BarcodeTimestamp = barcodeTimestamp
                        });
                    }
                    catch (Exception e) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"截图失败:截取一帧图片异常,{e}")
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
                await TakePhotoAsync(barcode, barcodeTimestamp, cancellation);
            }, cancellation);
            return Task.CompletedTask;
        }

        public void SetScanCodeFilterParams(ScanCodeFilterParams @params) {
            _barCodeFilterContainer.Pattern = @params.RegularExpression;
            _barCodeFilterContainer.MaxSize = @params.DuplicateBarcodeFilterCount;
            _barCodeFilterContainer.ExpirationTime = TimeSpan.FromMilliseconds(@params.ScanInterval);
        }

        private async Task ProcessImageAsync(MVIDCodeReader.MVID_CAM_OUTPUT_INFO stOutput, IntPtr ptr) {
            //帧时间戳
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (MVIDCodeReader.MVID_IMAGE_TYPE.MVID_IMAGE_BMP != stOutput.stImage.enImageType) {
                stOutput = (MVIDCodeReader.MVID_CAM_OUTPUT_INFO)(Marshal.PtrToStructure(ptr,
                    typeof(MVIDCodeReader.MVID_CAM_OUTPUT_INFO)) ?? new MVIDCodeReader.MVID_CAM_OUTPUT_INFO());
                var bitmap = await GetBitmapAsync(stOutput, ptr);
                //1024*768
                var thumbnailImage = bitmap?.GetThumbnailImage(800, 600, () => false, IntPtr.Zero);
                if (0 != stOutput.stCodeList.nCodeNum && BindingType != CameraBindingType.PanoramicCamera) {
                    if (IsShowBarcodeBorder && thumbnailImage is not null && thumbnailImage.PixelFormat != PixelFormat.Format8bppIndexed &&
                        stOutput.stCodeList.stCodeInfo?.Any() == true) {
                        //设置图像边框
                        using var g = Graphics.FromImage(thumbnailImage);

                        //画框
                        for (var i = 0; i < stOutput.stCodeList.nCodeNum; ++i) {
                            // ch:绘制矩形框 | en:Draw ractangle frame
                            var stPointList = new Point[4];
                            for (var j = 0; j < 4; ++j) {
                                stPointList[j].X =
                                    (int)(stOutput.stCodeList.stCodeInfo[i].stCornerPt[j].nX *
                                        (float)(thumbnailImage.Size.Width) / stOutput.stImage.nWidth);
                                stPointList[j].Y =
                                    (int)(stOutput.stCodeList.stCodeInfo[i].stCornerPt[j].nY *
                                          (float)(thumbnailImage.Size.Height) /
                                          stOutput.stImage.nHeight);
                            }
                            g.DrawPolygon(new System.Drawing.Pen(BarcodeBorderColor, BarcodeBorderSize), stPointList);
                        }
                    }

                    for (var i = 0; i < stOutput.stCodeList.nCodeNum; ++i) {
                        if (stOutput.stCodeList.stCodeInfo != null) {
                            var mvidCodeInfo = stOutput.stCodeList.stCodeInfo[i];
                            var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                                BarCode = mvidCodeInfo.strCode,
                                ScanTime = DateTime.Now
                            });
                            if (validateData) {
                                OnBarcodeRead(new BarcodeReadEventArgs() {
                                    Barcode = mvidCodeInfo.strCode,
                                    Timestamp = timestamp,
                                    CameraSerialNumber = this.Structure.chSerialNumber,
                                    ScanTime = DateTime.Now,
                                    ThumbImage = (Bitmap?)thumbnailImage,
                                    Image = bitmap,
                                    AreaCoords = Enumerable.Range(0, 4).Select(s => {
                                        if (bitmap != null)
                                            return new System.Drawing.Point {
                                                X = (int)(stOutput.stCodeList.stCodeInfo[i].stCornerPt[s].nX *
                                                    (float)(bitmap.Size.Width) / stOutput.stImage.nWidth),
                                                Y = (int)(stOutput.stCodeList.stCodeInfo[i].stCornerPt[s].nY *
                                                          (float)(bitmap.Size.Height) /
                                                          stOutput.stImage.nHeight)
                                            };
                                        return default;
                                    })?.ToList(),
                                });
                            }
                        }

                        await Task.Delay(1);
                    }
                }
                if (IsRealtimeImageEnabled) {
                    OnRealtimeImage(new RealtimeImageEventArgs() {
                        Timestamp = timestamp,
                        ThumbImage = (Bitmap?)thumbnailImage,
                    });
                }
                //显示图像
            }
        }

        private async Task<Bitmap?> GetBitmapAsync(MVIDCodeReader.MVID_CAM_OUTPUT_INFO stOutput, IntPtr ptr) {
            Bitmap? bitmap = null;
            try {
                bitmap = await ConvertPointerToImage(stOutput.stImage);

                if (IsOriginalImageOut) {
                    return (Bitmap?)bitmap?.GetThumbnailImage(bitmap?.Width ?? 1280, bitmap?.Height ?? 960,
                        () => false, IntPtr.Zero);
                }

                return (Bitmap?)bitmap?.GetThumbnailImage(800, 600, () => false, IntPtr.Zero);
            }
            finally {
                bitmap?.Dispose();
            }
        }

        private async Task<Bitmap?> ConvertPointerToImage(MVIDCodeReader.MVID_IMAGE_INFO pFrameInfo) {
            Bitmap? bitmap = null;
            try {
                await _semaphoreSlim.WaitAsync();

                var handle = GCHandle.Alloc(_imageBuffer, GCHandleType.Pinned);
                Marshal.Copy(pFrameInfo.pImageBuf, _imageBuffer, 0, (int)pFrameInfo.nImageLen);
                var pImage = handle.AddrOfPinnedObject();
                if (MVIDCodeReader.MVID_IMAGE_TYPE.MVID_IMAGE_MONO8 == pFrameInfo.enImageType) {
                    bitmap = new Bitmap(pFrameInfo.nWidth, pFrameInfo.nHeight, pFrameInfo.nWidth,
                        PixelFormat.Format8bppIndexed, pImage);

                    var cp = bitmap.Palette;
                    for (var i = 0; i < 256; i++) {
                        cp.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                    }

                    bitmap.Palette = cp;
                }
                else {
                    bitmap = new Bitmap(pFrameInfo.nWidth, pFrameInfo.nHeight, pFrameInfo.nWidth * 3,
                        PixelFormat.Format24bppRgb, pImage);
                }

                if (handle.IsAllocated) {
                    try {
                        handle.Free();
                    }
                    catch (Exception e) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = e
                        });
                    }
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
            finally {
                _semaphoreSlim.Release();
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
            try {
                await _barCodeSlim.WaitAsync();
                await Task.Delay(50);
                BarcodeRead?.Invoke(this, e);
            }
            finally {
                _barCodeSlim.Release();
            }
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
            await Task.Yield();
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual async void OnCameraStopped(CameraStoppedEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Paused;
            CameraStopped?.Invoke(this, e);
        }

        protected virtual async void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Uninitialized;
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual async void OnCameraDisconnected(CameraConnectionEventArgs e) {
            await Task.Yield();
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnPhotoTaken(PhotoTakenEventArgs e) {
            await Task.Yield();
            PhotoTaken?.Invoke(this, e);
        }

        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int SMTO_ABORTIFHUNG = 0x0002;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, int wParam, string lParam, int fuFlags,
            int uTimeout, IntPtr lpdwResult);

        public KeyValuePair<bool, string> AddSystemEnvironmentVariable(string path, string variableName = "Path") {
            try {
                using (RegistryKey? environmentKey = Registry.CurrentUser.OpenSubKey(@"Environment", true)) {
                    if (environmentKey != null) {
                        var currentValue = environmentKey.GetValue(variableName) as string;

                        // 检查是否已经包含 Percipio 路径
                        if (string.IsNullOrEmpty(currentValue) || !currentValue.Contains(path)) {
                            // 在现有值的末尾添加 Percipio 路径，并使用分号进行分隔
                            var newValue = currentValue + path;

                            environmentKey.SetValue(variableName, newValue);

                            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment",
                                SMTO_ABORTIFHUNG, 5000, IntPtr.Zero);

                            return new KeyValuePair<bool, string>(true, $"路径已成功添加到环境变量中");
                        }
                        else {
                            return new KeyValuePair<bool, string>(true, $"环境变量中已存在{path} 路径，无需添加。");
                        }
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "无法打开环境变量注册表项");
                    }
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
        }
    }
}