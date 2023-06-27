using System;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Reflection;
using System.Diagnostics;
using LogisticsBaseCSharp;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Device.Camera.SmartCamera {

    public class HuaraytechSmartCamera : ICamera {
        public string DeviceCode { get; private set; } = string.Empty;
        public DeviceStatus Status { get; private set; } = DeviceStatus.Uninitialized;

        private LogisticsWrapper? _dwsManager;

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
                _dwsManager.CodeHandle += delegate (object? sender, LogisticsCodeEventArgs args) {
                    try {
                        var volumeInfo = new VolumeInfo {
                            Length = args?.VolumeInfo.length ?? 0,
                            Width = args?.VolumeInfo.width ?? 0,
                            Height = args?.VolumeInfo.height ?? 0,
                            Volume = args?.VolumeInfo.volume ?? 0,
                        };
                        var scanTime = DateTime.Now;
                        if (args?.CodeList?.Any() == true &&
                            args?.AreaList?.Any() == true &&
                            args?.AreaList?.Count == args?.CodeList?.Count) {
                            for (var i = 0; i < args!.CodeList!.Count; i++) {
                                OnBarcodeHitEvent(new BarcodeHitEventArgs() {
                                    //Image = args.OriginalImage,
                                    Barcode = args.CodeList[i],
                                    AreaCoords = args.AreaList?[i],
                                    CameraId = args.CameraID,
                                    ScanTime = scanTime,
                                    Timestamp = args.CodeTimeStamp,
                                    Length = (float)volumeInfo.Length,
                                    Width = (float)volumeInfo.Width,
                                    Height = (float)volumeInfo.Height,
                                    Volume = (float)volumeInfo.Volume,
                                });
                            }
                        }
                    }
                    catch (Exception e) {
                        OnExcepted(e);
                        File.AppendAllLinesAsync($"{Directory.GetCurrentDirectory()}\\异常日志.txt",
                            new[] { e.ToString() });
                    }
                };
                //注册包裹结束后所有相机的扫码信息
                /*_dwsManager.AllCameraCodeInfoEventHandler += delegate (object? sender, AllCameraCodeInfoArgs args) {
                };*/

                //注册包裹结束后Ipc相机及条码拼图信息
                //_dwsManager.IpcCombineInfoEventHandler += IpcCombineInfoCBCallBack;

                //注册相机实时图片信息
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
                return new KeyValuePair<bool, string>(false, "相机连接成功");
            }
            return new KeyValuePair<bool, string>(false, status.ToString());
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
                Status = DeviceStatus.Uninitialized;
                OnDisconnected(this);
            }
        }

        public async Task<KeyValuePair<bool, string>> Initialization() {
            await Task.Yield();
            if (_dwsManager is not null) {
                return new KeyValuePair<bool, string>(false, "已初始化过!");
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
                    OnExcepted(new Exception($"开启DWS底层的相机断线上报功能失败!"));
                }
                //开启注册所有相机的读码信息的回调函数
                var cameraCodeinfoCb = _dwsManager.AttachAllCameraCodeinfoCB();
                if (!cameraCodeinfoCb) {
                    OnExcepted(new Exception($"开启注册所有相机的读码信息的回调失败!"));
                }
                //开启注册全景相机及条码抠图拼接图信息的回调函数
                var combineInfoCb = _dwsManager.AttachIpcCombineInfoCB();
                if (!combineInfoCb) {
                    OnExcepted(new Exception($"开启注册全景相机及条码抠图拼接图信息的回调失败!"));
                }

                //开启注册相机实时图片信息的回调函数
                var imageCb = _dwsManager.AttachRealImageCB();

                if (!imageCb) {
                    OnExcepted(new Exception($"开启注册相机实时图片信息的回调失败!"));
                }

                //注册相机断线回调的方法.当DWS设备中的相机断线的时候,就会把相关相机的信息回调给CameraDisconnectCallBack方法
                _dwsManager.CameraDisconnectEventHandler += delegate (object? sender, CameraStatusArgs args) {
                    OnExcepted(new Exception($"{this.CameraId}已断开!"));
                    Status = DeviceStatus.Disconnected;
                    //OnDisconnected(this);
                };
                //获取相机所有状态
                var camerasStatus = _dwsManager.GetCamerasStatus()?.ToList();
                if (camerasStatus?.Any() == true) {
                    OnExcepted(new Exception(JsonConvert.SerializeObject(camerasStatus)));
                }

                OnInitialized(this);
                return new KeyValuePair<bool, string>(true, "初始化成功!");
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            return new KeyValuePair<bool, string>(true, "初始化成功!");
        }

        public event EventHandler<IDevice>? Initialized;

        public event EventHandler<IDevice>? Connected;

        public event EventHandler<IDevice>? Disconnected;

        public event EventHandler<IDevice>? Reconnected;

        public event EventHandler<Exception>? Excepted;

        public string CameraName { get; private set; } = string.Empty;
        public string CameraId { get; private set; } = string.Empty;

        public float Framerate { get; private set; } = 0;
        public int BarcodeBorderSize { get; set; }

        public Color BarcodeBorderColor { get; set; }
        public bool IsShowBarcodeBorder { get; set; }
        public bool IsUseImageWatermark { get; set; }

        public event EventHandler<BarcodeHitEventArgs>? BarcodeHitEvent;

        public event EventHandler<Bitmap>? RealtimeImageEvent;

        // 导入 SetDllDirectory 函数
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDllDirectory(string lpPathName);

        // 导入 LoadLibraryEx 函数
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        public HuaraytechSmartCamera() {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                // 获取要加载的程序集名称
                var assemblyName = new AssemblyName(args.Name).Name;
                Debug.WriteLine(assemblyName);
                // 检查是否正在加载 LogisticsBaseCSharp.dll
                if (assemblyName is "LogisticsBaseCSharp" or "LogisticsBase64.dll") {
                    // 从指定文件夹中加载所需的 DLL 文件
                    string dllPath = Path.Combine($"{Directory.GetCurrentDirectory()}\\HuaraytechLib", assemblyName + ".dll");
                    if (File.Exists(dllPath)) {
                        return Assembly.LoadFrom(dllPath);
                    }
                }

                // 继续使用默认的加载逻辑
                return null;
            };
            var dllDirectory = SetDllDirectory($"{Directory.GetCurrentDirectory()}\\HuaraytechLib");
            if (dllDirectory) {
                var files = Directory.GetFiles($"{Directory.GetCurrentDirectory()}\\HuaraytechLib");
                foreach (var file in files) {
                    IntPtr dllHandle = LoadLibraryEx(file, IntPtr.Zero, 0);
                    if (dllHandle == IntPtr.Zero) {
                        int errorCode = Marshal.GetLastWin32Error();
                        Console.WriteLine($"无法加载 DLL 文件。错误码: {errorCode}");
                        Debug.WriteLine(file);
                        continue;
                    }
                }

                var first = files.First(f => f.Contains("LogisticsBase64.dll"));
                if (!string.IsNullOrWhiteSpace(first)) {
                    IntPtr dllHandle = LoadLibraryEx(first, IntPtr.Zero, 0);
                    Console.WriteLine(dllHandle);
                }
            }

            /*string dependenciesPath = Path.Combine(AppContext.BaseDirectory, "HuaraytechLib");
            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => {
                string assemblyName = new AssemblyName(eventArgs.Name).Name + ".dll";
                string assemblyPath = Path.Combine(dependenciesPath, assemblyName);

                if (File.Exists(assemblyPath)) {
                    return Assembly.LoadFrom(assemblyPath);
                }

                return null;
            };*/
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

        public struct VolumeInfo {
            public double Length { get; set; }

            public double Width { get; set; }

            public double Height { get; set; }

            public double Volume { get; set; }
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

        protected virtual async void OnRealtimeImageEvent(Bitmap e) {
            await Task.Yield();
            RealtimeImageEvent?.Invoke(this, e);
        }

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
    }
}