using System;
using NetSDKCS;
using System.Net;
using System.Linq;
using System.Text;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using Microsoft.Win32;
using MVIDCodeReaderNet;
using System.Reflection;
using System.Diagnostics;
using MvCodeReaderSDKNet;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Pen = System.Drawing.Pen;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Point = System.Drawing.Point;
using Image = System.Drawing.Image;
using Color = System.Drawing.Color;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.FilterContainer;
using Rectangle = System.Drawing.Rectangle;
using static System.Net.Mime.MediaTypeNames;
using static MVIDCodeReaderNet.MVIDCodeReader;
using JayTom.Dws.Camera.Attributes.CameraAttributes;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Wayzim;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision {

    public class HikvisionIndustrialCamera : IIndustrialCamera {
        private int _nRet = MVIDCodeReader.MVID_CR_OK;

        /// <summary>
        /// 设备列表(sdk)
        /// </summary>
        private static MVIDCodeReader.MVID_CAMERA_INFO_LIST _sdkDevList = new();

        private readonly SemaphoreSlim _drawSlim = new(1, 1);

        //private MVIDCodeReader.MVID_CAM_OUTPUT_INFO _stOutput = new();
        private MVIDCodeReader? _myCodeReader;

        private readonly SemaphoreSlim _takePhotoSlim = new(1, 1);
        private MVIDCodeReader.cbOutputdelegate? _imageCallback = null;

        private MVIDCodeReader.cbImageBufferdelegate? _readImageCallback = null;
        private double FrameRate { get; set; }
        private int _ocrMissCount = 0;
        private long _frameNo = 0;

        /// <summary>
        /// Ocr图像队列
        /// </summary>
        private readonly ConcurrentQueue<Bitmap> _ocrBitmapQueue = new();

        private Task? _ocrThread;
        private CancellationTokenSource? _ocrCancellationTokenSource;
        private readonly SemaphoreSlim _ocrSignal = new(0, 1);

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

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
                        Id = i,
                        //如果是海康的工业相机则支持
                        IsOcrSupported = ((stDevInfo.chManufacturerName?.Contains("Hikrobot") == true ||
                                           stDevInfo.chManufacturerName?.Contains("Hikvision") == true) &&
                                          stDevInfo.chModelName?.StartsWith("MV-PD") == true),
                        SupportedBindingType =
                            CameraBindingType.ScannerCamera | CameraBindingType.PanoramaCamera
                    };
                    if (cameraInfo.Model.StartsWith("MV-PD")) {
                        _devInfo.AddOrUpdate(cameraInfo.SerialNumber, cameraInfo, (k, v) => cameraInfo);
                        cameraInfos.Add(cameraInfo);
                    }
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
                    _nRet = _myCodeReader?.MVID_CR_CAM_GetIntValue_NET("Height", ref nIntValue) ?? 0;
                    if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:获取相机属性值[Height]失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"获取相机属性值[Height]失败,{_nRet:X}!");
                    }
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

                    /*//设置抠图

                    _nRet = _myCodeReader?.MVID_CR_Algorithm_SetIntValue_NET(MVIDCodeReader.KEY_WAYBILL_ABILITY, MVIDCodeReader.MVID_WAYBILL) ?? 0;
                    if (MVIDCodeReader.MVID_CR_OK != _nRet) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"抠图设置失败,{_nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"抠图设置失败,{_nRet:X}!");
                    }*/

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

                    //注册Ocr线程
                    if (this.BindingType is CameraBindingType.OcrCamera) {
                        if (Ocr is not null) {
                            var (key, value) = await Ocr.Initialize();
                            if (key) {
                                _ocrCancellationTokenSource = new CancellationTokenSource();
                                _ocrThread = Task.Run(
                                    () => OcrCallbackThread(_ocrCancellationTokenSource.Token),
                                    _ocrCancellationTokenSource.Token);
                            }
                            else {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception($"Ocr初始化失败:{value}!")
                                });
                                return new KeyValuePair<bool, string>(false, $"Ocr初始化失败:{value}!");
                            }
                        }
                        else {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception("Ocr对象未初始化!")
                            });
                            return new KeyValuePair<bool, string>(false, "Ocr对象未初始化!");
                        }
                    }

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

        /// <summary>
        /// Ocr回调处理逻辑
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task OcrCallbackThread(CancellationToken token) {
            while (true) {
                await _ocrSignal.WaitAsync(token);
                Bitmap? bitmap = null;
                while (_ocrBitmapQueue.TryDequeue(out var queuedBitmap)) {
                    bitmap?.Dispose();
                    bitmap = queuedBitmap;
                }

                if (bitmap is null) {
                    continue;
                }

                var thumbnail = GenerateThumbnail(bitmap);
                var result = Ocr?.ParseOcrResult(bitmap);
                if (result is not null && !string.IsNullOrEmpty(result.BarCode)) {
                    _ocrMissCount = 0;
                    ClearOcrQueue();
                    var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo {
                        BarCode = result.BarCode,
                        ScanTime = DateTime.Now
                    });
                    if (validateData.IsValidationPassed) {
                        result.CameraSerialNumber = Info?.SerialNumber ?? string.Empty;
                        if (IsShowBarcodeBorder && thumbnail is not null &&
                            thumbnail.PixelFormat != PixelFormat.Format8bppIndexed) {
                            thumbnail = await DrawIndicator(
                                thumbnail,
                                new Size(bitmap.Width, bitmap.Height),
                                result);
                        }
                        result.Thumbnail = thumbnail;
                        result.BarCode = _barCodeFilterContainer.RegexReplace(result.BarCode);
                        OnOcrContentRecognized(result);
                    }
                    else {
                        result.Image?.Dispose();
                        if (!IsRealtimeImageEnabled) {
                            thumbnail?.Dispose();
                        }
                    }
                }
                else {
                    result?.Image?.Dispose();
                    if (result?.Image is null) {
                        bitmap.Dispose();
                    }
                    if (!IsRealtimeImageEnabled) {
                        thumbnail?.Dispose();
                    }

                    _ocrMissCount++;
                    if (_ocrMissCount > 3) {
                        ClearOcrQueue();
                    }
                }

                if (IsRealtimeImageEnabled) {
                    OnRealtimeImage(new RealtimeImageEventArgs {
                        ThumbImage = thumbnail,
                        Timestamp = result?.SubmitTimestamp ?? 0
                    });
                }
            }
        }

        //条码回调事件
        public void ImageCallbackFunc(IntPtr pstOutput, IntPtr puser) {
            if (Status == CameraStatus.Running && IntPtr.Zero != pstOutput) {
                var stOutput = (MVIDCodeReader.MVID_CAM_OUTPUT_INFO)(Marshal.PtrToStructure(pstOutput,
                    typeof(MVIDCodeReader.MVID_CAM_OUTPUT_INFO)) ?? new MVIDCodeReader.MVID_CAM_OUTPUT_INFO());
                ProcessImage(stOutput);
            }
        }

        /// <summary>
        /// 无解码信息回调
        /// </summary>
        public void ReadImageCallback(MVIDCodeReader.MVID_IMAGE_INFO output, IntPtr user) {
            try {
                var image = ConvertPointerToImage(output);
                if (BindingType is CameraBindingType.OcrCamera && image is not null) {
                    EnqueueOcrFrame(image);
                }
                else {
                    image?.Dispose();
                }
            }
            catch (Exception exception) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = exception
                });
            }
        }

        /// <summary>
        /// 将最新 OCR 帧放入有界内存队列，主动释放过期帧。
        /// </summary>
        private void EnqueueOcrFrame(Bitmap image) {
            while (_ocrBitmapQueue.Count >= 4 && _ocrBitmapQueue.TryDequeue(out var staleImage)) {
                staleImage.Dispose();
            }

            _ocrBitmapQueue.Enqueue(image);
            try {
                _ocrSignal.Release();
            }
            catch (SemaphoreFullException) {
                // 已有工作信号时无需重复唤醒。
            }
        }

        /// <summary>
        /// 清空并释放尚未处理的 OCR 图像。
        /// </summary>
        private void ClearOcrQueue() {
            while (_ocrBitmapQueue.TryDequeue(out var image)) {
                image.Dispose();
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
            else if (BindingType is CameraBindingType.PanoramaCamera or CameraBindingType.OcrCamera) {
                //注册不包含解码信息的回调
                if (_readImageCallback is null) {
                    _readImageCallback = delegate (ref MVIDCodeReader.MVID_IMAGE_INFO output, IntPtr user) {
                        ReadImageCallback(output, user);
                    };
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
                CameraInfo = this.Info,
                Camera = this
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
                ClearOcrQueue();
                Status = CameraStatus.Paused;
            }
            return new KeyValuePair<bool, string>(true, string.Empty);
        }

        public void Dispose() {
            if (Status != CameraStatus.Uninitialized) {
                Status = CameraStatus.Paused;
                _ocrCancellationTokenSource?.Cancel();
                try {
                    _ocrThread?.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) {
                }

                var nRet = _myCodeReader?.MVID_CR_CAM_StopGrabbing_NET() ?? 0;
                if (MVIDCodeReader.MVID_CR_OK != nRet) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = new Exception($"停止相机失败:{_nRet:X}")
                    });
                }

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
                _ocrThread?.Dispose();
                _ocrThread = null;
                _ocrCancellationTokenSource?.Dispose();
                _ocrCancellationTokenSource = null;
                _imageCallback = null;
                _readImageCallback = null;
                _myCodeReader = null;
                ClearOcrQueue();
                this.Info = null;
            }
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            //设置限定读码之类的参数
        }

        /// <summary>
        /// Ocr
        /// </summary>
        public IOcr? Ocr { get; set; }

        public int BarcodeBorderSize { get; set; } = 5;
        public System.Drawing.Color BarcodeBorderColor { get; set; } = System.Drawing.Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;
        public bool IsRealtimeImageEnabled { get; private set; }
        public int TakePhotoDelay { get; set; }

        public event EventHandler<BarcodeReadEventArgs>? BarcodeRead;

        public event EventHandler<OcrResult>? OcrContentRecognized;

        public event EventHandler<BarcodeReadEventArgs>? FilteredBarcodeReturned;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        public void StartRealTimeImage() {
            IsRealtimeImageEnabled = true;
        }

        public void StopRealTimeImage() {
            IsRealtimeImageEnabled = false;
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public async Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default) {
            await Task.Delay(TakePhotoDelay, cancellation);
            if (Status != CameraStatus.Running) {
                return;
            }

            var lockTaken = false;
            try {
                await _takePhotoSlim.WaitAsync(cancellation);
                lockTaken = true;
                var pFrameInfo = new MVIDCodeReader.MVID_IMAGE_INFO();
                _nRet = _myCodeReader?.MVID_CR_CAM_GetImageBuffer_NET(ref pFrameInfo, 10) ?? -1;
                if (_nRet != MVIDCodeReader.MVID_CR_OK) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                        Exception = new Exception($"截图失败:截取一帧图片失败,{_nRet:X}")
                    });
                    return;
                }

                var image = ConvertPointerToImage(pFrameInfo);
                var thumbnailImage = GenerateThumbnail(image);
                OnPhotoTaken(new PhotoTakenEventArgs {
                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    CameraSerialNumber = Info?.SerialNumber ?? string.Empty,
                    Image = image,
                    PhotoTime = DateTime.Now,
                    ThumbImage = (Bitmap?)thumbnailImage,
                    Barcode = barcode,
                    BarcodeTimestamp = barcodeTimestamp
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                throw;
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = new Exception($"截图失败:截取一帧图片异常,{e}")
                });
            }
            finally {
                if (lockTaken) {
                    _takePhotoSlim.Release();
                }
            }
        }

        public async Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default) {
            await Task.Delay(delay, cancellation);
            await TakePhotoAsync(barcode, barcodeTimestamp, cancellation);
        }

        public void SetScanCodeFilterParams(ScanCodeFilterParams @params) {
            _barCodeFilterContainer = new BarCodeFilterContainer {
                Pattern = @params.RegularExpression,
                MaxSize = @params.DuplicateBarcodeFilterCount,
                ExpirationTime = TimeSpan.FromMilliseconds(@params.ScanInterval),
                FilterOutContent = @params.FilterOutContent,
                BarCodeFilterMode = @params.BarCodeFilterMode,
                CustomRegularExpressionItems = @params.CustomRegularExpressionItems,
                IsUseCustomRegexReplacement = @params.IsUseCustomRegexReplacement,
                IsUseFilteredBarcodeTypes = @params.IsUseFilteredBarcodeTypes,
                CustomRegexReplacementItems = @params.CustomRegexReplacementItems
            };
            BarCodeFilterContainer.ResetFilter();
        }

        public async Task<Bitmap> DrawIndicator(Bitmap thumbnail, Size originalSize,
            OcrResult result) {
            var sortedAreas = new List<List<double>>()
            {
                result.BarcodeArea ?? new List<double>(),
                result.RecipientAddressArea ?? new List<double>(),
                result.ThreeSegmentArea ?? new List<double>(),
                result.SenderAddressArea ?? new List<double>()
            };

            sortedAreas.Sort((a, b) => a[1].CompareTo(b[1])); // 根据Y轴值进行排序

            var yOffset = 30; // 初始偏移量
            try {
                await _drawSlim.WaitAsync();
                using var g = Graphics.FromImage(thumbnail);
                foreach (var area in sortedAreas.Where(area => !(area[1] <= 0) && !string.IsNullOrEmpty(GetTextForArea(result, area)))) {
                    // 绘制指示器和文本
                    DrawIndicatorForArea(g, thumbnail, originalSize, area, GetTextForArea(result, area), GetColorForArea(result, area), yOffset);

                    yOffset += 40; // 每个指示器之间的间隔为40
                }
                return thumbnail;
            }
            finally {
                _drawSlim.Release();
            }
        }

        private Color GetColorForArea(OcrResult result, List<double> area) {
            if (area == result.BarcodeArea) {
                return BarcodeBorderColor;
            }
            else if (area == result.RecipientAddressArea) {
                return Color.Orange;
            }
            else if (area == result.ThreeSegmentArea) {
                return Color.DodgerBlue;
            }
            else if (area == result.SenderAddressArea) {
                return Color.OrangeRed;
            }

            return Color.Black; // 默认颜色为黑色
        }

        private string GetTextForArea(OcrResult result, List<double> area) {
            if (area == result.BarcodeArea) {
                return result.BarCode;
            }
            else if (area == result.RecipientAddressArea) {
                return result.RecipientAddress;
            }
            else if (area == result.ThreeSegmentArea) {
                return result.ThreeSegmentCode;
            }
            else if (area == result.SenderAddressArea) {
                return result.SenderAddress;
            }

            return string.Empty;
        }

        private void DrawIndicatorForArea(Graphics g, Image thumbnail, Size originalSize, List<double> areaPoints, string text, Color color, int yOffset) {
            try {
                var imageWidth = originalSize.Width > 0 ? originalSize.Width : 1;
                var imageHeight = originalSize.Height > 0 ? originalSize.Height : 1;

                var convertPoints = ConvertPoint(areaPoints);
                var points = new Point[4];
                for (var i = 0; i < convertPoints.Count; i++) {
                    points[i].X = (int)(convertPoints[i].X * ((float)thumbnail.Size.Width / imageWidth));
                    points[i].Y = (int)(convertPoints[i].Y * ((float)thumbnail.Size.Height / imageHeight));
                }

                using var indicatorPen = new Pen(color, Math.Max(1, BarcodeBorderSize - 4));
                g.DrawPolygon(indicatorPen, points);

                using var font = new System.Drawing.Font("Arial", 12);
                using var brush = new SolidBrush(color);
                using var linePen = new Pen(color);

                // 截断文本
                if (text.Length >= 20) {
                    text = text[..18] + "...";
                }

                //g.DrawString(text, font, brush, 3, yOffset);
                var textWidth = (int)g.MeasureString(text, font).Width;
                var textHeight = (int)g.MeasureString(text, font).Height;

                var lineY = textHeight + yOffset + 3;

                // 判断points[0]坐标在缩略图的左边还是右边
                var isLeftSide = (points[0].X) < thumbnail.Size.Width / 2;

                // 根据判断结果调整绘制位置
                if (isLeftSide) // 如果在左边，靠右绘制
                {
                    var rightMargin = 210;
                    g.DrawString(text, font, brush, thumbnail.Width - textWidth - rightMargin, yOffset);
                    g.DrawLine(linePen, thumbnail.Width - rightMargin, lineY, thumbnail.Width - textWidth - rightMargin, lineY);
                    g.DrawLine(linePen, thumbnail.Width - textWidth - rightMargin, lineY, points[0].X, points[0].Y);
                }
                else // 如果在右边，靠左绘制
                {
                    g.DrawString(text, font, brush, 3, yOffset);
                    g.DrawLine(linePen, 3, lineY, textWidth + 3, lineY);
                    g.DrawLine(linePen, textWidth + 3, lineY, points[0].X, points[0].Y);
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
            }
        }

        private void ProcessImage(MVIDCodeReader.MVID_CAM_OUTPUT_INFO stOutput) {
            //帧时间戳
            var scanTime = DateTime.Now;
            var timestamp = new DateTimeOffset(scanTime).ToUnixTimeMilliseconds();
            if (MVIDCodeReader.MVID_IMAGE_TYPE.MVID_IMAGE_BMP != stOutput.stImage.enImageType) {
                var bitmap = GetBitmap(stOutput);

                var thumbnailImage = GenerateThumbnail(bitmap);
                List<ValidationResult> validationResults = new();
                for (var i = 0; i < stOutput.stCodeList.nCodeNum; ++i) {
                    if (stOutput.stCodeList.stCodeInfo == null) continue;
                    var mvidCodeInfo = stOutput.stCodeList.stCodeInfo[i];
                    var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                        BarCode = mvidCodeInfo.strCode,
                        ScanTime = scanTime
                    });
                    validationResults.Add(validateData);
                }
                if (0 != stOutput.stCodeList.nCodeNum && BindingType != CameraBindingType.PanoramaCamera) {
                    if (IsShowBarcodeBorder && thumbnailImage is not null && thumbnailImage.PixelFormat != PixelFormat.Format8bppIndexed &&
                        stOutput.stCodeList.stCodeInfo?.Any() == true) {
                        //设置图像边框
                        using var g = Graphics.FromImage(thumbnailImage);

                        //画框
                        for (var i = 0; i < stOutput.stCodeList.nCodeNum; ++i) {
                            if (stOutput.stCodeList.stCodeInfo?[i].strCode == null ||
                                stOutput.stCodeList.stCodeInfo?[i].stCornerPt == null) continue;
                            var borderColor = BarcodeBorderColor;
                            var result = validationResults.FirstOrDefault(f =>
                                f.BarCode.Equals(stOutput.stCodeList.stCodeInfo[i].strCode ?? string.Empty));
                            borderColor = result?.FilteredCategory switch {
                                FilteredCategory.RuleFiltered => Color.Red,
                                FilteredCategory.TimeFiltered => Color.DarkOrange,
                                _ => BarcodeBorderColor
                            };

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
                            using var borderPen = new Pen(borderColor, BarcodeBorderSize);
                            g.DrawPolygon(borderPen, stPointList);
                        }
                    }

                    var emittedImageCount = 0;
                    for (var i = 0; i < stOutput.stCodeList.nCodeNum; ++i) {
                        if (stOutput.stCodeList.stCodeInfo != null) {
                            var mvidCodeInfo = stOutput.stCodeList.stCodeInfo[i];
                            var validateData = validationResults.Any(a => a.IsValidationPassed && a.BarCode.Equals(mvidCodeInfo.strCode));
                            if (validateData || !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)) {
                                OnBarcodeRead(new BarcodeReadEventArgs {
                                    Barcode = _barCodeFilterContainer.RegexReplace(
                                        validateData
                                            ? mvidCodeInfo.strCode
                                            : _barCodeFilterContainer.FilterOutContent),
                                    Timestamp = timestamp,
                                    CameraSerialNumber = Structure.chSerialNumber,
                                    ScanTime = scanTime,
                                    ThumbImage = (emittedImageCount > 0 || IsRealtimeImageEnabled) &&
                                                 thumbnailImage is not null
                                        ? new Bitmap(thumbnailImage)
                                        : thumbnailImage,
                                    Image = emittedImageCount > 0 && bitmap is not null
                                        ? new Bitmap(bitmap)
                                        : bitmap,
                                    AreaCoords = [.. Enumerable.Range(0, 4).Select(s => {
                                        if (bitmap != null)
                                            return new System.Drawing.Point {
                                                X = (int)(stOutput.stCodeList.stCodeInfo[i].stCornerPt[s].nX *
                                                    (float)(bitmap.Size.Width) / stOutput.stImage.nWidth),
                                                Y = (int)(stOutput.stCodeList.stCodeInfo[i].stCornerPt[s].nY *
                                                          (float)(bitmap.Size.Height) /
                                                          stOutput.stImage.nHeight)
                                            };
                                        return default;
                                    })],
                                    FrameNo = Interlocked.Read(ref _frameNo)
                                });
                                emittedImageCount++;
                            }
                            /*if (!validateData) {
                                //被过滤的
                                await Task.Factory.StartNew(() => {
                                    OnFilteredBarcodeReturned(new BarcodeReadEventArgs() {
                                        Barcode = _barCodeFilterContainer.RegexReplace(validateData ? mvidCodeInfo.strCode : _barCodeFilterContainer.FilterOutContent),
                                        Timestamp = timestamp,
                                        CameraSerialNumber = this.Structure.chSerialNumber,
                                        ScanTime = scanTime,
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
                                        FrameNo = _frameNo,
                                    });
                                });
                            }*/
                        }

                    }

                    if (emittedImageCount == 0) {
                        bitmap?.Dispose();
                        if (!IsRealtimeImageEnabled) {
                            thumbnailImage?.Dispose();
                        }
                    }

                    Interlocked.Increment(ref _frameNo);
                }
                else {
                    bitmap?.Dispose();
                    if (!IsRealtimeImageEnabled) {
                        thumbnailImage?.Dispose();
                    }
                }
                if (IsRealtimeImageEnabled) {
                    OnRealtimeImage(new RealtimeImageEventArgs() {
                        Timestamp = timestamp,
                        ThumbImage = (Bitmap?)thumbnailImage,
                    });
                }
                //显示图像
                // await Task.Delay(1);
            }
        }

        private Bitmap? GetBitmap(MVIDCodeReader.MVID_CAM_OUTPUT_INFO stOutput) {
            var bitmap = ConvertPointerToImage(stOutput.stImage);
            if (IsOriginalImageOut) {
                return bitmap;
            }

            var thumbnail = (Bitmap?)GenerateThumbnail(bitmap);
            bitmap?.Dispose();
            return thumbnail;
        }

        private Bitmap? ConvertPointerToImage(MVIDCodeReader.MVID_IMAGE_INFO pFrameInfo) {
            if (pFrameInfo.pImageBuf == IntPtr.Zero || pFrameInfo.nImageLen == 0 ||
                pFrameInfo.nWidth <= 0 || pFrameInfo.nHeight <= 0) {
                return null;
            }

            var sourceLength = checked((int)pFrameInfo.nImageLen);
            var isMonochrome =
                MVIDCodeReader.MVID_IMAGE_TYPE.MVID_IMAGE_MONO8 == pFrameInfo.enImageType;
            var stride = checked(pFrameInfo.nWidth * (isMonochrome ? 1 : 3));
            try {
                return CameraImageProcessing.CopyPackedFrame(
                    pFrameInfo.pImageBuf,
                    sourceLength,
                    pFrameInfo.nWidth,
                    pFrameInfo.nHeight,
                    isMonochrome ? PixelFormat.Format8bppIndexed : PixelFormat.Format24bppRgb,
                    stride);
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = e
                });
                return null;
            }
        }

        private static IPAddress ConvertUintToIpAddress(uint ipAddressValue) {
            var addressBytes = BitConverter.GetBytes(ipAddressValue);
            Array.Reverse(addressBytes);

            return new IPAddress(addressBytes);
        }

        private List<Point> ConvertPoint(List<double>? coord) {
            var points = new List<Point>();
            if (coord?.Count == 8) {
                points = [.. Enumerable.Range(0, coord.Count / 2).Select(i => new Point((int)coord[i * 2], (int)coord[i * 2 + 1]))];

                return SortPointsInCounterClockwiseOrder(points);
            }

            return points;
        }

        private List<Point> SortPointsInCounterClockwiseOrder(List<Point> points) {
            // 计算多边形的中心点
            var center = new Point(points.Sum(p => p.X) / points.Count, points.Sum(p => p.Y) / points.Count);

            // 根据相对于中心点的极角排序点
            points.Sort((p1, p2) => {
                var angle1 = Math.Atan2(p1.Y - center.Y, p1.X - center.X);
                var angle2 = Math.Atan2(p2.Y - center.Y, p2.X - center.X);
                return angle1.CompareTo(angle2);
            });

            return points;
        }

        protected virtual void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnBarcodeRead(BarcodeReadEventArgs e) {
            BarcodeRead?.Invoke(this, e);
        }

        protected virtual void OnRealtimeImage(RealtimeImageEventArgs e) {
            RealtimeImage?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e) {
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual void OnCameraStopped(CameraStoppedEventArgs e) {
            Status = CameraStatus.Paused;
            CameraStopped?.Invoke(this, e);
        }

        protected virtual void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            Status = CameraStatus.Uninitialized;
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual void OnCameraDisconnected(CameraConnectionEventArgs e) {
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual void OnPhotoTaken(PhotoTakenEventArgs e) {
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

        /// <summary>
        /// 生成缩略图
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="thumbnailWidth"></param>
        /// <param name="thumbnailHeight"></param>
        /// <returns></returns>
        public static Image? GenerateThumbnail(Image? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        public Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        protected virtual void OnOcrContentRecognized(OcrResult e) {
            OcrContentRecognized?.Invoke(this, e);
        }

        protected virtual void OnFilteredBarcodeReturned(BarcodeReadEventArgs e) {
            FilteredBarcodeReturned?.Invoke(this, e);
        }
    }
}
