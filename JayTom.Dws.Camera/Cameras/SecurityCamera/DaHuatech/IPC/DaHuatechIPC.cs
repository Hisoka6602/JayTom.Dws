using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using System.Drawing;
using DaHua.Play.Net;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using static DaHua.Play.Net.DhPlaySdk;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR;

namespace JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.IPC {

    public class DaHuatechIPC {
        private static readonly Lazy<DaHuatechIPC> _ipcInstance = new(() => new DaHuatechIPC());
        private static BaseDaHuatech? _instance;
        private static ConcurrentDictionary<string, IpcDevInfo> _loginDev = new();
        private static SemaphoreSlim _logInSlim = new(1);
        private static SemaphoreSlim _switchRealtimeFrameSlim = new(1);
        private static SemaphoreSlim _captureSlim = new(1);
        private static DecCBFun? _decCbFun;
        private static readonly Channel<(Func<Bitmap, Task> Callback, Bitmap Image)> _channel =
            Channel.CreateBounded<(Func<Bitmap, Task>, Bitmap)>(
                new BoundedChannelOptions(8) {
                    SingleReader = true,
                    FullMode = BoundedChannelFullMode.Wait
                });
        private static Task? _channelProcessor;
        public static DaHuatechIPC Instance => _ipcInstance.Value;

        private DaHuatechIPC() {
            _instance ??= BaseDaHuatech.CreateInstance();
            _decCbFun += delegate (int port, IntPtr buf, int size, ref DhPlaySdk.FRAME_INFO info, IntPtr data, int reserved2) {
                var item = _loginDev.FirstOrDefault(entry => entry.Value.DevPlayInfo.PlayPort == port);
                if (item.Value?.DevPlayInfo.RealtimeFrameBitmapCallBack is { } callback) {
                    item.Value.DevPlayInfo.CaptureSize = new Size(info.nWidth, info.nHeight);
                    var bitmap = DhPlaySdk.ConvertToGrayscaleBmp(buf, size, info);
                    if (!_channel.Writer.TryWrite((callback, bitmap))) {
                        bitmap.Dispose();
                    }
                }
            };
            _channelProcessor = ProcessChannel();
        }

        /// <summary>
        /// 处理回调
        /// </summary>
        private static async Task ProcessChannel() {
            await foreach (var item in _channel.Reader.ReadAllAsync()) {
                try {
                    await item.Callback(item.Image).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    item.Image.Dispose();
                    NLog.LogManager.GetCurrentClassLogger().Error($"处理回调异常:{ex}");
                }
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <param name="playChannelId"></param>
        /// <param name="realtimeFrameBitmapCallBack"></param>
        /// <param name="captureCallBack"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> LogIn(string serialNo, string userName, string passWord,
            int playChannelId = 0,
            Func<Bitmap, Task>? realtimeFrameBitmapCallBack = null,
            Func<CaptureInfo, Task>? captureCallBack = null) {
            await Task.Yield();
            try {
                await _logInSlim.WaitAsync();
                var tryGetValue = _loginDev.TryGetValue(serialNo, out var loginInfo);
                if (!tryGetValue || (loginInfo?.LogInHandle == IntPtr.Zero)) {
                    var info = _instance?.GetDeviceNetInfo(serialNo);
                    if (info is not null) {
                        var mDeviceInfo = new NET_DEVICEINFO_Ex();
                        var mLoginId = NETClient.LoginWithHighLevelSecurity(info.Value.szIP
                            , (ushort)info.Value.nPort, userName, passWord,
                            EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref mDeviceInfo);
                        if (IntPtr.Zero == mLoginId) {
                            var lastError = NETClient.GetLastError();
                            return new KeyValuePair<bool, object>(false, lastError);
                        }
                        //添加到字典
                        loginInfo = new IpcDevInfo { LogInHandle = mLoginId, LoggedInDeviceInfo = mDeviceInfo };
                        if (realtimeFrameBitmapCallBack is not null) {
                            loginInfo.DevPlayInfo.RealtimeFrameBitmapCallBack = realtimeFrameBitmapCallBack;
                        }
                        if (captureCallBack is not null) {
                            loginInfo.DevPlayInfo.CaptureCallBack = captureCallBack;
                        }
                        _loginDev.TryAdd(serialNo, loginInfo);

                        return new KeyValuePair<bool, object>(true, mLoginId.ToString());
                    }
                    else {
                        return new KeyValuePair<bool, object>(false, "不存在该设备或未枚举");
                    }
                }
                else {
                    return new KeyValuePair<bool, object>(true, loginInfo);
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, e.Message);
            }
            finally {
                _logInSlim.Release();
            }
        }

        /// <summary>
        /// 退出
        /// </summary>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> LogOut(string serialNo) {
            await Task.Yield();
            try {
                await _logInSlim.WaitAsync();
                _loginDev.TryGetValue(serialNo, out var mLoginId);
                if (mLoginId != null && mLoginId.LogInHandle != IntPtr.Zero) {
                    if (mLoginId.DevPlayInfo.PlaybackMode != PlaybackMode.None && mLoginId.DevPlayInfo.PlayHandle != IntPtr.Zero) {
                        await StopRealtimePlay(serialNo);
                    }
                    //注销其他事件
                    var result = NETClient.Logout(mLoginId.LogInHandle);
                    if (!result) {
                        var lastError = NETClient.GetLastError();
                        //退出play
                        return new KeyValuePair<bool, string>(false, lastError);
                    }
                    _loginDev.TryRemove(serialNo, out mLoginId);
                    return new KeyValuePair<bool, string>(true, mLoginId?.ToString() ?? string.Empty);
                }
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _logInSlim.Release();
            }
        }

        /// <summary>
        /// 开始实时画面
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="realtimeFrameBitmapCallBack"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> StartRealtimePlay(string serialNo, Func<Bitmap, Task>? realtimeFrameBitmapCallBack = null) {
            try {
                await _switchRealtimeFrameSlim.WaitAsync();

                var tryGetValue = _loginDev.TryGetValue(serialNo, out var dev);
                if (tryGetValue && dev is not null) {
                    if (dev.DevPlayInfo.PlaybackMode == PlaybackMode.RealTime) {
                        return new KeyValuePair<bool, string>(true, "已开启实时预览");
                    }
                    var playGetFreePort = DhPlaySdk.PLAY_GetFreePort(out var plPort);
                    if (!playGetFreePort) {
                        return new KeyValuePair<bool, string>(playGetFreePort, "获取端口失败!");
                    }

                    var exists = false;
                    do {
                        exists = _loginDev.Any() &&
                                 _loginDev.FirstOrDefault(f => f.Value.DevPlayInfo.PlayPort == plPort && !f.Key.Equals(serialNo))
                                     .Value != null;
                        plPort++;
                    } while (exists);

                    dev.DevPlayInfo.PlayPort = plPort;
                    var openMode = DhPlaySdk.PLAY_SetStreamOpenMode(plPort, 1);
                    if (!openMode) {
                        return new KeyValuePair<bool, string>(openMode, "设置流模式失败!");
                    }

                    var playSetDecCbStream = DhPlaySdk.PLAY_SetDecCBStream(plPort, 1);
                    if (!playSetDecCbStream) {
                        return new KeyValuePair<bool, string>(openMode, "设置缓存区域失败!");
                    }

                    var playOpenStream = DhPlaySdk.PLAY_OpenStream(plPort, IntPtr.Zero, 0, 1024 * 512 * 6);

                    if (!playOpenStream) {
                        return new KeyValuePair<bool, string>(openMode, "开启播放流失败!");
                    }

                    var realPlayId = NETClient.RealPlay(dev.LogInHandle, dev.DevPlayInfo.PlayChannelId, IntPtr.Zero);
                    if (realPlayId == IntPtr.Zero) {
                        return new KeyValuePair<bool, string>(false, "通道播放失败!");
                    }
                    dev.DevPlayInfo.PlayHandle = realPlayId;
                    //设置播放回调
                    var realDataCallBack = NETClient.SetRealDataCallBack(realPlayId,
                        (handle, type, buffer, size, param, user) => {
                            if (type == 0) {
                                NETClient.PlayInputData((int)user, buffer, size);
                            }
                        }, plPort,
                        EM_REALDATA_FLAG.DATA_WITH_FRAME_INFO | EM_REALDATA_FLAG.PCM_AUDIO_DATA | EM_REALDATA_FLAG.RAW_DATA | EM_REALDATA_FLAG.YUV_DATA);
                    if (!realDataCallBack) {
                        return new KeyValuePair<bool, string>(realDataCallBack, "设置播放回调失败!");
                    }
                    //设置解码模块

                    var playSetEngine = DhPlaySdk.PLAY_SetEngine(plPort, DecodeType.Hevc, 0);

                    if (!playSetEngine) {
                        return new KeyValuePair<bool, string>(playSetEngine, "设置解码模块失败!");
                    }
                    //设置图片质量
                    var playSetPicQuality = DhPlaySdk.PLAY_SetPicQuality(plPort, true);

                    if (!playSetPicQuality) {
                        return new KeyValuePair<bool, string>(playSetEngine, "设置图片质量失败!");
                    }
                    /*//设置颜色
                    var playSetColor = DhPlaySdk.PLAY_SetColor(plPort, 0, 64, 64, 64, 64);
                    if (!playSetColor) {
                        return new KeyValuePair<bool, string>(playSetEngine, "设置颜色失败!");
                    }*/
                    //启用高清图像内部调整策略

                    var picAdjustment = DhPlaySdk.PLAY_EnableLargePicAdjustment(plPort, true);
                    if (!picAdjustment) {
                        return new KeyValuePair<bool, string>(picAdjustment, "启用高清图像内部调整策略失败!");
                    }

                    var playPlay = DhPlaySdk.PLAY_Play(plPort, IntPtr.Zero);

                    if (!playPlay) {
                        return new KeyValuePair<bool, string>(playPlay, "播放失败!");
                    }
                    var playSetDecCallBack = _decCbFun != null && DhPlaySdk.PLAY_SetDecCallBack(plPort, _decCbFun);
                    dev.DevPlayInfo.PlaybackMode = playSetDecCallBack ? PlaybackMode.RealTime : PlaybackMode.None;
                    if (playSetDecCallBack && realtimeFrameBitmapCallBack is not null) {
                        dev.DevPlayInfo.RealtimeFrameBitmapCallBack = realtimeFrameBitmapCallBack;
                    }
                    return new KeyValuePair<bool, string>(playSetDecCallBack, $"{(playSetDecCallBack ? "开启实时预览成功" : "设置播放回调失败!")}");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                _switchRealtimeFrameSlim.Release();
            }
            return new KeyValuePair<bool, string>(false, "开启实时预览失败");
        }

        /// <summary>
        /// 停止实时画面
        /// </summary>
        /// <param name="serialNo"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> StopRealtimePlay(string serialNo) {
            try {
                await _switchRealtimeFrameSlim.WaitAsync();
                var tryGetValue = _loginDev.TryGetValue(serialNo, out var mLoginId);
                if (tryGetValue && mLoginId is not null) {
                    var indexOf = _loginDev.Keys.OrderBy(o => o).ToList().IndexOf(serialNo);
                    //判断是否已经开启

                    if (mLoginId.DevPlayInfo.PlaybackMode == PlaybackMode.RealTime && mLoginId.DevPlayInfo.PlayHandle != IntPtr.Zero) {
                        var ret = NETClient.StopRealPlay(mLoginId.DevPlayInfo.PlayHandle);
                        if (!ret) {
                            var lastError = NETClient.GetLastError();
                            return new KeyValuePair<bool, string>(false, lastError);
                        }
                        //停止数据回调
                        var playStop = DhPlaySdk.PLAY_Stop(mLoginId.DevPlayInfo.PlayPort);
                        if (playStop) {
                            DhPlaySdk.PLAY_ResetSourceBuffer(mLoginId.DevPlayInfo.PlayPort);
                            PLAY_CloseStream(mLoginId.DevPlayInfo.PlayPort);
                            mLoginId.DevPlayInfo = new DevPlayInfo();
                        }
                        return new KeyValuePair<bool, string>(true, string.Empty);
                    }
                    else {
                        return new KeyValuePair<bool, string>(true, $"未开启预览:{mLoginId.SerialNo}");
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
        }

        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="timestamp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task CaptureAsync(string serialNo, long timestamp, CancellationToken cancellation = default) {
            //获取已登录的设备
            await _captureSlim.WaitAsync(cancellation);
            var tryGetValue = _loginDev.TryGetValue(serialNo, out var dev);
            if (tryGetValue && dev is { DevPlayInfo.CaptureSize: not null, DevPlayInfo.PlayPort: > 0 }) {
                uint bmpSize = 0;
                // 计算缓冲区大小
                var bufferSize = (uint)(40 + dev.DevPlayInfo.CaptureSize.Value.Width * dev.DevPlayInfo.CaptureSize.Value.Height * 4);

                // 分配缓冲区
                var bmpBuffer = Marshal.AllocHGlobal((int)bufferSize);

                try {
                    if (DhPlaySdk.PLAY_GetPicBMP(dev.DevPlayInfo.PlayPort, bmpBuffer, bufferSize, ref bmpSize)) {
                        // 使用非托管内存中的数据创建 Bitmap 对象
                        using var stream = new System.IO.MemoryStream();
                        byte[] bmpData = new byte[bmpSize];
                        Marshal.Copy(bmpBuffer, bmpData, 0, (int)bmpSize);
                        stream.Write(bmpData, 0, bmpData.Length);
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                        var bitmap = new Bitmap(stream);
                        var captureCallBack = dev.DevPlayInfo.CaptureCallBack;
                        if (captureCallBack is not null) {
                            await captureCallBack(new CaptureInfo() {
                                Bitmap = bitmap,
                                SerialNo = serialNo,
                                Timestamp = timestamp
                            }).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
                finally {
                    Marshal.FreeHGlobal(bmpBuffer);
                }
            }
        }

        /// <summary>
        /// 注册截图回调事件
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="callback"></param>
        public void RegisterCaptureCallback(string serialNo, Func<CaptureInfo, Task> callback) {
            var tryGetValue = _loginDev.TryGetValue(serialNo, out var dev);
            if (tryGetValue && dev is not null) {
                dev.DevPlayInfo.CaptureCallBack = callback;
            }
        }

        /// <summary>
        /// 注册实时回调(Bitmap)
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="callback"></param>
        public void RegisterRealtimeFrameCallback(string serialNo, Func<Bitmap, Task> callback) {
            var tryGetValue = _loginDev.TryGetValue(serialNo, out var dev);
            if (tryGetValue && dev is not null) {
                dev.DevPlayInfo.RealtimeFrameBitmapCallBack = callback;
            }
        }

        /// <summary>
        /// 获取已登录设备信息
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public List<IpcDevInfo>? GetDevLogInInfo(Expression<Func<IpcDevInfo, bool>> @where) {
            var compiledWhere = @where.Compile();

            return _loginDev.Values.Where(compiledWhere)?.ToList();
        }
    }

    public class CaptureInfo {

        /// <summary>
        /// 传入的时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string SerialNo { get; set; } = string.Empty;

        /// <summary>
        /// 通道
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public Bitmap? Bitmap { get; set; }
    }
}
