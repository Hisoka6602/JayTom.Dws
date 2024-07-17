using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.BarCodeReader;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Camera.Attributes.CameraAttributes;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

namespace JayTom.Dws.Camera.Cameras.IndustrialCamera.DaHua {

    public class DaHuaBarCodeReader : IDisposable {
        private DynamsoftBarCodeReader _dynamsoftBarCodeReader = new();
        private static ConcurrentDictionary<string, DEVICE_NET_INFO_EX> _devInfo = new();
        private static ConcurrentDictionary<string, IntPtr> _loginDev = new();

        /// <summary>
        /// 设备枚举回调
        /// </summary>
        private static fSearchDevicesCBEx? _mSearchDevicesCbEx;

        /// <summary>
        /// 设备重连回调
        /// </summary>
        private static fHaveReConnectCallBack? _mReConnectCallBack;

        /// <summary>
        /// 设备断连回调
        /// </summary>
        private static fDisConnectCallBack? _mDisConnectCallBack;

        /// <summary>
        /// 设备数据回调
        /// </summary>
        private static fRealDataCallBackEx2? _mRealDataCallBackEx2;

        /// <summary>
        /// 设备远程抓图回调
        /// </summary>
        private static fSnapRevCallBack? _mSnapRevCallBack;

        private static SemaphoreSlim _enumerateSlim = new(1);
        private static SemaphoreSlim _realtimeFrameSlim = new(1);
        private static SemaphoreSlim _snapRevPhotoSlim = new(1);
        private static SemaphoreSlim _takePhotoSlim = new(1);
        private static object _initLock = new();
        private static DaHuaBarCodeReader? _instance;
        private static byte[] _realtimeFrameBytes = Array.Empty<byte>();
        private static byte[] _imageBytes = Array.Empty<byte>();

        /// <summary>
        /// 实时预览句柄
        /// </summary>
        private static ConcurrentDictionary<string, IntPtr> _realPlayInfo = new();

        /// <summary>
        /// 实时预览事件
        /// </summary>
        private static ConcurrentDictionary<string, Action<Bitmap?>> _realtimeFrameEvent = new();

        /// <summary>
        /// 远程抓图
        /// </summary>
        private static ConcurrentDictionary<string, Action<Bitmap?>> _imageEvent = new();

        public void Dispose() {
            _dynamsoftBarCodeReader.Dispose();
        }

        private DaHuaBarCodeReader() {
        }

        public static DaHuaBarCodeReader CreateInstance() {
            lock (_initLock) {
                if (_instance is null) {
                    _instance ??= new DaHuaBarCodeReader();

                    //设备枚举回调
                    _mSearchDevicesCbEx += async delegate (IntPtr handle, IntPtr intPtr, IntPtr user) {
                        var info = (NET_DEVICE_NET_INFO_EX2)(Marshal.PtrToStructure(intPtr, typeof(NET_DEVICE_NET_INFO_EX2)) ?? IntPtr.Zero);
                        if (info.stuDevInfo.iIPVersion == 4) {
                            await _enumerateSlim.WaitAsync();
                            _devInfo.AddOrUpdate(info.stuDevInfo.szSerialNo, key => info.stuDevInfo,
                                (key, oldValue) => {
                                    oldValue.verifyData = info.stuDevInfo.verifyData;
                                    oldValue.szVendor = info.stuDevInfo.szVendor;
                                    oldValue.szDevName = info.stuDevInfo.szDevName;
                                    oldValue.wVideoInputCh = info.stuDevInfo.wVideoInputCh;
                                    return oldValue;
                                });
                            _enumerateSlim.Release();
                        }
                    };
                    //设备断连回调
                    _mDisConnectCallBack += delegate (IntPtr id, IntPtr dvrip, int port, IntPtr user) {
                    };
                    //设备重连回调
                    _mReConnectCallBack += delegate (IntPtr id, IntPtr dvrip, int port, IntPtr user) {
                    };
                    //设备连接
                    //设备登录

                    //设备数据回调
                    _mRealDataCallBackEx2 += async delegate (IntPtr handle, uint type, IntPtr buffer, uint size, IntPtr nint,
                        IntPtr user) {
                            try {
                                await _realtimeFrameSlim.WaitAsync();
                                //取出登录id
                                var (key, value) = _realPlayInfo.FirstOrDefault(f => f.Value == handle);
                                if (key != null) {
                                    var tryGetValue = _realtimeFrameEvent.TryGetValue(key, out var callback);
                                    if (tryGetValue) {
                                        Image? imageBitmap = null;
                                        _realtimeFrameBytes = new byte[size];
                                        Marshal.Copy(buffer, _realtimeFrameBytes, 0, (int)size);
                                        using var stream = new MemoryStream(_realtimeFrameBytes);
                                        imageBitmap = Image.FromStream(stream);

                                        var image = imageBitmap?.GetThumbnailImage(imageBitmap.Width, imageBitmap.Height,
                                            () => false, IntPtr.Zero);

                                        if (image != null) callback?.Invoke((Bitmap)image);

                                        imageBitmap?.Dispose();
                                    }
                                }
                            }
                            catch (Exception e) {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }
                            finally {
                                _realtimeFrameSlim.Release();
                            }
                        };
                    //设备远程抓图回调
                    _mSnapRevCallBack += async delegate (IntPtr id, IntPtr buf, uint len, uint type, uint serial, IntPtr user) {
                        if (len > 0) {
                            try {
                                await _snapRevPhotoSlim.WaitAsync();
                                await Task.Delay(50);
                                var (key, value) = _loginDev.FirstOrDefault(f => f.Value == id);
                                if (key != null && _imageEvent.TryGetValue(key, out var callback)) {
                                    if (type == 10) //.jpg
                                    {
                                        _imageBytes = new byte[len];
                                        Marshal.Copy(buf, _imageBytes, 0, (int)len);

                                        using var stream = new MemoryStream(_imageBytes);
                                        var valid = IsImageDataValid(stream);
                                        if (valid) {
                                            using var imageBitmap = Image.FromStream(stream);
                                            using var thumbnail = imageBitmap.GetThumbnailImage(imageBitmap.Width, imageBitmap.Height, () => false, IntPtr.Zero);
                                            callback?.Invoke(new Bitmap(thumbnail));
                                        }
                                    }
                                }
                            }
                            catch (Exception e) {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }
                            finally {
                                _snapRevPhotoSlim.Release();
                            }
                        }
                    };
                    //实时抓图
                }
            }
            return _instance;
        }

        /// <summary>
        /// 枚举相机
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<List<DEVICE_NET_INFO_EX>?> EnumDevices() {
            await Task.Yield();
            _devInfo.Clear();
            List<DEVICE_NET_INFO_EX>? devices = new();
            try {
                var ipInfos = GetAllIpInfo();

                foreach (var ipInfo in ipInfos) {
                    var stuIn = new NET_IN_STARTSERACH_DEVICE {
                        dwSize = (uint)Marshal.SizeOf(typeof(NET_IN_STARTSERACH_DEVICE)),
                        emSendType = EM_SEND_SEARCH_TYPE.MULTICAST_AND_BROADCAST,
                        cbSearchDevices = _mSearchDevicesCbEx,
                        szLocalIp = ipInfo
                    };

                    var stuOut = new NET_OUT_STARTSERACH_DEVICE {
                        dwSize = (uint)Marshal.SizeOf(typeof(NET_OUT_STARTSERACH_DEVICE))
                    };
                    await Task.Delay(50);
                    NETClient.StartSearchDevicesEx(ref stuIn, ref stuOut);
                }
                await Task.Delay(1000);
                await _enumerateSlim.WaitAsync();

                devices.AddRange(_devInfo.Select(s => s.Value));

                _enumerateSlim.Release();
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            return devices;
        }

        /// <summary>
        /// 获取相机信息
        /// </summary>
        /// <returns></returns>
        public DEVICE_NET_INFO_EX? GetDeviceNetInfo(string serialNo) {
            _devInfo.TryGetValue(serialNo, out var info);
            return info;
        }

        /// <summary>
        /// 设备登录
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> LogIn(string serialNo, string userName, string passWord) {
            await Task.Yield();

            try {
                await _enumerateSlim.WaitAsync();
                var tryGetValue = _loginDev.TryGetValue(serialNo, out var loginId);
                if (!tryGetValue || (loginId == IntPtr.Zero)) {
                    var getValue = _devInfo.TryGetValue(serialNo, out var info);
                    if (getValue) {
                        var mDeviceInfo = new NET_DEVICEINFO_Ex();
                        var mLoginId = NETClient.LoginWithHighLevelSecurity(info.szIP
                            , (ushort)info.nPort, userName, passWord,
                            EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref mDeviceInfo);
                        if (IntPtr.Zero == mLoginId) {
                            var lastError = NETClient.GetLastError();
                            return new KeyValuePair<bool, string>(false, lastError);
                        }
                        //添加到字典
                        _loginDev.TryAdd(serialNo, mLoginId);

                        return new KeyValuePair<bool, string>(true, mLoginId.ToString());
                    }
                    else {
                        return new KeyValuePair<bool, string>(false, "不存在该设备或未枚举");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(true, loginId.ToString());
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _enumerateSlim.Release();
            }
        }

        /// <summary>
        /// 设备注销
        /// </summary>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> LogOut(string serialNo) {
            await Task.Yield();
            try {
                await _enumerateSlim.WaitAsync();
                _loginDev.TryGetValue(serialNo, out var mLoginId);
                var result = NETClient.Logout(mLoginId);
                if (!result) {
                    var lastError = NETClient.GetLastError();

                    return new KeyValuePair<bool, string>(false, lastError);
                }

                _loginDev.TryRemove(serialNo, out mLoginId);
                return new KeyValuePair<bool, string>(true, mLoginId.ToString());
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _enumerateSlim.Release();
            }
        }

        /// <summary>
        /// 注册远程图片回调事件
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="callback"></param>
        public void RegisterImageCallback(string serialNo, [NotNull] Action<Bitmap> callback) {
            _imageEvent.AddOrUpdate(serialNo, callback!, (k, v) => callback);
        }

        //注册实时抓图回调

        /// <summary>
        /// 注册实时录像回调
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="callback"></param>
        public void RegisterRealtimeFrameCallback(string serialNo, [NotNull] Action<Bitmap> callback) {
            _realtimeFrameEvent.AddOrUpdate(serialNo, callback!, (k, v) => callback);
        }

        /// <summary>
        /// 获取远程实时图片
        /// </summary>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> GetRealtimeImage(string serialNo) {
            await Task.Yield();

            try {
                await _takePhotoSlim.WaitAsync();
                await Task.Delay(400);
                var tryGetValue = _loginDev.TryGetValue(serialNo, out var mLoginId);
                if (tryGetValue) {
                    var indexOf = _loginDev.Keys.OrderBy(o => o).ToList().IndexOf(serialNo);
                    var asyncSnap = new NET_SNAP_PARAMS {
                        Channel = (uint)indexOf,
                        Quality = 6,
                        ImageSize = 2,
                        mode = 0,
                        CmdSerial = (uint)new Random().Next(0, 65536),
                    };
                    var ret = NETClient.SnapPictureEx(mLoginId, asyncSnap, IntPtr.Zero);
                    if (!ret) {
                        var lastError = NETClient.GetLastError();
                        return new KeyValuePair<bool, string>(false, lastError);
                    }

                    return new KeyValuePair<bool, string>(true, string.Empty);
                }
                else {
                    return new KeyValuePair<bool, string>(false, "设备未登录");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _takePhotoSlim.Release();
            }
        }

        /// <summary>
        /// 开始实时画面预览
        /// </summary>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> StartRealtimePlay(string serialNo) {
            try {
                return new KeyValuePair<bool, string>(false, "暂时不支持实时画面");
                /*await _switchRealtimeFrameSlim.WaitAsync();
                var tryGetValue = _loginDev.TryGetValue(serialNo, out var mLoginId);
                if (tryGetValue) {
                    var indexOf = _loginDev.Keys.OrderBy(o => o).ToList().IndexOf(serialNo);
                    //判断是否已经开启
                    var isRealPlay = _realPlayInfo.TryGetValue(serialNo, out var mRealPlayId);
                    if (isRealPlay && mRealPlayId != IntPtr.Zero) {
                        return new KeyValuePair<bool, string>(true, "已经开启了预览");
                    }
                    var ret = NETClient.RealPlay(mLoginId, indexOf, IntPtr.Zero);
                    if (ret == IntPtr.Zero) {
                        var lastError = NETClient.GetLastError();
                        return new KeyValuePair<bool, string>(false, $"开始预览失败:{lastError}");
                    }

                    var back = NETClient.SetRealDataCallBack(ret, _mRealDataCallBackEx2, IntPtr.Zero,
                        EM_REALDATA_FLAG.RAW_DATA);
                    if (!back) {
                        var lastError = NETClient.GetLastError();
                        return new KeyValuePair<bool, string>(false, lastError);
                    }
                    _realPlayInfo.TryAdd(serialNo, ret);
                    return new KeyValuePair<bool, string>(true, string.Empty);
                }
                else {
                    return new KeyValuePair<bool, string>(false, "设备未登录");
                }*/
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                //_switchRealtimeFrameSlim.Release();
            }
        }

        /// <summary>
        /// 停止实时画面预览
        /// </summary>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> StopRealtimePlay(string serialNo) {
            /*try {
                await _switchRealtimeFrameSlim.WaitAsync();
                var tryGetValue = _loginDev.TryGetValue(serialNo, out var mLoginId);
                if (tryGetValue) {
                    var indexOf = _loginDev.Keys.OrderBy(o => o).ToList().IndexOf(serialNo);
                    //判断是否已经开启
                    var isRealPlay = _realPlayInfo.TryGetValue(serialNo, out var mRealPlayId);
                    if (isRealPlay && mRealPlayId != IntPtr.Zero) {
                        var ret = NETClient.StopRealPlay(mRealPlayId);
                        if (!ret) {
                            var lastError = NETClient.GetLastError();
                            return new KeyValuePair<bool, string>(false, lastError);
                        }
                        _realPlayInfo.Remove(serialNo, out _);
                        return new KeyValuePair<bool, string>(true, string.Empty);
                    }
                    else {
                        return new KeyValuePair<bool, string>(true, $"未开启预览:{mRealPlayId}");
                    }
                }
                else {
                    return new KeyValuePair<bool, string>(false, "设备未登录");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _switchRealtimeFrameSlim.Release();
            }*/

            return new KeyValuePair<bool, string>(false, string.Empty);
        }

        //查询录像文件
        private KeyValuePair<bool, string> QueryFile(IntPtr mLoginId, int channelId, DateTime startTime, DateTime endTime, ref NET_RECORDFILE_INFO[] infos, ref int fileCount) {
            //set stream type 设置码流类型 (一律主码流)
            const EM_STREAM_TYPE streamType = EM_STREAM_TYPE.MAIN;
            var pStream = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));
            Marshal.StructureToPtr((int)streamType, pStream, true);
            NETClient.SetDeviceMode(mLoginId, EM_USEDEV_MODE.RECORD_STREAM_TYPE, pStream);
            //query record file 查询录像文件
            var ret = NETClient.QueryRecordFile(mLoginId, channelId, EM_QUERY_RECORD_TYPE.ALL, startTime, endTime, null, ref infos, ref fileCount, 5000, false);
            Console.WriteLine($"{channelId}");
            Console.WriteLine($"{startTime}--{endTime}");
            Console.WriteLine($"fileCount:{fileCount}");
            return (false == ret || fileCount <= 0) ? new KeyValuePair<bool, string>(false, "录像文件不存在") : new KeyValuePair<bool, string>(true, string.Empty);
        }

        //实时抓图(一张)
        //开启实时抓图流

        private static List<string> GetAllIpInfo() {
            var ipAddressItems = new List<string>();
            //获取所有网卡信息
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in nics) {
                //Wireless80211         无线网卡
                //Ppp                   宽带连接
                //Ethernet              以太网卡
                if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue; //判断是否为以太网卡
                //获取以太网卡网络接口信息
                var ip = adapter.GetIPProperties();
                //获取单播地址集
                var ipCollection = ip.UnicastAddresses;
                ipAddressItems.AddRange(from ipadd in ipCollection where ipadd.Address.AddressFamily == AddressFamily.InterNetwork select ipadd.Address.ToString());
            }

            return ipAddressItems;
        }

        private static bool IsImageDataValid(Stream stream) {
            try {
                var header = new byte[8]; // 读取文件头的字节数
                if (stream.Read(header, 0, 8) != 8) {
                    return false;
                }

                // 判断图像文件头是否匹配有效的图像格式
                // 这里以JPEG格式为例
                var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF };
                for (var i = 0; i < 3; i++) {
                    if (header[i] != jpegHeader[i]) {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return false;
            }
        }
    }
}