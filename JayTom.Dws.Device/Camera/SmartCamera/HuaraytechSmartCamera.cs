using System;
using System.Drawing;
using System.Buffers;
using Newtonsoft.Json;
using TurboJpegWrapper;
using System.Collections;
using LogisticsBaseCSharp;
using System.Drawing.Imaging;
using JayTom.Dws.Device.Light;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using static LogisticsBaseCSharp.LogisticsAPIStruct;
using JayTom.Dws.Device.Camera.SmartCamera.Huaraytech;

namespace JayTom.Dws.Device.Camera.SmartCamera {

    public class HuaraytechSmartCamera : ICamera {
        private static SemaphoreSlim _semaphoreSlim = new(1, 1);
        public string DeviceCode { get; private set; } = string.Empty;
        public DeviceStatus Status { get; private set; } = DeviceStatus.Uninitialized;
        public DeviceType Type => DeviceType.Camera;

        private LogisticsWrapper? _dwsManager;
        private const int PoolSize = 5;

        private static readonly ThreadLocal<MemoryStream[]> StreamPool = new(() => new MemoryStream[PoolSize]);
        private static readonly ThreadLocal<BinaryWriter[]> WriterPool = new(() => new BinaryWriter[PoolSize]);

        public Task<KeyValuePair<bool, string>> Reconnect() {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, string>> Connect<T>(T connectParam) {
            await Task.Yield();
            //写连接事件
            if (Status == DeviceStatus.Connected) {
                return new KeyValuePair<bool, string>(false, "相机已连接！");
            }
            if (_dwsManager is null) {
                return new KeyValuePair<bool, string>(false, "未初始化！");
            }
            var status = _dwsManager.Start();
            if (status == (int)EAppRunStatus.EAppStatusInitOk) {
                //注册包裹信息回调的方法.当DWS设备扫描到包裹信息,就会回调给PackageInfoCallBack方法

                _dwsManager.CodeHandle += DwsManagerOnCodeHandle;
                _dwsManager.IpcCombineInfoEventHandler += DwsManagerOnIpcCombineInfoEventHandler;
                //注册包裹结束后所有相机的扫码信息
                /*_dwsManager.AllCameraCodeInfoEventHandler += delegate (object? sender, AllCameraCodeInfoArgs args) {
                };*/

                //注册包裹结束后Ipc相机及条码拼图信息
                //_dwsManager.IpcCombineInfoEventHandler += IpcCombineInfoCBCallBack;

                //注册相机实时图片信息
                //_dwsManager.RealImageEventHandler += DwsManagerOnRealImageEventHandler;
                /*_dwsManager.RealImageEventHandler += delegate (object? sender, RealImageArgs args) {
                    try {
                        File.AppendAllLinesAsync($"{Directory.GetCurrentDirectory()}\\异常日志.txt",
                            new[] { "实时图像" });
                        OnRealtimeImageEvent(args.realImage);
                    }
                    catch (Exception e) {
                        File.AppendAllLinesAsync($"{Directory.GetCurrentDirectory()}\\异常日志.txt",
                            new[] { e.ToString() });
                    }
                };*/
                Status = DeviceStatus.Connected;
                OnConnected(this);
                return new KeyValuePair<bool, string>(true, "相机连接成功");
            }
            return new KeyValuePair<bool, string>(false, status.ToString());
        }

        /// <summary>
        /// 全景相机照片实时回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="NotImplementedException"></exception>
        private async void DwsManagerOnIpcCombineInfoEventHandler(object? sender, IpcCombineInfoArgs args) {
            try {
                await _semaphoreSlim.WaitAsync();
                var rawImage = new RawImage(args.ipcImage.width, args.ipcImage.height, args.ipcImage.type, args.ipcImage.dataSize,
                    args.ipcImage.ImageData, args.ipcImage.img_idx);
                var image = ToBitmap(rawImage);
                OnPanoramaCaptured(new PanoramaCaptureEventArgs() {
                    Image = image,
                    CameraId = args.Reserved
                });
            }
            catch (Exception e) {
                OnExcepted(e);
                await File.AppendAllLinesAsync($"{Directory.GetCurrentDirectory()}\\异常日志.txt",
                    new[] { JsonConvert.SerializeObject(e) });
            }
            finally {
                _semaphoreSlim.Release();
            }
        }

        private async void DwsManagerOnRealImageEventHandler(object? sender, RealImageArgs e) {
            /*try {
                await _semaphoreSlim.WaitAsync();
                var bitmap = ToBitmap(e.realImage);
                OnRealtimeImageEvent(new RealtimeImageEventArgs() {
                    Bitmap = bitmap,
                    Camera = this
                });
            }
            catch (Exception exception) {
                Console.WriteLine(exception);
            }
            finally {
                _semaphoreSlim.Release();
            }*/
        }

        /// <summary>
        /// 相机回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void DwsManagerOnCodeHandle(object? sender, LogisticsCodeEventArgs args) {
            if (args.OutputResult != 0) {
                try {
                    await _semaphoreSlim.WaitAsync();
                    var volumeInfo = new VolumeInfo {
                        Length = args.VolumeInfo.length,
                        Width = args.VolumeInfo.width,
                        Height = args.VolumeInfo.height,
                        Volume = args.VolumeInfo.volume,
                    };
                    var info = new BaseCodeData {
                        OutputResult = args.OutputResult,
                        CameraID = args.CameraID,
                        CodeList = args.CodeList,
                        AreaList = args.AreaList,
                        Weight = args.Weight,
                        VolumeInfo = volumeInfo,
                        OriImage = args.OriginalImage,
                        WayImage = args.WaybillImage,
                        CodeTimeStamp = args.CodeTimeStamp,
                        CodesInfo = args.CodesInfo,
                        BagTimeInfo = new TimeInfo {
                            TimeCallback = args.Bag_TimeInfo.timeCallback,
                            TimeCodeParse = args.Bag_TimeInfo.timeCodeParse,
                            TimeCollect = args.Bag_TimeInfo.timeCollect,
                            TimeDown = args.Bag_TimeInfo.timeDown,
                            TimeFrameGet = args.Bag_TimeInfo.timeFrameGet,
                            TimeFrameSend = args.Bag_TimeInfo.timeFrameSend,
                            TimeUp = args.Bag_TimeInfo.timeUp,
                            TimVol = args.Bag_TimeInfo.timVol,
                            TimWeight = args.Bag_TimeInfo.timWeight
                        },
                        WeightInfo = new WeightData {
                            Flag = args.WeightData.flag,
                            OrigData = args.WeightData.origData,
                            Weight = args.WeightData.weight,
                            WeightTimeStamp = args.WeightData.weightTimeStamp
                        },
                    };

                    var scanTime = DateTime.Now;
                    if (info?.CodeList?.Any() == true &&
                        info?.AreaList?.Any() == true &&
                        info?.AreaList?.Count == info?.CodeList?.Count) {
                        if (args?.CodeList?.Any(a => a.Equals("noread")) == true) {
                            OnNotBarcodeHitEvent(new BarcodeHitEventArgs() {
                                Barcode = "noread",
                                ScanTime = scanTime,
                                CameraName = CameraName
                            });
                            return;
                        }
                        var image = ToBitmap(info?.OriImage);
                        if (image != null) {
                            if (IsShowBarcodeBorder && args?.AreaList?.Count > 0) {
                                //画边框
                                if (info?.AreaList?.Any() == true) {
                                    image = ConvertToNonIndexedPixelFormat(image);
                                    using var graphics = Graphics.FromImage(image);
                                    using var pen = new Pen(BarcodeBorderColor, BarcodeBorderSize);
                                    foreach (var point in info.AreaList) {
                                        graphics.DrawPolygon(pen, point);
                                    }
                                }
                            }
                        }
                        for (var i = 0; i < info!.CodeList!.Count; i++) {
                            var split = info.CameraID.Split(":");
                            OnBarcodeHitEvent(new BarcodeHitEventArgs {
                                Image = image,
                                Barcode = info.CodeList[i],
                                AreaCoords = info.AreaList?[i],
                                CameraId = split?.Length > 1 ? split[1] : info.CameraID,
                                ScanTime = scanTime,
                                Timestamp = info.CodeTimeStamp,
                                Length = (float)volumeInfo.Length,
                                Width = (float)volumeInfo.Width,
                                Height = (float)volumeInfo.Height,
                                Volume = (float)volumeInfo.Volume,
                                CameraName = CameraName,
                                AllBarCodes = string.Join(",", info?.CodeList ?? new List<string>())
                            });
                        }
                    }
                }
                catch (Exception e) {
                    OnExcepted(e);
                    await File.AppendAllLinesAsync($"{Directory.GetCurrentDirectory()}\\异常日志.txt",
                        new[] { JsonConvert.SerializeObject(e) });
                }
                finally {
                    _semaphoreSlim.Release();
                }
            }
        }

        public async void Dispose() {
            await Task.Yield();
            if (Status == DeviceStatus.Connected) {
                //关闭DWS底层的相机断线上报功能
                _dwsManager?.DetachCameraDisconnectCB();

                //卸载扫码结果处理逻辑回调函数
                _dwsManager?.DetachAllCameraCodeinfoCB();

                //卸载全景相机及条码抠图拼接图信息结果处理逻辑回调函数
                _dwsManager?.DetachIpcCombineInfoCB();

                //卸载相机实时图片信息结果处理逻辑回调函数
                _dwsManager?.DetachRealImageCB();
                if (_dwsManager is not null) {
                    _dwsManager.CodeHandle -= DwsManagerOnCodeHandle;
                    _dwsManager.IpcCombineInfoEventHandler -= DwsManagerOnIpcCombineInfoEventHandler;
                    _dwsManager.RealImageEventHandler -= DwsManagerOnRealImageEventHandler;
                }
                _dwsManager?.StopApp();
                Status = DeviceStatus.Uninitialized;
                OnDisconnected(this);
            }
        }

        public async Task<KeyValuePair<bool, string>> Initialization() {
            await Task.Yield();
            if (Status == DeviceStatus.Connected) {
                return new KeyValuePair<bool, string>(false, "相机已连接,不需要再初始化!");
            }
            else if (Status != DeviceStatus.Uninitialized) {
                return new KeyValuePair<bool, string>(false, "已初始化过");
            }
            //写初始化事件
            _dwsManager ??= new LogisticsWrapper();
            try {
                var status = _dwsManager.Initialization(".\\Cfg\\LogisticsBase.cfg");
                if (status != (int)EAppRunStatus.EAppStatusInitOk) {
                    /*retStr = ErrorInfo.GetErrorMessage(status);
                    LogTextOnUI("After dwsManager.Initialization return ret :" + status + " ret info:" + retStr);
                    LogHelper.Log.InfoFormat("[dwsManager.Initialization]After dwsManager.Initialization return ret:{0},ret info:{1}"
                        , status, retStr);*/
                    return new KeyValuePair<bool, string>(false, status.ToString());
                }
                //开启DWS底层的相机断线上报功能
                var disconnectCb = _dwsManager.AttachCameraDisconnectCB();
                if (!disconnectCb) {
                    OnExcepted(new Exception("开启DWS底层的相机断线上报功能失败!"));
                }
                //开启注册所有相机的读码信息的回调函数
                var cameraCodeinfoCb = _dwsManager.AttachAllCameraCodeinfoCB();
                if (!cameraCodeinfoCb) {
                    OnExcepted(new Exception("开启注册所有相机的读码信息的回调失败!"));
                }
                //开启注册全景相机及条码抠图拼接图信息的回调函数
                var combineInfoCb = _dwsManager.AttachIpcCombineInfoCB();
                if (!combineInfoCb) {
                    OnExcepted(new Exception("开启注册全景相机及条码抠图拼接图信息的回调失败!"));
                }

                //开启注册相机实时图片信息的回调函数
                var imageCb = _dwsManager.AttachRealImageCB();

                if (!imageCb) {
                    OnExcepted(new Exception("开启注册相机实时图片信息的回调失败!"));
                }

                //注册相机断线回调的方法.当DWS设备中的相机断线的时候,就会把相关相机的信息回调给CameraDisconnectCallBack方法
                _dwsManager.CameraDisconnectEventHandler += delegate (object? sender, CameraStatusArgs args) {
                    if (!args.IsOnline) {
                        OnExcepted(new Exception($"[CameraId:{CameraId},CameraUserID:{args.CameraUserID},CameraKey:{args.CameraKey}]已断开!"));
                        Status = DeviceStatus.Disconnected;
                    }

                    //OnDisconnected(this);
                };
                //获取相机所有状态
                var camerasStatus = _dwsManager.GetCamerasStatus()?.ToList();

                if (camerasStatus?.Any() == true) {
                    OnExcepted(new Exception(JsonConvert.SerializeObject(camerasStatus)));
                }
                Status = DeviceStatus.Initialized;
                OnInitialized(this);
                return new KeyValuePair<bool, string>(true, "初始化成功!");
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return new KeyValuePair<bool, string>(false, $"初始化失败:{e.Message}");
            }
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public string SerialNumber { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string CameraName { get; private set; } = "大华智能相机";
        public string CameraId { get; set; } = string.Empty;

        public float Framerate { get; private set; } = 0;
        public int BarcodeBorderSize { get; set; } = 15;

        public System.Drawing.Color BarcodeBorderColor { get; set; } = System.Drawing.Color.LawnGreen;
        public bool IsShowBarcodeBorder { get; set; } = true;
        public bool IsUseImageWatermark { get; set; }

        public string Brand => "华睿";
        public CameraStatus CameraStatus { get; private set; } = CameraStatus.Disconnected;
        public CameraType CameraType { get; set; } = CameraType.SmartCamera;
        public ConnectionType ConnectionType { get; private set; } = ConnectionType.Ethernet;

        public event EventHandler<BarcodeHitEventArgs>? BarcodeHitEvent;

        public event EventHandler<BarcodeHitEventArgs>? NotBarcodeHitEvent;

        public event EventHandler<RealtimeImageEventArgs>? RealtimeImageEvent;

        public event EventHandler<PanoramaCaptureEventArgs>? PanoramaCaptured;

        /*// 导入 SetDllDirectory 函数
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDllDirectory(string lpPathName);

        // 导入 LoadLibraryEx 函数
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);*/

        public async Task<List<ICamera>> RetrieveCamera(CancellationToken token = default) {
            await Task.Yield();
            var tagsList = _dwsManager?.GetCamerasStatus().ToList();

            var infos = _dwsManager?.GetWorkCameraInfo()?.ToList();
            var list = infos?.Select(s => new HuaraytechSmartCamera {
                SerialNumber = s.camDevSerialNumber,
                CameraName = s.camDevID,
                Model = s.camDevModelName,
                Version = s.camDevVendor,
                CameraType = s.camDevModelName.Contains("DH-MV") ? CameraType.SmartCamera : CameraType.IndustrialCamera,
                //Framerate = _dwsManager?.GetFrameRate()
            })?.ToList();
            return new List<ICamera>(list ?? new List<HuaraytechSmartCamera>());
        }

        public KeyValuePair<bool, string> SetFilterCondition<T>(T condition) {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetBarcodeType(BarcodeType type) {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Pause() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> Resume() {
            throw new NotImplementedException();
        }

        public KeyValuePair<bool, string> SetConfiguration<T>(T configData) {
            throw new NotImplementedException();
        }

        public enum EAppRunStatus {
            EAppStatusInitOk = 0000,                    //初始化成功
            ERunStatusIvalidHandlerror,                 //句柄不可用，底层初始化失败
            ERunStatusAlreadyRunError,                  //已有软件运行或初始化失败，请关闭软件后重启
            ERunStatusInitialAgainError,                //软件已初始化，请关闭软件重启

            ERunStatusNoCfg = 1000,                     //载入配置文件失败，请检查路径或配置文件命名是否正确
            ERunStatusCfgError,                         //配置文件格式错误，请确认配置文件内容格式是否正确

            ERunStatusNoEncryptedDog = 2200,            //未检测到加密狗，检查或插拔加密狗
            ERunStatusAlgorithmError,                   //加密狗，初始化算法失败
            ERunStatusBarcodeAlgError = 2300,               //初始化一维码算法失败
            ERunStatusBarcodeAlgfinError,               //反初始化一维码算法失败
            ERunStatusDMcodeAlgError = 2400,                //初始化二维码算法失败
            ERunStatusDMcodeAlgfinError,                //反初始化二维码算法失败
            ERunStatusMattingAlgError = 2500,               //初始化抠图算法失败
            ERunStatusMattingAlgfinError,               //反初始化抠图算法失败
            ERunStatusIpcGrayAlgError = 2600,               //初始化全景灰度识别算法失败
            ERunStatusIpcGrayAlgfinError,               //反初始化全景灰度识别算法失败

            ERunStatusCameraNumError = 3000,            //缺少相机，确认实际可连相机个数满足配置个数
            ERunStatusCameraOpendError,                 //部分相机已被连接，确认相机未被连接
            ERunStatusCameraListNumError,               //配置的相机列表个数跟实际配置num不一致
            ERunStatusCameraListUnmatch,                //配置的相机列表，部分相机不存在，确认列表中的相机都存在且可连接
            ERunStatusSoftEncryptionError,              //相机授权失败，确认所有相机都已授权
            ERunStatusIpcCameraError,                   //初始化全景相机失败，确认全景相机存在可连接
            ERunStatusNtpServerError,                   //开启时间同步服务失败

            ERunStatus3DCameraError = 4000,             //3D相机初始化失败，客户端检查3D相机可用

            ERunStatusWeightError = 5000,               //称重模块初始化失败，调试确认当前配置下能正常连接称

            ERunStatusCodeRuleFilterError = 6000,       //条码过滤规则模块初始化失败，检查过滤规则配置格式是否正确

            ERunStatusModuleOutputError = 7000,         //输出规则模块初始化失败，检查输出模块配置是否正确
            ERunStatusModuleDbDataError,                //数据库模块创建失败

            ERunStatusLocalImagePathError = 8000,       //本地图片文件夹不存在，确认本地路径正确
            ERunStatusLocalImageNumError,               //本地图片路径个数不对，确认本地路径文件夹个数跟配置保持一致
            ERunStatusLocalImageInitError,              //本地图片文件夹模式初始化失败，检查路径
        }

        protected virtual async void OnBarcodeHitEvent(BarcodeHitEventArgs e) {
            await Task.Yield();
            BarcodeHitEvent?.Invoke(this, e);
        }

        protected virtual async void OnExcepted(Exception e) {
            await Task.Yield();
            Excepted?.Invoke(this, e);
        }

        protected virtual async void OnRealtimeImageEvent(RealtimeImageEventArgs e) {
            await Task.Yield();
            RealtimeImageEvent?.Invoke(this, e);
        }

        //这个方法后续需要修改
        protected virtual async void OnInitialized(IDevice e) {
            await Task.Yield();
            Initialized?.Invoke(this, e);
        }

        protected virtual async void OnConnected(IDevice e) {
            await Task.Yield();
            Connected?.Invoke(this, e);
        }

        protected virtual async void OnDisconnected(IDevice e) {
            await Task.Yield();
            Disconnected?.Invoke(this, e);
        }

        private Bitmap? ToBitmap(VslbImage? image) {
            if (image?.ImageData is null || image?.ImageData == IntPtr.Zero) {
                return null;
            }
            var vslbImage = image.Value;
            var type = (EImageType)vslbImage.type;
            try {
                switch (type) {
                    case EImageType.eImageTypeNormal:
                    case EImageType.eImageTypeBGR: {
                            var channels = (type == EImageType.eImageTypeBGR ? 3 : 1);
                            var fmt = (channels == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed);
                            var returnBmp = new Bitmap(vslbImage.width, vslbImage.height, fmt);
                            if (channels == 1) {
                                var palette = returnBmp.Palette;
                                for (var ii = 0; ii < 256; ii++)
                                    palette.Entries[ii] = System.Drawing.Color.FromArgb(ii, ii, ii);
                                returnBmp.Palette = palette;
                            }

                            var bmpData = returnBmp.LockBits(new System.Drawing.Rectangle(0, 0, vslbImage.width, vslbImage.height),
                                ImageLockMode.ReadWrite, fmt);
                            unsafe {
                                byte* src = (byte*)vslbImage.ImageData;
                                byte* dst = (byte*)bmpData.Scan0;
                                int stride = bmpData.Stride;

                                for (int y = 0; y < vslbImage.height; y++) {
                                    for (int x = 0; x < vslbImage.width * channels; x++) {
                                        dst[y * stride + x] = src[y * vslbImage.width * channels + x];
                                    }
                                }
                            }

                            returnBmp.UnlockBits(bmpData);
                            //Marshal.FreeHGlobal(vslbImage.ImageData);
                            return returnBmp;
                        }
                    case EImageType.eImageTypeJpeg: {
                            unsafe {
                                using var tjDecompress = new TJDecompressor();
                                var imgType = EImageType.eImageTypeNormal;
                                var retImg = tjDecompress.Decompress(vslbImage.ImageData, (ulong)vslbImage.dataSize, TJFlags.NONE);

                                imgType = retImg.PixelFormat switch {
                                    TJPixelFormats.TJPF_GRAY => EImageType.eImageTypeNormal,
                                    TJPixelFormats.TJPF_BGR => EImageType.eImageTypeBGR,
                                    _ => imgType
                                };

                                // 使用Span和Memory避免不必要的内存分配和拷贝
                                var dataSpan = new Span<byte>(retImg.Data);

                                var tempMemory = new Memory<byte>(dataSpan.ToArray());

                                var rawImg = vslbImage.Clone();
                                rawImg.ImageData = (IntPtr)tempMemory.Pin().Pointer;
                                //rawImg.ImageData = vslbImage.ImageData;
                                rawImg.dataSize = retImg.Data.Length;
                                rawImg.type = (int)imgType;
                                rawImg.img_idx = vslbImage.img_idx;
                                rawImg.width = retImg.Width;
                                rawImg.height = retImg.Height;
                                return ToBitmap(rawImg);
                            }
                        }
                }
            }
            catch (Exception e) {
                OnExcepted(e);
            }

            return null;
        }

        private unsafe Bitmap? ToBitmap(RawImage imageInfo) {
            if (imageInfo.ImageData == IntPtr.Zero || imageInfo.DataSize <= 0 || imageInfo.Height <= 0 || imageInfo.Width <= 0) {
                return null;
            }

            var type = (LogisticsAPIStruct.EImageType)imageInfo.Type;
            try {
                switch (type) {
                    case LogisticsAPIStruct.EImageType.eImageTypeNormal:
                    case LogisticsAPIStruct.EImageType.eImageTypeBGR: {
                            var channels = (type == LogisticsAPIStruct.EImageType.eImageTypeBGR ? 3 : 1);
                            var fmt = (channels == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed);
                            var returnBmp = new Bitmap(imageInfo.Width, imageInfo.Height, fmt);

                            if (channels == 1) {
                                var palette = returnBmp.Palette;
                                for (var ii = 0; ii < 256; ii++)
                                    palette.Entries[ii] = Color.FromArgb(ii, ii, ii);
                                returnBmp.Palette = palette;
                            }

                            var bmpData = returnBmp.LockBits(new Rectangle(0, 0, imageInfo.Width, imageInfo.Height), ImageLockMode.ReadWrite, fmt);

                            // 使用指针操作进行像素复制
                            var src = (byte*)imageInfo.ImageData;
                            var dst = (byte*)bmpData.Scan0;
                            var srcStride = imageInfo.Width * channels;
                            var dstStride = bmpData.Stride;

                            for (var y = 0; y < imageInfo.Height; ++y) {
                                Buffer.MemoryCopy(src, dst, dstStride, srcStride);
                                src += srcStride;
                                dst += dstStride;
                            }

                            returnBmp.UnlockBits(bmpData);
                            return returnBmp;
                        }
                    case LogisticsAPIStruct.EImageType.eImageTypeJpeg: {
                            using (var tjDecompress = new TJDecompressor()) {
                                var imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                var retImg = tjDecompress.Decompress(imageInfo.ImageData, (ulong)imageInfo.DataSize, TJFlags.NONE);

                                if (retImg.PixelFormat == TJPixelFormats.TJPF_GRAY) {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                }
                                else if (retImg.PixelFormat == TJPixelFormats.TJPF_BGR) {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeBGR;
                                }
                                var tempPtr = Marshal.AllocHGlobal(retImg.Data.Length);

                                Marshal.Copy(retImg.Data, 0, tempPtr, retImg.Data.Length);
                                var rawImg = new RawImage(retImg.Width, retImg.Height, (int)imgType, retImg.Data.Length, tempPtr, imageInfo.ImageIndex);
                                return ToBitmap(rawImg);
                            }
                        }
                    default:
                        break;
                }
            }
            catch (Exception ex) {
                // exception
            }

            return null;
        }

        public async Task<Bitmap?> ToBitmap1(VslbImage? image, List<Point[]>? areaList) {
            if (image?.ImageData == IntPtr.Zero || image?.ImageData == null) {
                return null;
            }

            var vslbImage = image.Value;
            var type = (EImageType)vslbImage.type;

            try {
                switch (type) {
                    case EImageType.eImageTypeNormal:
                    case EImageType.eImageTypeBGR: {
                            try {
                                await _semaphoreSlim.WaitAsync();
                                var channels = (type == EImageType.eImageTypeBGR ? 3 : 1);
                                var fmt = (channels == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed);

                                var returnBmp = new Bitmap(vslbImage.width, vslbImage.height, fmt);

                                if (channels == 1) {
                                    var palette = returnBmp.Palette;
                                    for (var ii = 0; ii < 256; ii++)
                                        palette.Entries[ii] = System.Drawing.Color.FromArgb(ii, ii, ii);
                                    returnBmp.Palette = palette;
                                }

                                var bmpData = returnBmp.LockBits(new Rectangle(0, 0, vslbImage.width, vslbImage.height),
                                    ImageLockMode.ReadWrite, fmt);
                                if (channels == 1) {
                                    var imgData = new byte[vslbImage.width * vslbImage.height];
                                    Marshal.Copy(vslbImage.ImageData, imgData, 0, vslbImage.width * vslbImage.height);
                                    var bmpPtr = bmpData.Scan0;

                                    for (var y = 0; y < vslbImage.height; y++) {
                                        for (var x = 0; x < vslbImage.width; x++) {
                                            var pixelValue = imgData[y * vslbImage.width + x];
                                            Marshal.WriteByte(bmpPtr, y * bmpData.Stride + x, pixelValue);
                                        }
                                    }
                                }
                                else {
                                    Marshal.Copy(vslbImage.ImageData, new byte[vslbImage.dataSize], 0, vslbImage.dataSize);
                                }

                                returnBmp.UnlockBits(bmpData);
                                return returnBmp;
                            }
                            finally {
                                _semaphoreSlim.Release();
                            }
                        }
                    case EImageType.eImageTypeJpeg: {
                            using var tjDecompress = new TJDecompressor();
                            var imgType = EImageType.eImageTypeNormal;
                            var retImg = tjDecompress.Decompress(vslbImage.ImageData, (ulong)vslbImage.dataSize,
                                TJFlags.NONE);

                            imgType = retImg.PixelFormat switch {
                                TJPixelFormats.TJPF_GRAY => EImageType.eImageTypeNormal,
                                TJPixelFormats.TJPF_BGR => EImageType.eImageTypeBGR,
                                _ => imgType
                            };

                            // Allocate memory in the unmanaged heap and copy the data to that memory location
                            var dataPtr = Marshal.AllocHGlobal(retImg.Data.Length);
                            Marshal.Copy(retImg.Data, 0, dataPtr, retImg.Data.Length);

                            // Make the recursive call after processing the current image
                            var decompressedBitmap = new VslbImage {
                                ImageData = dataPtr,
                                dataSize = retImg.Data.Length,
                                type = (int)imgType,
                                width = retImg.Width,
                                height = retImg.Height
                            };
                            return await ToBitmap1(decompressedBitmap, areaList);
                        }
                }
            }
            catch (Exception e) {
                OnExcepted(e);
            }

            return null;
        }

        public static Bitmap ConvertToNonIndexedPixelFormat(Bitmap image) {
            if (image.PixelFormat.HasFlag(PixelFormat.Indexed)) {
                // 创建一个新的32位ARGB格式的图像，将索引像素转换为非索引像素
                var newImage = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(newImage)) {
                    graphics.DrawImage(image, 0, 0);
                }
                image.Dispose();
                return newImage;
            }
            return image;
        }

        public async Task<Bitmap?> ToBitmap3(VslbImage? image, List<Point[]>? areaList) {
            if (image?.ImageData == IntPtr.Zero || image?.ImageData == null) {
                return null;
            }

            var vslbImage = image.Value;
            var type = (EImageType)vslbImage.type;

            try {
                switch (type) {
                    case EImageType.eImageTypeNormal:
                    case EImageType.eImageTypeBGR: {
                            try {
                                await _semaphoreSlim.WaitAsync();
                                var channels = (type == EImageType.eImageTypeBGR ? 3 : 1);
                                var fmt = (channels == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed);

                                var returnBmp = new Bitmap(vslbImage.width, vslbImage.height, fmt);

                                if (channels == 1) {
                                    var palette = returnBmp.Palette;
                                    for (var ii = 0; ii < 256; ii++)
                                        palette.Entries[ii] = System.Drawing.Color.FromArgb(ii, ii, ii);
                                    returnBmp.Palette = palette;
                                }

                                using (var memoryOwner = MemoryPool<byte>.Shared.Rent(vslbImage.width * vslbImage.height)) {
                                    var imgData = memoryOwner.Memory.Slice(0, vslbImage.width * vslbImage.height).ToArray();
                                    Marshal.Copy(vslbImage.ImageData, imgData, 0, vslbImage.width * vslbImage.height);
                                    var bmpData = returnBmp.LockBits(new Rectangle(0, 0, vslbImage.width, vslbImage.height),
                                        ImageLockMode.ReadWrite, fmt);
                                    var bmpPtr = bmpData.Scan0;

                                    for (var y = 0; y < vslbImage.height; y++) {
                                        for (var x = 0; x < vslbImage.width; x++) {
                                            var pixelValue = imgData[y * vslbImage.width + x];
                                            Marshal.WriteByte(bmpPtr, y * bmpData.Stride + x, pixelValue);
                                        }
                                    }

                                    returnBmp.UnlockBits(bmpData);
                                }

                                return returnBmp;
                            }
                            finally {
                                _semaphoreSlim.Release();
                            }
                        }
                    case EImageType.eImageTypeJpeg: {
                            using var tjDecompress = new TJDecompressor();
                            var imgType = EImageType.eImageTypeNormal;
                            var retImg = tjDecompress.Decompress(vslbImage.ImageData, (ulong)vslbImage.dataSize,
                                TJFlags.NONE);

                            imgType = retImg.PixelFormat switch {
                                TJPixelFormats.TJPF_GRAY => EImageType.eImageTypeNormal,
                                TJPixelFormats.TJPF_BGR => EImageType.eImageTypeBGR,
                                _ => imgType
                            };

                            // Allocate memory in the unmanaged heap and copy the data to that memory location
                            var dataPtr = Marshal.AllocHGlobal(retImg.Data.Length);
                            Marshal.Copy(retImg.Data, 0, dataPtr, retImg.Data.Length);

                            // Make the recursive call after processing the current image
                            var decompressedBitmap = new VslbImage {
                                ImageData = dataPtr,
                                dataSize = retImg.Data.Length,
                                type = (int)imgType,
                                width = retImg.Width,
                                height = retImg.Height
                            };
                            return await ToBitmap3(decompressedBitmap, areaList);
                        }
                }
            }
            catch (Exception e) {
                OnExcepted(e);
            }

            return null;
        }

        public Bitmap ToBitmap5(VslbImage imageInfo) {
            if (imageInfo.ImageData == IntPtr.Zero || imageInfo.dataSize <= 0 || imageInfo.height <= 0 || imageInfo.width <= 0) {
                return null;
            }

            var type = (LogisticsAPIStruct.EImageType)imageInfo.type;
            try {
                switch (type) {
                    case LogisticsAPIStruct.EImageType.eImageTypeNormal:
                    case LogisticsAPIStruct.EImageType.eImageTypeBGR: {
                            int channels = (type == LogisticsAPIStruct.EImageType.eImageTypeBGR ? 3 : 1);
                            PixelFormat fmt = (channels == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed);

                            using (MemoryStream stream = new MemoryStream()) {
                                // 写入图像数据到内存流中
                                using (BinaryWriter writer = new BinaryWriter(stream)) {
                                    byte[] data = new byte[imageInfo.dataSize];
                                    Marshal.Copy(imageInfo.ImageData, data, 0, imageInfo.dataSize);
                                    writer.Write(data, 0, imageInfo.dataSize);
                                    writer.Flush();
                                    // 不需要显式调用 writer.Close() 或 writer.Dispose()
                                }
                                stream.Position = 0;
                                // 创建位图对象
                                Bitmap returnBmp = new Bitmap(imageInfo.width, imageInfo.height, fmt);
                                if (channels == 1) {
                                    var palette = returnBmp.Palette;
                                    for (var ii = 0; ii < 256; ii++)
                                        palette.Entries[ii] = Color.FromArgb(ii, ii, ii);
                                    returnBmp.Palette = palette;
                                }

                                // 从内存流中读取图像数据并解码为位图
                                returnBmp = new Bitmap(stream);
                                // 释放非托管内存
                                Marshal.FreeHGlobal(imageInfo.ImageData);
                                return returnBmp;
                            }
                        }
                    case LogisticsAPIStruct.EImageType.eImageTypeJpeg: {
                            using (var tjDecompress = new TJDecompressor()) {
                                var imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                var retImg = tjDecompress.Decompress(imageInfo.ImageData, (ulong)imageInfo.dataSize, TJFlags.NONE);

                                if (retImg.PixelFormat == TJPixelFormats.TJPF_GRAY) {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                }
                                else if (retImg.PixelFormat == TJPixelFormats.TJPF_BGR) {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeBGR;
                                }

                                // 创建内存流并将解压后的图像数据写入到内存流中
                                using (MemoryStream stream = new MemoryStream(retImg.Data)) {
                                    var buffer = new byte[stream.Length];
                                    stream.Read(buffer, 0, buffer.Length);

                                    IntPtr imageDataPtr = Marshal.AllocHGlobal(buffer.Length);
                                    Marshal.Copy(buffer, 0, imageDataPtr, buffer.Length);
                                    var rawImg = new VslbImage {
                                        ImageData = imageDataPtr,
                                        dataSize = (int)stream.Length,
                                        type = (int)imgType,
                                        width = retImg.Width,
                                        height = retImg.Height
                                    };
                                    return ToBitmap(rawImg);
                                }
                            }
                        }
                    default:
                        break;
                }
            }
            catch (Exception ex) {
                // 异常处理
            }

            return null;
        }

        public Bitmap ToBitmap2(VslbImage imageInfo) {
            if (imageInfo.ImageData == IntPtr.Zero || imageInfo.dataSize <= 0 || imageInfo.height <= 0 || imageInfo.width <= 0) {
                return null;
            }

            var type = (LogisticsAPIStruct.EImageType)imageInfo.type;

            try {
                switch (type) {
                    case LogisticsAPIStruct.EImageType.eImageTypeNormal:
                    case LogisticsAPIStruct.EImageType.eImageTypeBGR: {
                            int channels = (type == LogisticsAPIStruct.EImageType.eImageTypeBGR ? 3 : 1);
                            PixelFormat fmt = (channels == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed);

                            var streamPool = StreamPool.Value;
                            var writerPool = WriterPool.Value;

                            MemoryStream stream = null;
                            BinaryWriter writer = null;

                            for (int i = 0; i < PoolSize; i++) {
                                if (streamPool[i] == null) {
                                    stream = new MemoryStream();
                                    writer = new BinaryWriter(stream);
                                    streamPool[i] = stream;
                                    writerPool[i] = writer;
                                    break;
                                }
                                else if (!streamPool[i].TryGetBuffer(out _)) {
                                    stream = streamPool[i];
                                    writer = writerPool[i];
                                    stream.SetLength(0);
                                    stream.Position = 0;
                                    break;
                                }
                            }

                            if (stream == null || writer == null) {
                                // 如果对象池已满，使用普通的内存操作
                                stream = new MemoryStream();
                                writer = new BinaryWriter(stream);
                            }

                            byte[] data = new byte[imageInfo.dataSize];
                            Marshal.Copy(imageInfo.ImageData, data, 0, imageInfo.dataSize);
                            writer.Write(data, 0, imageInfo.dataSize);
                            writer.Flush();

                            stream.Position = 0;
                            Bitmap returnBmp = new Bitmap(imageInfo.width, imageInfo.height, fmt);

                            if (channels == 1) {
                                var palette = returnBmp.Palette;
                                for (var ii = 0; ii < 256; ii++)
                                    palette.Entries[ii] = Color.FromArgb(ii, ii, ii);
                                returnBmp.Palette = palette;
                            }

                            returnBmp = new Bitmap(stream);

                            Marshal.FreeHGlobal(imageInfo.ImageData);
                            return returnBmp;
                        }
                    case LogisticsAPIStruct.EImageType.eImageTypeJpeg: {
                            using (var tjDecompress = new TJDecompressor()) {
                                var imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                var retImg = tjDecompress.Decompress(imageInfo.ImageData, (ulong)imageInfo.dataSize, TJFlags.NONE);

                                if (retImg.PixelFormat == TJPixelFormats.TJPF_GRAY) {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                }
                                else if (retImg.PixelFormat == TJPixelFormats.TJPF_BGR) {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeBGR;
                                }

                                var streamPool = StreamPool.Value;
                                var writerPool = WriterPool.Value;

                                MemoryStream stream = null;
                                BinaryWriter writer = null;

                                for (int i = 0; i < PoolSize; i++) {
                                    if (streamPool[i] == null) {
                                        stream = new MemoryStream(retImg.Data);
                                        writer = new BinaryWriter(stream);
                                        streamPool[i] = stream;
                                        writerPool[i] = writer;
                                        break;
                                    }
                                    else if (!streamPool[i].TryGetBuffer(out _)) {
                                        stream = streamPool[i];
                                        writer = writerPool[i];
                                        stream.SetLength(retImg.Data.Length);
                                        stream.Position = 0;
                                        stream.Write(retImg.Data, 0, retImg.Data.Length);
                                        stream.Position = 0;
                                        break;
                                    }
                                }

                                if (stream == null || writer == null) {
                                    // 如果对象池已满，使用普通的内存操作
                                    stream = new MemoryStream(retImg.Data);
                                    writer = new BinaryWriter(stream);
                                }

                                IntPtr imageDataPtr = Marshal.AllocHGlobal(retImg.Data.Length);
                                Marshal.Copy(retImg.Data, 0, imageDataPtr, retImg.Data.Length);

                                var rawImg = new VslbImage {
                                    ImageData = imageDataPtr,
                                    dataSize = retImg.Data.Length,
                                    type = (int)imgType,
                                    width = retImg.Width,
                                    height = retImg.Height
                                };

                                return ToBitmap(rawImg);
                            }
                        }
                    default:
                        break;
                }
            }
            catch (Exception ex) {
                // 异常处理
            }

            return null;
        }

        protected virtual async void OnNotBarcodeHitEvent(BarcodeHitEventArgs e) {
            await Task.Yield();
            NotBarcodeHitEvent?.Invoke(this, e);
        }

        protected virtual async void OnPanoramaCaptured(PanoramaCaptureEventArgs e) {
            await Task.Yield();
            PanoramaCaptured?.Invoke(this, e);
        }
    }
}