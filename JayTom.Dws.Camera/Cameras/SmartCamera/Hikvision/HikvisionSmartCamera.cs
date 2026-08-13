using System;
using System.Net;
using System.Linq;
using System.Text;
using ThridLibray;
using System.Drawing;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Threading;
using MVIDCodeReaderNet;
using MvCodeReaderSDKNet;
using System.IO.Packaging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.Concurrency;
using JayTom.Dws.Camera.FilterContainer;
using static MVIDCodeReaderNet.MVIDCodeReader;
using JayTom.Dws.Camera.Attributes.CameraAttributes;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

namespace JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision {

    public class HikvisionSmartCamera : ISmartCamera {
        private static MvCodeReader.MV_CODEREADER_DEVICE_INFO_LIST _sdkDeviceList = new();
        private MvCodeReader? _mvCodeReader;

        private Task? _barcodeThread;
        private Task? _continuousSoftTriggerThread;

        private MvCodeReader.MV_CODEREADER_DEVICE_INFO _structure;
        private CancellationTokenSource _tokenSource = new();
        private long _frameNo = 0;
        /// <summary>脱离 SDK 拉帧线程执行图像解码和事件发布的无损顺序调度器。</summary>
        private LosslessOrderedDispatcher<HikvisionCapturedFrame>? _frameDispatcher;

        /// <summary>
        /// Ocr图像队列
        /// </summary>
        private readonly ConcurrentQueue<Bitmap> _ocrBitmapQueue = new();
        private readonly SemaphoreSlim _ocrFrameSignal = new(0, 1);

        private Task? _ocrThread;
        private CancellationTokenSource? _ocrCancellationTokenSource;
        private readonly SemaphoreSlim _drawSlim = new(1);
        public CameraInfo? Info { get; private set; } = new();

        //过滤器
        private BarCodeFilterContainer _barCodeFilterContainer = new();

        /// <summary>
        /// 设备列表
        /// </summary>
        private static ConcurrentDictionary<string, CameraInfo> _devInfo = new();

        public HikvisionSmartCamera(CameraInfo info) {
            this.Info = info;
            this.Info.Type = CameraType.SmartCamera;
        }

        public HikvisionSmartCamera() {
        }

        public SdkType SdkType => SdkType.SmartCameraSdk;
        public string SdkName => "MvCodeReaderSDK.Net";

        public bool IsOriginalImageOut { get; set; } = true;
        public CameraStatus Status { get; private set; } = CameraStatus.Uninitialized;
        public CameraBindingType BindingType { get; set; } = CameraBindingType.ScannerCamera;

        public async Task<List<CameraInfo>?> EnumerateCameras() {
            await Task.Yield();
            //枚举相机
            _devInfo.Clear();
            var cameraInfos = new List<CameraInfo>();
            _sdkDeviceList = new MvCodeReader.MV_CODEREADER_DEVICE_INFO_LIST();
            var nRet = MvCodeReader.MV_CODEREADER_EnumDevices_NET(ref _sdkDeviceList, MvCodeReader.MV_CODEREADER_GIGE_DEVICE);
            if (nRet != 0) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"相机枚举异常:{nRet:X}")
                });
                return cameraInfos;
            }

            for (var i = 0; i < _sdkDeviceList.nDeviceNum; i++) {
                var stDevInfo = (MvCodeReader.MV_CODEREADER_DEVICE_INFO)(Marshal.PtrToStructure(_sdkDeviceList.pDeviceInfo[i], typeof(MvCodeReader.MV_CODEREADER_DEVICE_INFO)) ?? new MvCodeReader.MV_CODEREADER_DEVICE_INFO());
                if (stDevInfo.nTLayerType == MvCodeReader.MV_CODEREADER_GIGE_DEVICE) {
                    //网口相机
                    var buffer = Marshal.UnsafeAddrOfPinnedArrayElement(stDevInfo.SpecialInfo.stGigEInfo ?? [], 0);
                    var stGigEDeviceInfo = (MvCodeReader.MV_CODEREADER_GIGE_DEVICE_INFO)(Marshal.PtrToStructure(buffer, typeof(MvCodeReader.MV_CODEREADER_GIGE_DEVICE_INFO)) ?? new MvCodeReader.MV_CODEREADER_GIGE_DEVICE_INFO());
                    var cameraInfo = new CameraInfo() {
                        Brand = stGigEDeviceInfo.chManufacturerName ?? string.Empty,
                        IpAddress = ConvertUintToIpAddress(stGigEDeviceInfo.nCurrentIp).ToString(),
                        Model = stGigEDeviceInfo.chModelName ?? string.Empty,
                        Version = stGigEDeviceInfo.chDeviceVersion ?? string.Empty,
                        SerialNumber =
                            stGigEDeviceInfo.chSerialNumber ?? string.Empty, //还有一个设备序列号nDeviceNumber不想知道是干吗用的
                        Name = stGigEDeviceInfo.chUserDefinedName ?? string.Empty,
                        Type = CameraType.SmartCamera,
                        ConnectionType = stDevInfo.nTLayerType == MvCodeReader.MV_CODEREADER_GIGE_DEVICE
                            ? CameraConnectionType.Ethernet
                            : (stDevInfo.nTLayerType == MvCodeReader.MV_CODEREADER_USB_DEVICE
                                ? CameraConnectionType.Usb
                                : CameraConnectionType.Unknown),
                        Id = i,
                        IsOcrSupported = ((stGigEDeviceInfo.chManufacturerName?.Contains("Hikrobot") == true ||
                                           stGigEDeviceInfo.chManufacturerName?.Contains("Hikvision") == true) &&
                                          stGigEDeviceInfo.chModelName?.StartsWith("MV-ID") == true),
                        SupportedBindingType = CameraBindingType.ScannerCamera |
                                               CameraBindingType.PanoramaCamera | CameraBindingType.OcrCamera
                    };
                    if (cameraInfo.Model.StartsWith("MV-ID")) {
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

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImage;

        public async Task<KeyValuePair<bool, string>> Initialize(object param) {
            await Task.Yield();
            if (_mvCodeReader != null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
            }

            if (param is CameraInfo cameraInfo) {
                this.Info = cameraInfo;
                var tryGetValue = _devInfo.TryGetValue(cameraInfo.SerialNumber, out var devInfo);
                if (tryGetValue && devInfo is not null) {
                    if (cameraInfo.Id >= MvCodeReader.MV_CODEREADER_MAX_XML_SYMBOLIC_NUM) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("初始化失败:Id大于最大设备支持个数!")
                        });
                        return new KeyValuePair<bool, string>(false, "Id大于最大设备支持个数!");
                    }

                    if (_sdkDeviceList.pDeviceInfo[devInfo.Id] == nint.Zero) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception("初始化失败:Id不存在或已断开!")
                        });
                        return new KeyValuePair<bool, string>(false, "Id不存在或已断开!");
                    }

                    var deviceInfo = _sdkDeviceList.pDeviceInfo[devInfo.Id];
                    _structure =
               (MvCodeReader.MV_CODEREADER_DEVICE_INFO)(Marshal.PtrToStructure(deviceInfo,
                   typeof(MvCodeReader.MV_CODEREADER_DEVICE_INFO)) ?? new MvCodeReader.MV_CODEREADER_DEVICE_INFO());
                    //创建对象，打开设备
                    _mvCodeReader ??= new MvCodeReader();
                    //创建句柄
                    int nRet = _mvCodeReader.MV_CODEREADER_CreateHandle_NET(ref _structure);
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
                    //获取相机属性
                    //("Width", ref nIntValue) ?? 0;

                    var nIntValue = new MvCodeReader.MV_CODEREADER_INTVALUE_EX();
                    nRet = _mvCodeReader.MV_CODEREADER_GetIntValue_NET("Width", ref nIntValue);
                    if (nRet != MvCodeReader.MV_CODEREADER_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:获取相机属性值[Width]失败,{nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"获取相机属性值[Width]失败,{nRet:X}!");
                    }
                    var nWidth = (int)nIntValue.nCurValue;
                    nRet = _mvCodeReader.MV_CODEREADER_GetIntValue_NET("Height", ref nIntValue);
                    if (nRet != MvCodeReader.MV_CODEREADER_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:获取相机属性值[Height]失败,{nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"获取相机属性值[Height]失败,{nRet:X}!");
                    }
                    var nHeight = (int)nIntValue.nCurValue;
                    //("Width", ref nIntValue)
                    //设置采集模式
                    if (IsUseTriggerMode) {
                        nRet = _mvCodeReader.MV_CODEREADER_SetEnumValue_NET("TriggerMode", (uint)MvCodeReader.MV_CODEREADER_TRIGGER_MODE.MV_CODEREADER_TRIGGER_MODE_ON);
                        if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                            _mvCodeReader.MV_CODEREADER_DestroyHandle_NET();
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception($"初始化失败:设置采集模式失败,{nRet:X}")
                            });
                            return new KeyValuePair<bool, string>(false, $"设置采集模式失败,{nRet:X}!");
                        }

                        if (TriggerMode == TriggerMode.Software) {
                            nRet = _mvCodeReader.MV_CODEREADER_SetEnumValue_NET("TriggerSource", (uint)MvCodeReader.MV_CODEREADER_TRIGGER_SOURCE.MV_CODEREADER_TRIGGER_SOURCE_SOFTWARE);
                            if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                                _mvCodeReader.MV_CODEREADER_DestroyHandle_NET();
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception($"初始化失败:设置采集模式失败,{nRet:X}")
                                });
                                return new KeyValuePair<bool, string>(false, $"设置采集模式失败,{nRet:X}!");
                            }
                        }
                        else {
                            //管脚
                            //(uint)MvCodeReader.MV_CODEREADER_TRIGGER_SOURCE.MV_CODEREADER_TRIGGER_SOURCE_LINE0;
                            nRet = _mvCodeReader.MV_CODEREADER_SetEnumValue_NET("TriggerSource", (uint)SourceLine);
                            if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                                _mvCodeReader.MV_CODEREADER_DestroyHandle_NET();
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception($"初始化失败:设置采集模式失败,{nRet:X}")
                                });
                                return new KeyValuePair<bool, string>(false, $"设置采集模式失败,{nRet:X}!");
                            }
                        }
                    }
                    else {
                        nRet = _mvCodeReader.MV_CODEREADER_SetEnumValue_NET("TriggerMode", (uint)MvCodeReader.MV_CODEREADER_TRIGGER_MODE.MV_CODEREADER_TRIGGER_MODE_OFF);
                        if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                            _mvCodeReader.MV_CODEREADER_DestroyHandle_NET();
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception($"初始化失败:设置采集模式失败,{nRet:X}")
                            });
                            return new KeyValuePair<bool, string>(false, $"设置采集模式失败,{nRet:X}!");
                        }
                    }

                    /*
                    nRet = _mvCodeReader.MV_CODEREADER_RegisterImageCallBackEx2_NET(ImageCallbackFunc, IntPtr.Zero);
                    if (nRet != MvCodeReader.MV_CODEREADER_OK) {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"初始化失败:注册回调函数失败,{nRet:X}")
                        });
                        return new KeyValuePair<bool, string>(false, $"注册回调函数失败,{nRet:X}!");
                    }
                    */

                    //获取参数ExposureTime
                    //获取参数Gain
                    //获取参数AcquisitionFrameRate
                    //注册回调函数
                    EnsureFrameDispatcher();
                    _tokenSource = new CancellationTokenSource();
                    _barcodeThread = Task.Run(() => BarcodeCallbackThread(_tokenSource.Token));

                    if (TriggerMode == TriggerMode.Software) {
                        _continuousSoftTriggerThread =
                            Task.Run(() => ContinuousSoftTrigger(200, _tokenSource.Token));
                    }

                    OnCameraInitialized(new CameraInitializedEventArgs() {
                        CameraInfo = this.Info
                    });

                    //注册Ocr线程
                    if (this.BindingType is CameraBindingType.OcrCamera) {
                        //nRet = _mvCodeReader.MV_CODEREADER_SetWayBillEnable_NET(true);
                        //NLog.LogManager.GetCurrentClassLogger().Error($"设置抠图:{nRet:X2}");
                        if (Ocr is not null) {
                            var (key, value) = await Ocr.Initialize();
                            if (key) {
                                _ocrCancellationTokenSource = new CancellationTokenSource();
                                _ocrThread =
                                    Task.Run(() => OcrCallbackThread(_ocrCancellationTokenSource.Token));
                            }
                            else {
                                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                    Exception = new Exception($"Ocr初始化失败:{value}!")
                                });
                                return new KeyValuePair<bool, string>(false, $"Ocr初始化失败:{value}!");
                            }

                            //创建推理回调
                            Ocr.OcrContentRecognized += OcrOnOcrContentRecognized;
                        }
                        else {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception("Ocr对象未初始化!")
                            });
                            return new KeyValuePair<bool, string>(false, "Ocr对象未初始化!");
                        }
                    }
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
        /// Ocr推理回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OcrOnOcrContentRecognized(object? sender, OcrResult e) {
            if (e?.Image != null) {
                var thumbnail = GenerateThumbnail(e.Image);
                if (!string.IsNullOrEmpty(e.BarCode)) {
                    //过滤
                    var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                        BarCode = e.BarCode,
                        ScanTime = DateTime.Now
                    });
                    if (validateData.IsValidationPassed) {
                        e.CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty;
                        //画框
                        /*if (IsShowBarcodeBorder && thumbnail is not null &&
                            thumbnail.PixelFormat != PixelFormat.Format8bppIndexed &&
                            e.IsSuccess) {
                            //暂时屏蔽画框
                            thumbnail = await DrawIndicator(thumbnail, new Size(e.Image.Width, e.Image.Height), e);
                        }*/
                        e.Thumbnail = thumbnail;
                        e.BarCode = _barCodeFilterContainer.RegexReplace(e.BarCode);
                        OnOcrContentRecognized(e);
                    }
                    else {
                        e.Image.Dispose();
                        if (!IsRealtimeImageEnabled) {
                            thumbnail?.Dispose();
                        }
                    }
                }
                else {
                    e.Image.Dispose();
                    if (!IsRealtimeImageEnabled) {
                        thumbnail?.Dispose();
                    }
                }
                if (IsRealtimeImageEnabled) {
                    OnRealtimeImage(new RealtimeImageEventArgs() {
                        ThumbImage = thumbnail,
                        Timestamp = e?.SubmitTimestamp ?? 0
                    });
                }
            }
        }

        public async Task<KeyValuePair<bool, string>> Start(object param) {
            await Task.Yield();
            // ch:开始采集 | en:Start Grabbing
            int nRet = _mvCodeReader?.MV_CODEREADER_StartGrabbing_NET() ?? 0;
            if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception($"启动失败,{nRet:X}")
                });
                return new KeyValuePair<bool, string>(false, $"启动失败,{nRet:X}");
            }

            OnCameraStarted(new CameraStartedEventArgs() {
                CameraInfo = this.Info,
                Camera = this
            });
            return new KeyValuePair<bool, string>(true, $"启动成功");
        }

        public Task<KeyValuePair<bool, string>> Stop() {
            if (_mvCodeReader is null || Status is CameraStatus.Uninitialized or CameraStatus.Paused) {
                return Task.FromResult(
                    new KeyValuePair<bool, string>(true, "相机已停止"));
            }

            var result = _mvCodeReader.MV_CODEREADER_StopGrabbing_NET();
            if (result != MvCodeReader.MV_CODEREADER_OK) {
                return Task.FromResult(
                    new KeyValuePair<bool, string>(false, $"停止失败,{result:X}"));
            }

            Status = CameraStatus.Paused;
            CameraStopped?.Invoke(this, new CameraStoppedEventArgs {
                CameraInfo = Info
            });
            return Task.FromResult(
                new KeyValuePair<bool, string>(true, "停止成功"));
        }

        public void Dispose() {
            if (Status != CameraStatus.Uninitialized) {
                //注销线程
                _tokenSource.Cancel();
                //停止SDK
                _mvCodeReader?.MV_CODEREADER_StopGrabbing_NET();
                _mvCodeReader?.MV_CODEREADER_CloseDevice_NET();
                _mvCodeReader?.MV_CODEREADER_DestroyHandle_NET();
                if (_continuousSoftTriggerThread is not null) {
                    WaitForWorker(_continuousSoftTriggerThread);
                    _continuousSoftTriggerThread.Dispose();
                    _continuousSoftTriggerThread = null;
                }

                if (_barcodeThread is not null) {
                    WaitForWorker(_barcodeThread);
                    _barcodeThread.Dispose();
                    _barcodeThread = null;
                }
                _frameDispatcher?.Dispose();
                _frameDispatcher = null;
                //置空对象
                _mvCodeReader = null;
                OnCameraDisconnected(new CameraConnectionEventArgs() {
                    CameraInfo = this.Info
                });
                OnCameraUnregistered(new CameraUnregisteredEventArgs() {
                    CameraInfo = this.Info
                });
                _ocrCancellationTokenSource?.Cancel();
                try {
                    _ocrFrameSignal.Release();
                }
                catch (SemaphoreFullException) {
                }
                if (_ocrThread is not null) {
                    WaitForWorker(_ocrThread);
                    _ocrThread.Dispose();
                    _ocrThread = null;
                }
                ClearOcrQueue();
                this.Info = null;
            }
        }

        private static void WaitForWorker(Task worker) {
            try {
                worker.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) {
            }
        }

        private void ClearOcrQueue() {
            while (_ocrBitmapQueue.TryDequeue(out var bitmap)) {
                bitmap.Dispose();
            }
        }

        /// <summary>
        /// 图像回调
        /// </summary>
        /// <param name="pData"></param>
        /// <param name="pstFrameInfoEx2"></param>
        /// <param name="pUser"></param>
        public void ImageCallbackFunc(IntPtr pData, IntPtr pstFrameInfoEx2, IntPtr pUser) {
            var scanTime = DateTime.Now;
            var frameInfo = (MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)(Marshal.PtrToStructure(pstFrameInfoEx2, typeof(MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)) ?? new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2());
            var barcodeResult = (MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2)(Marshal.PtrToStructure(frameInfo.UnparsedBcrList.pstCodeListEx2, typeof(MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2)) ?? new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2());
            var frameNo = Interlocked.Increment(ref _frameNo);
            QueueCapturedFrame(pData, frameInfo, barcodeResult, scanTime, frameNo);
        }

        private void HandleBarcodeReading(Bitmap? bmp, MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 stFrameInfo,
            MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 stBcrResult, DateTime scanTime, long timestamp,
            long frameNo,
            List<BarcodeTriggeredEventArgs>? filteredBarcodes = null) {
            try {
                //智能相机没有纯图像回调,暂时先写在这里
                if (this.BindingType is CameraBindingType.OcrCamera) {
                    //调用Ocr
                    //添加图片到识别队列
                    //抠图
                    if (bmp is not null) {
                        while (_ocrBitmapQueue.Count >= 3 &&
                               _ocrBitmapQueue.TryDequeue(out var staleBitmap)) {
                            staleBitmap.Dispose();
                        }
                        _ocrBitmapQueue.Enqueue(bmp);
                        try {
                            _ocrFrameSignal.Release();
                        }
                        catch (SemaphoreFullException) {
                        }
                    }
                }
                else {
                    var thumbnailImage = IsOriginalImageOut
                        ? GenerateThumbnail(bmp)
                        : bmp is null
                            ? null
                            : new Bitmap(bmp);
                    if (stBcrResult.nCodeNum > 0) {
                        //画区域
                        if (IsShowBarcodeBorder && thumbnailImage is not null &&
                            thumbnailImage.PixelFormat != PixelFormat.Format8bppIndexed &&
                            stBcrResult.stBcrInfoEx2 is { Length: > 0 }) {
                            using var g = Graphics.FromImage(thumbnailImage);
                            using var pen = new Pen(BarcodeBorderColor, BarcodeBorderSize);
                            var barcodeCount = Math.Min(
                                checked((int)stBcrResult.nCodeNum),
                                stBcrResult.stBcrInfoEx2.Length);
                            for (var i = 0; i < barcodeCount; i++) {
                                var points = new Point[4];
                                for (var j = 0; j < 4; ++j) {
                                    points[j].X = (int)(stBcrResult.stBcrInfoEx2[i].pt[j].x *
                                        (float)(thumbnailImage.Size.Width) / stFrameInfo.nWidth);
                                    points[j].Y = (int)(stBcrResult.stBcrInfoEx2[i].pt[j].y *
                                        (float)(thumbnailImage.Size.Height) / stFrameInfo.nHeight);
                                }
                                g.DrawPolygon(pen, points);
                            }
                        }

                        var barcodeTriggeredEventArgsList = filteredBarcodes ??
                            FilterBarcodes(
                                stBcrResult,
                                scanTime,
                                timestamp,
                                bmp,
                                thumbnailImage,
                                stFrameInfo,
                                frameNo);
                        if (filteredBarcodes is not null) {
                            AttachBarcodeImages(
                                barcodeTriggeredEventArgsList,
                                bmp,
                                thumbnailImage,
                                stFrameInfo);
                        }
                        if (barcodeTriggeredEventArgsList.Count > 0) {
                            foreach (var barcodeTriggeredEventArgse in barcodeTriggeredEventArgsList) {
                                OnBarcodeReadTriggered(barcodeTriggeredEventArgse);
                            }
                        }
                        else {
                            bmp?.Dispose();
                            if (!IsRealtimeImageEnabled) {
                                thumbnailImage?.Dispose();
                            }
                        }
                    }
                    else {
                        //如果没读到条码
                        if (TriggerMode == TriggerMode.Hardware) {
                            OnNotBarcodeHitEvent(new BarcodeReadEventArgs() {
                                Timestamp = timestamp,
                                Barcode = "NoRead",
                                Image = bmp,
                                ThumbImage = IsRealtimeImageEnabled && thumbnailImage is not null
                                    ? new Bitmap(thumbnailImage)
                                    : thumbnailImage,
                                CameraSerialNumber = this.Info?.SerialNumber ?? string.Empty,
                                ScanTime = scanTime,
                                FrameNo = frameNo
                            });
                        }
                        else {
                            bmp?.Dispose();
                            if (!IsRealtimeImageEnabled) {
                                thumbnailImage?.Dispose();
                            }
                        }
                    }

                    if (IsRealtimeImageEnabled) {
                        OnRealtimeImage(new RealtimeImageEventArgs() {
                            ThumbImage = (Bitmap?)thumbnailImage,
                            Timestamp = timestamp
                        });
                    }
                }
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("解析帧数据异常", e)
                });
            }
        }

        public void SetParameters(Dictionary<string, object> parameters) {
            throw new NotImplementedException();
        }

        public bool IsRealtimeImageEnabled { get; private set; }

        public void StartRealTimeImage() {
            IsRealtimeImageEnabled = true;
        }

        public void StopRealTimeImage() {
            IsRealtimeImageEnabled = false;
        }

        public event EventHandler<PhotoTakenEventArgs>? PhotoTaken;

        public Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, CancellationToken cancellation = default) {
            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                Exception = new Exception("没有实现拍照方法")
            });
            return Task.CompletedTask;
        }

        public Task TakePhotoAsync(string barcode, long packageTimestampMilliseconds, TimeSpan delay, CancellationToken cancellation = default) {
            throw new NotImplementedException();
        }

        public int TakePhotoDelay { get; set; }

        /// <summary>
        /// Ocr
        /// </summary>
        public IOcr? Ocr { get; set; }

        public int BarcodeBorderSize { get; set; } = 5;
        public bool IsHideNoRead { get; set; } = false;
        public Color BarcodeBorderColor { get; set; } = Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;
        public bool IsUseTriggerMode { get; set; } = true;
        public TriggerMode TriggerMode { get; set; } = TriggerMode.Hardware;
        public int SourceLine { get; set; } = 0;
        public bool IsMergeBarCodes { get; set; } = true;
        public string MultiBarcodeDelimiter { get; set; } = "_";

        public void SoftwareTriggerOnce() {
            if (IsUseTriggerMode && TriggerMode == TriggerMode.Software) {
                var result = _mvCodeReader?.MV_CODEREADER_SetCommandValue_NET("TriggerSoftware") ?? 0;
                if (MvCodeReader.MV_CODEREADER_OK != result) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                        Exception = new Exception($"软触发异常:{result:X}")
                    });
                }
                return;
            }

            OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                Exception = new Exception("需要初始化时使用触发模式，并且使用软触发才能生效")
            });
        }

        public event EventHandler<BarcodeTriggeredEventArgs>? BarcodeReadTriggered;

        public event EventHandler<BarcodeReadEventArgs>? FilteredBarcodeReturned;

        public event EventHandler<BarcodeReadEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<OcrResult>? OcrContentRecognized;

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

        private static IPAddress ConvertUintToIpAddress(uint ipAddressValue) {
            var addressBytes = BitConverter.GetBytes(ipAddressValue);
            Array.Reverse(addressBytes);

            return new IPAddress(addressBytes);
        }

        private async Task ContinuousSoftTrigger(int intervalTime, CancellationToken token) {
            if (intervalTime <= 0) {
                intervalTime = 100;
            }
            while (!token.IsCancellationRequested) {
                try {
                    if (IsUseTriggerMode && TriggerMode == TriggerMode.Software) {
                        var nRet = _mvCodeReader?.MV_CODEREADER_SetCommandValue_NET("TriggerSoftware") ?? 0;
                        if (MvCodeReader.MV_CODEREADER_OK != nRet) {
                            OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                                Exception = new Exception($"软触发异常:{nRet:X}")
                            });
                        }
                    }
                    else {
                        OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                            Exception = new Exception($"需要初始化时使用触发模式，并且使用软触发才能生效")
                        });
                    }
                }
                catch (Exception e) {
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                        Exception = e
                    });
                }

                await Task.Delay(intervalTime, token);
            }
        }

        /// <summary>确保图像解码和事件发布运行在独立的相机帧处理线程上。</summary>
        private void EnsureFrameDispatcher() {
            _frameDispatcher ??= new LosslessOrderedDispatcher<HikvisionCapturedFrame>(
                ProcessCapturedFrame,
                (frame, exception) => {
                    frame.Buffer.Dispose();
                    OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                        Exception = new Exception("后台处理海康扫码帧异常", exception)
                    });
                });
        }

        /// <summary>仅复制 SDK 指针数据并立即入队，不在拉帧线程执行解码、缩略图或用户事件。</summary>
        private void QueueCapturedFrame(
            IntPtr source,
            MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 frameInfo,
            MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 barcodeResult,
            DateTime scanTime,
            long frameNo) {
            PooledFrameBuffer? buffer = null;
            try {
                EnsureFrameDispatcher();
                buffer = PooledFrameBuffer.CopyFrom(source, checked((int)frameInfo.nFrameLen));
                var frame = new HikvisionCapturedFrame(
                    buffer,
                    frameInfo,
                    barcodeResult,
                    scanTime,
                    new DateTimeOffset(scanTime).ToUnixTimeMilliseconds(),
                    frameNo);
                if (_frameDispatcher?.TryEnqueue(frame) == true) {
                    buffer = null;
                }
            }
            catch (Exception exception) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs {
                    Exception = new Exception("复制海康扫码帧异常", exception)
                });
            }
            finally {
                buffer?.Dispose();
            }
        }

        /// <summary>在独立长驻线程中按收帧顺序执行图像解码、过滤和事件发布。</summary>
        private void ProcessCapturedFrame(HikvisionCapturedFrame frame) {
            using (frame.Buffer) {
                if (BindingType != CameraBindingType.OcrCamera &&
                    !HasFrameConsumer(frame.BarcodeResult)) {
                    return;
                }

                List<BarcodeTriggeredEventArgs>? filteredBarcodes = null;
                if (BindingType != CameraBindingType.OcrCamera &&
                    frame.BarcodeResult.nCodeNum > 0) {
                    filteredBarcodes = FilterBarcodes(
                        frame.BarcodeResult,
                        frame.ScanTime,
                        frame.Timestamp,
                        null,
                        null,
                        frame.FrameInfo,
                        frame.FrameNo);
                    if (filteredBarcodes.Count == 0 && !IsRealtimeImageEnabled) {
                        return;
                    }
                }

                var bitmap = GetBitmap(frame.Buffer.Buffer, frame.Buffer.Length, frame.FrameInfo);
                HandleBarcodeReading(
                    bitmap,
                    frame.FrameInfo,
                    frame.BarcodeResult,
                    frame.ScanTime,
                    frame.Timestamp,
                    frame.FrameNo,
                    filteredBarcodes);
            }
        }

        /// <summary>在构造 Bitmap 前判断 SDK 元数据是否存在任何业务或预览消费者。</summary>
        private bool HasFrameConsumer(
            MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2 barcodeResult) {
            if (IsRealtimeImageEnabled && RealtimeImage is not null) {
                return true;
            }
            if (barcodeResult.nCodeNum <= 0) {
                return TriggerMode == TriggerMode.Hardware && NotBarcodeHitEvent is not null;
            }
            return BarcodeReadTriggered is not null;
        }

        private async Task BarcodeCallbackThread(CancellationToken token) {
            var pData = IntPtr.Zero;
            var stFrameInfoEx2 = new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2();
            var pstFrameInfoEx2 =
                Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)));
            try {
                Marshal.StructureToPtr(stFrameInfoEx2, pstFrameInfoEx2, false);
                while (!token.IsCancellationRequested) {
                    if (Status != CameraStatus.Running) {
                        await Task.Delay(100, token);
                        continue;
                    }

                    var result = _mvCodeReader?.MV_CODEREADER_GetOneFrameTimeoutEx2_NET(
                        ref pData,
                        pstFrameInfoEx2,
                        500) ?? 0;
                    if (result != MvCodeReader.MV_CODEREADER_OK) {
                        continue;
                    }

                    var scanTime = DateTime.Now;
                    stFrameInfoEx2 =
                        (MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)(
                            Marshal.PtrToStructure(
                                pstFrameInfoEx2,
                                typeof(MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2)) ??
                            new MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2());
                    if (stFrameInfoEx2.nFrameLen <= 0) {
                        continue;
                    }

                    var frameNo = Interlocked.Increment(ref _frameNo);
                    var barcodeResult =
                        (MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2)(
                            Marshal.PtrToStructure(
                                stFrameInfoEx2.UnparsedBcrList.pstCodeListEx2,
                                typeof(MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2)) ??
                            new MvCodeReader.MV_CODEREADER_RESULT_BCR_EX2());
                    if (!(IsHideNoRead && barcodeResult.nCodeNum < 1)) {
                        QueueCapturedFrame(pData, stFrameInfoEx2, barcodeResult, scanTime, frameNo);
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) {
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = new Exception("取码回调线程异常", e)
                });
            }
            finally {
                Marshal.FreeHGlobal(pstFrameInfoEx2);
            }
        }

        public List<BarcodeTriggeredEventArgs> FilterBarcodes(MvCodeReader.
            MV_CODEREADER_RESULT_BCR_EX2 stBcrResultEx2, DateTime scanTime, long timestamp,
            Bitmap? bmp, Bitmap? thumbnailImage, MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 stFrameInfoEx2,
            long? capturedFrameNo = null) {
            var barcodeInfos = stBcrResultEx2.stBcrInfoEx2;
            if (barcodeInfos is null || barcodeInfos.Length == 0 || stBcrResultEx2.nCodeNum <= 0) {
                return [];
            }

            var barcodeCount = Math.Min(
                checked((int)stBcrResultEx2.nCodeNum),
                barcodeInfos.Length);
            var triggeredEventArgsList = new List<BarcodeTriggeredEventArgs>(barcodeCount);
            var emittedBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            BarcodeTriggeredEventArgs? firstCandidate = null;
            var serialNumber = Info?.SerialNumber ?? string.Empty;
            var frameNo = capturedFrameNo ?? Interlocked.Read(ref _frameNo);
            for (var i = 0; i < barcodeCount; i++) {
                var barcodeInfo = barcodeInfos[i];
                var barcode = Encoding.Default.GetString(barcodeInfo.chCode ?? []).TrimEnd('\0');
                var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo() {
                    BarCode = string.IsNullOrWhiteSpace(barcode) ? "NoRead" : barcode,
                    ScanTime = scanTime
                });
                if (validateData.IsValidationPassed || !string.IsNullOrWhiteSpace(_barCodeFilterContainer.FilterOutContent)) {
                    var outputBarcode = _barCodeFilterContainer.RegexReplace(
                        (validateData.IsValidationPassed
                            ? barcode
                            : _barCodeFilterContainer.FilterOutContent) ?? "NoRead");
                    var areaCoordinates = new List<Point>(4);
                    for (var pointIndex = 0; pointIndex < 4; pointIndex++) {
                        if (bmp is { Width: > 0, Height: > 0 } &&
                            stFrameInfoEx2.nWidth > 0 && stFrameInfoEx2.nHeight > 0) {
                            areaCoordinates.Add(new Point {
                                X = (int)(barcodeInfo.pt[pointIndex].x * (float)bmp.Width / stFrameInfoEx2.nWidth),
                                Y = (int)(barcodeInfo.pt[pointIndex].y * (float)bmp.Height / stFrameInfoEx2.nHeight)
                            });
                        }
                        else {
                            areaCoordinates.Add(new Point {
                                X = (int)barcodeInfo.pt[pointIndex].x,
                                Y = (int)barcodeInfo.pt[pointIndex].y
                            });
                        }
                    }

                    var eventArgs = new BarcodeTriggeredEventArgs {
                        Timestamp = timestamp,
                        TotalProcCost = (int)barcodeInfo.nTotalProcCost,
                        AlgoCost = barcodeInfo.sAlgoCost,
                        Ppm = barcodeInfo.sPPM,
                        BarType = GetBarType((MvCodeReader.MV_CODEREADER_CODE_TYPE)barcodeInfo.nBarType),
                        Barcode = outputBarcode,
                        Image = bmp,
                        ThumbImage = thumbnailImage,
                        AppearCount = barcodeInfo.sAppearCount,
                        Angle = barcodeInfo.nAngle,
                        CodeIdentifier = barcodeInfo.nSubPackageId.ToString(),
                        Len = (int)barcodeInfo.nLen,
                        CameraSerialNumber = serialNumber,
                        ScanTime = scanTime,
                        AreaCoords = areaCoordinates,
                        FrameNo = frameNo
                    };
                    firstCandidate ??= eventArgs;
                    if (!outputBarcode.Equals(
                            _barCodeFilterContainer.FilterOutContent,
                            StringComparison.OrdinalIgnoreCase) &&
                        emittedBarcodes.Add(outputBarcode)) {
                        triggeredEventArgsList.Add(eventArgs);
                    }
                }
            }
            if (triggeredEventArgsList.Count == 0 && firstCandidate is not null) {
                triggeredEventArgsList.Add(firstCandidate);
            }

            for (var index = 0; index < triggeredEventArgsList.Count; index++) {
                var eventArgs = triggeredEventArgsList[index];
                if (index > 0 && bmp is not null) {
                    eventArgs.Image = new Bitmap(bmp);
                }
                if ((index > 0 || IsRealtimeImageEnabled) && thumbnailImage is not null) {
                    eventArgs.ThumbImage = new Bitmap(thumbnailImage);
                }
            }

            return triggeredEventArgsList;
        }

        /// <summary>在元数据过滤完成后一次性附加图像并把原始坐标缩放到输出位图。</summary>
        private void AttachBarcodeImages(
            List<BarcodeTriggeredEventArgs> barcodes,
            Bitmap? bitmap,
            Bitmap? thumbnail,
            MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 frameInfo) {
            for (var index = 0; index < barcodes.Count; index++) {
                var barcode = barcodes[index];
                if (bitmap is { Width: > 0, Height: > 0 } &&
                    frameInfo.nWidth > 0 && frameInfo.nHeight > 0 &&
                    barcode.AreaCoords is not null) {
                    for (var pointIndex = 0; pointIndex < barcode.AreaCoords.Count; pointIndex++) {
                        var point = barcode.AreaCoords[pointIndex];
                        barcode.AreaCoords[pointIndex] = new Point {
                            X = (int)(point.X * (long)bitmap.Width / frameInfo.nWidth),
                            Y = (int)(point.Y * (long)bitmap.Height / frameInfo.nHeight)
                        };
                    }
                }
                barcode.Image = index > 0 && bitmap is not null
                    ? new Bitmap(bitmap)
                    : bitmap;
                barcode.ThumbImage = (index > 0 || IsRealtimeImageEnabled) && thumbnail is not null
                    ? new Bitmap(thumbnail)
                    : thumbnail;
            }
        }

        /// <summary>
        /// Ocr回调处理逻辑
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task OcrCallbackThread(CancellationToken token) {
            try {
                while (!token.IsCancellationRequested) {
                    await _ocrFrameSignal.WaitAsync(token);
                    Bitmap? bitmap = null;
                    while (_ocrBitmapQueue.TryDequeue(out var queuedBitmap)) {
                        bitmap?.Dispose();
                        bitmap = queuedBitmap;
                    }
                    if (bitmap is null) {
                        continue;
                    }

                    var result = Ocr?.ParseOcrResult(bitmap);
                    var thumbnail = GenerateThumbnail(bitmap);
                    if (result is not null &&
                        !string.IsNullOrEmpty(result.BarCode) &&
                        result.IsSuccess) {
                        ClearOcrQueue();
                        var validateData = _barCodeFilterContainer.ValidateData(new BarCodeFilterInfo {
                            BarCode = result.BarCode,
                            ScanTime = DateTime.Now
                        });
                        if (validateData.IsValidationPassed) {
                            result.CameraSerialNumber = Info?.SerialNumber ?? string.Empty;
                            if (IsShowBarcodeBorder &&
                                thumbnail is not null &&
                                thumbnail.PixelFormat != PixelFormat.Format8bppIndexed) {
                                thumbnail = await DrawIndicator(
                                    thumbnail,
                                    new Size(bitmap.Width, bitmap.Height),
                                    result);
                            }
                            result.Image ??= bitmap;
                            result.Thumbnail = thumbnail;
                            result.BarCode = _barCodeFilterContainer.RegexReplace(result.BarCode);
                            OnOcrContentRecognized(result);
                        }
                        else {
                            result.Image?.Dispose();
                            if (result.Image != bitmap) {
                                bitmap.Dispose();
                            }
                            if (!IsRealtimeImageEnabled) {
                                thumbnail?.Dispose();
                            }
                        }
                    }
                    else if (TriggerMode == TriggerMode.Hardware) {
                        OnOcrContentRecognized(new OcrResult {
                            BarCode = "NoRead",
                            Image = bitmap,
                            Thumbnail = thumbnail,
                            CropImage = result?.CropImage,
                            CameraSerialNumber = Info?.SerialNumber ?? string.Empty,
                            ElapsedTime = result?.ElapsedTime ?? 0,
                            RecognitionTime = result?.RecognitionTime ?? DateTime.Now,
                            RecognitionTimestamp = result?.RecognitionTimestamp ??
                                                   new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()
                        });
                    }
                    else {
                        result?.Image?.Dispose();
                        if (result?.Image != bitmap) {
                            bitmap.Dispose();
                        }
                        if (!IsRealtimeImageEnabled) {
                            thumbnail?.Dispose();
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
            catch (OperationCanceledException) when (token.IsCancellationRequested) {
            }
        }

        public async Task<Bitmap> DrawIndicator(Bitmap thumbnail, Size originalSize,
           OcrResult result) {
            var sortedAreas = new List<List<decimal>>()
            {
                result.BarcodeArea ?? new List<decimal>(),
                result.RecipientAddressArea ?? new List<decimal>(),
                result.ThreeSegmentArea ?? new List<decimal>(),
                result.SenderAddressArea ?? new List<decimal>()
            };

            var yOffset = 30; // 初始偏移量
            try {
                await _drawSlim.WaitAsync();
                sortedAreas.Sort((a, b) => a[1].CompareTo(b[1])); // 根据Y轴值进行排序
                using var g = Graphics.FromImage(thumbnail);
                foreach (var area in sortedAreas.Where(area => !(area[1] <= 0) && !string.IsNullOrEmpty(GetTextForArea(result, area)))) {
                    // 绘制指示器和文本
                    DrawIndicatorForArea(g, thumbnail, originalSize, area, GetTextForArea(result, area), GetColorForArea(result, area), yOffset);

                    yOffset += 40; // 每个指示器之间的间隔为40
                }
            }
            finally {
                _drawSlim.Release();
            }
            return thumbnail;
        }

        private Color GetColorForArea(OcrResult result, List<decimal> area) {
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

        private string GetTextForArea(OcrResult result, List<decimal> area) {
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

        private void DrawIndicatorForArea(Graphics g, Image thumbnail, Size originalSize, List<decimal> areaPoints, string text, Color color, int yOffset) {
            try {
                var imageWidth = originalSize.Width > 0 ? originalSize.Width : 1;
                var imageHeight = originalSize.Height > 0 ? originalSize.Height : 1;

                var convertPoints = ConvertPoint(areaPoints);
                var points = new Point[4];
                for (var i = 0; i < convertPoints.Count; i++) {
                    points[i].X = (int)(convertPoints[i].X * ((float)thumbnail.Size.Width / imageWidth));
                    points[i].Y = (int)(convertPoints[i].Y * ((float)thumbnail.Size.Height / imageHeight));
                }

                using var borderPen = new Pen(color, Math.Max(1, BarcodeBorderSize - 4));
                g.DrawPolygon(borderPen, points);

                using var font = new Font("Arial", 12);
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

        private List<Point> ConvertPoint(List<decimal>? coord) {
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

        /// <summary>
        /// 获取图像
        /// </summary>
        /// <param name="pData"></param>
        /// <param name="stFrameInfoEx2"></param>
        /// <returns></returns>
        private Bitmap? GetBitmap(byte[] frameBuffer, int frameLength,
            MvCodeReader.MV_CODEREADER_IMAGE_OUT_INFO_EX2 stFrameInfoEx2) {
            try {
                Bitmap? bitmap = null;
                switch (stFrameInfoEx2.enPixelType) {
                    case MvCodeReader.MvCodeReaderGvspPixelType.PixelType_CodeReader_Gvsp_Mono8: {
                            bitmap = CameraImageProcessing.CopyPackedFrame(
                                frameBuffer,
                                frameLength,
                                stFrameInfoEx2.nWidth,
                                stFrameInfoEx2.nHeight,
                                PixelFormat.Format8bppIndexed,
                                stFrameInfoEx2.nWidth);
                            break;
                        }
                    case MvCodeReader.MvCodeReaderGvspPixelType.PixelType_CodeReader_Gvsp_Jpeg: {
                            bitmap = CameraImageProcessing.DecodeCompressedFrame(
                                frameBuffer,
                                frameLength);
                            break;
                        }
                }
                if (!IsOriginalImageOut) {
                    var thumbnail = GenerateThumbnail(bitmap);
                    bitmap?.Dispose();
                    bitmap = thumbnail;
                }
                return bitmap;
            }
            catch (Exception e) {
                OnCameraExceptionOccurred(new CameraExceptionEventArgs() {
                    Exception = e
                });
                return null;
            }
        }

        private string GetBarType(MvCodeReader.MV_CODEREADER_CODE_TYPE nBarType) {
            return nBarType switch {
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_TDCR_DM => "DM码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_TDCR_QR => "QR码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_EAN8 => "EAN8码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_UPCE => "UPCE码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_UPCA => "UPCA码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_EAN13 => "EAN13码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_ISBN13 => "ISBN13码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_CODABAR => "库德巴码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_ITF25 => "交叉25码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_CODE39 => " Code 39码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_CODE93 => "Code 93码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_CODE128 => "Code 128码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_TDCR_PDF417 => "PDF417码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_MATRIX25 => "MATRIX25码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_MSI => "MSI码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_CODE11 => "Code 11码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_INDUSTRIAL25 => "industria125码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_CHINAPOST => "中国邮政码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_BCR_ITF14 => "交叉14码",
                MvCodeReader.MV_CODEREADER_CODE_TYPE.MV_CODEREADER_TDCR_ECC140 => "ECC140码",
                _ => "/"
            };
        }

        protected virtual void OnCameraExceptionOccurred(CameraExceptionEventArgs e) {
            CameraExceptionOccurred?.Invoke(this, e);
        }

        protected virtual void OnCameraInitialized(CameraInitializedEventArgs e) {
            Status = CameraStatus.Initialized;
            CameraInitialized?.Invoke(this, e);
        }

        protected virtual void OnBarcodeReadTriggered(BarcodeTriggeredEventArgs e) {
            BarcodeReadTriggered?.Invoke(this, e);
        }

        protected virtual void OnCameraStarted(CameraStartedEventArgs e) {
            Status = CameraStatus.Running;
            CameraStarted?.Invoke(this, e);
        }

        protected virtual void OnCameraUnregistered(CameraUnregisteredEventArgs e) {
            Status = CameraStatus.Uninitialized;
            CameraUnregistered?.Invoke(this, e);
        }

        protected virtual void OnNotBarcodeHitEvent(BarcodeReadEventArgs e) {
            NotBarcodeHitEvent?.Invoke(this, e);
        }

        protected virtual void OnCameraDisconnected(CameraConnectionEventArgs e) {
            Status = CameraStatus.Disconnected;
            CameraDisconnected?.Invoke(this, e);
        }

        protected virtual void OnRealtimeImage(RealtimeImageEventArgs e) {
            RealtimeImage?.Invoke(this, e);
        }

        public static Bitmap? GenerateThumbnail(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return CameraImageProcessing.CreateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        public unsafe Bitmap? GenerateThumbnail1(Bitmap? sourceImage, int thumbnailWidth = 800, int thumbnailHeight = 600) {
            return GenerateThumbnail(sourceImage, thumbnailWidth, thumbnailHeight);
        }

        protected virtual void OnOcrContentRecognized(OcrResult e) {
            OcrContentRecognized?.Invoke(this, e);
        }
    }
}
