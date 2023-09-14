using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using static JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.DaHuatechSecurityCamera;

namespace JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech {

    public class BaseDaHuatech {
        private static fDisConnectCallBack? _mDisConnectCallBack;
        private static fHaveReConnectCallBack? _mReConnectCallBack;
        private static fRealDataCallBackEx2? _mRealDataCallBackEx2;
        private static fSearchDevicesCBEx? _mSearchDevicesCbEx;
        private static fSnapRevCallBack? _mSnapRevCallBack;
        private static BaseDaHuatech? _instance;
        private static object _initLock = new();
        private static SemaphoreSlim _enumerateSlim = new(1);
        private static ConcurrentDictionary<string, DEVICE_NET_INFO_EX> _devInfo = new();
        private static ConcurrentDictionary<string, IntPtr> _loginDev = new();
        private static ConcurrentQueue<ImageMessageInfo> _imageMessageQueue = new();
        private static ConcurrentDictionary<string, Action<Bitmap?>> _imageEvent = new();
        private static ConcurrentDictionary<string, Action<Bitmap?>> _realtimeFrameEvent = new();
        private static ConcurrentDictionary<string, IntPtr> _realPlayInfo = new();
        private static SemaphoreSlim _snapRevPhotoSlim = new(1);
        private static SemaphoreSlim _realtimeFrameSlim = new(1);
        private static byte[] _imageBytes = Array.Empty<byte>();
        private static byte[] _realtimeFrameBytes = Array.Empty<byte>();
        private static SemaphoreSlim _takePhotoSlim = new(1);
        private static SemaphoreSlim _switchRealtimeFrameSlim = new(1);

        private BaseDaHuatech() {
        }

        public static BaseDaHuatech CreateInstance() {
            //定义事件
            //判断初始化
            if (_instance is null) {
                lock (_initLock) {
                    _instance ??= new BaseDaHuatech();

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
                    _mDisConnectCallBack += delegate (IntPtr id, IntPtr dvrip, int port, IntPtr user) {
                    };
                    _mReConnectCallBack += delegate (IntPtr id, IntPtr dvrip, int port, IntPtr user) {
                    };
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
                            finally {
                                _realtimeFrameSlim.Release();
                            }
                        };
                    _mSnapRevCallBack += async delegate (IntPtr id, IntPtr buf, uint len, uint type, uint serial, IntPtr user) {
                        if (len > 0) {
                            try {
                                await _snapRevPhotoSlim.WaitAsync();

                                // 取出登录id
                                var (key, value) = _loginDev.FirstOrDefault(f => f.Value == id);
                                if (key != null) {
                                    // 取出绑定事件
                                    var tryGetValue = _imageEvent.TryGetValue(key, out var callback);
                                    if (tryGetValue) {
                                        Image? imageBitmap = null;
                                        if (type == 10) //.jpg
                                        {
                                            unsafe {
                                                byte[] fixedBuffer = new byte[len];  // 固定内存缓冲区

                                                fixed (byte* pBuffer = fixedBuffer) {
                                                    var ptr = new IntPtr(pBuffer);
                                                    Marshal.Copy(buf, fixedBuffer, 0, (int)len);

                                                    using var stream = new UnmanagedMemoryStream(pBuffer, len);
                                                    stream.Seek(0, SeekOrigin.Begin);
                                                    imageBitmap = Image.FromStream(stream);
                                                }
                                            }
                                        }

                                        var image = imageBitmap?.GetThumbnailImage(imageBitmap.Width, imageBitmap.Height,
                                            () => false, IntPtr.Zero);

                                        if (image != null) callback?.Invoke((Bitmap)image);

                                        imageBitmap?.Dispose();
                                    }
                                }
                            }
                            finally {
                                _snapRevPhotoSlim.Release();
                            }
                        }
                    };
                    NETClient.SetNetworkParam(new NET_PARAM() {
                        nWaittime = 10000,// 等待超时时间(毫秒)
                        nConnectTime = 10000,// 连接超时时间(毫秒)
                    });
                    NETClient.Init(_mDisConnectCallBack, IntPtr.Zero, null);
                    //自动重连
                    NETClient.SetAutoReconnect(_mReConnectCallBack, IntPtr.Zero);
                    //抓图回调
                    NETClient.SetSnapRevCallBack(_mSnapRevCallBack, IntPtr.Zero);
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
                throw new Exception(e.Message, e);
            }
            return devices;
        }

        /// <summary>
        /// 获取所有Ip地址
        /// </summary>
        /// <returns></returns>
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
        /// 注册图片回调事件
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="callback"></param>
        public void RegisterImageCallback(string serialNo, [NotNull] Action<Bitmap> callback) {
            _imageEvent.AddOrUpdate(serialNo, callback, (k, v) => callback);
        }

        /// <summary>
        /// 注册实时录像回调
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="callback"></param>
        public void RegisterRealtimeFrameCallback(string serialNo, [NotNull] Action<Bitmap> callback) {
            _realtimeFrameEvent.AddOrUpdate(serialNo, callback, (k, v) => callback);
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
                await Task.Delay(150);
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
                await _switchRealtimeFrameSlim.WaitAsync();
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
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _switchRealtimeFrameSlim.Release();
            }
        }

        /// <summary>
        /// 停止实时画面预览
        /// </summary>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> StopRealtimePlay(string serialNo) {
            try {
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
            }

            //RealPlay
        }
    }
}