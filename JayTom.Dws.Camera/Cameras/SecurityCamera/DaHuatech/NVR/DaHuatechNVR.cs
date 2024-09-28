using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using System.Drawing;
using DaHua.Play.Net;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static DaHua.Play.Net.DhPlaySdk;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.IPC;

namespace JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech.NVR {

    public class DaHuatechNVR {
        private static readonly Lazy<DaHuatechNVR> _nvrInstance = new(() => new DaHuatechNVR());
        private static BaseDaHuatech? _instance;
        private static ConcurrentDictionary<string, NvrDevInfo> _loginDev = new();
        private static SemaphoreSlim _logInSlim = new(1);
        private static ConcurrentDictionary<string, HistoricalWatermark> _historicalWatermarkInfos = new();
        private static SemaphoreSlim _switchRealtimeFrameSlim = new(1);
        private static SemaphoreSlim _captureSlim = new(1);
        private static SemaphoreSlim _changingViewSizeSlim = new(1);

        private static fCBDecode? _fCbDecode;
        private static fCBDecode? _recordingfCbDecode;
        private static fTimeDownLoadPosCallBack? _timeDownLoadPosCallBack;
        private static bool isloaded;
        private static Channel<(long port, FRAME_DECODE_INFO pFrameDecodeInfo, FRAME_INFO_EX pFrameInfo, IntPtr pUser)> _fcbChannel = Channel.CreateUnbounded<(long port, FRAME_DECODE_INFO pFrameDecodeInfo, FRAME_INFO_EX pFrameInfo, IntPtr pUser)>();
        private static Channel<(long port, FRAME_DECODE_INFO pFrameDecodeInfo, FRAME_INFO_EX pFrameInfo, IntPtr pUser)> _recordingChannel = Channel.CreateUnbounded<(long port, FRAME_DECODE_INFO pFrameDecodeInfo, FRAME_INFO_EX pFrameInfo, IntPtr pUser)>();
        private readonly SemaphoreSlim _ptzOperationSlim = new(1);
        public static DaHuatechNVR Instance => _nvrInstance.Value;
        private static bool _isChangingViewSize;

        private DaHuatechNVR() {
            _instance ??= BaseDaHuatech.CreateInstance();
            _fCbDecode += (long port, ref FRAME_DECODE_INFO info, ref FRAME_INFO_EX frameInfo, IntPtr user) => {
                if (!_fcbChannel.Writer.TryWrite((port, info, frameInfo, user))) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"-回调实时流异常");
                }
            };
            _recordingfCbDecode +=
                (long port, ref FRAME_DECODE_INFO info, ref FRAME_INFO_EX frameInfo, IntPtr user) => {
                    if (!_recordingChannel.Writer.TryWrite((port, info, frameInfo, user))) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"-回调录像流异常");
                    }
                };

            _timeDownLoadPosCallBack += (handle, size, loadSize, index, recordfileinfo, user) => {
            };
            ProcessVisibleDecodeChannel();
            RecordingDecodeChannel();
        }

        /// <summary>
        /// 实时预览回调
        /// </summary>
        private static async void ProcessVisibleDecodeChannel() {
            await foreach (var item in _fcbChannel.Reader.ReadAllAsync()) {
                var (port, pFrameDecodeInfo, pFrameInfo, pUser) = item;
                try {
                    var playInfo = _loginDev.Values
                        .SelectMany(nvrDevInfo => nvrDevInfo.DevPlayInfos) // 扁平化 DevPlayInfos 列表
                        .FirstOrDefault(devPlayInfo =>
                            devPlayInfo.PlayChannelId.Equals((int)pUser) &&
                            devPlayInfo.PlayPort == port &&
                            devPlayInfo.PlaybackMode == PlaybackMode.RealTime);
                    var callBack = playInfo?.RealtimePreviewCallBack;
                    if (callBack != null && playInfo is not null && !_isChangingViewSize) {
                        var bytes = DhPlaySdk.ConvertFrameInfoToRgbByteArray(pFrameDecodeInfo, playInfo.NvrPreviewSize.Width, playInfo.NvrPreviewSize.Height);

                        _ = callBack(new RealtimePreviewInfo() {
                            ChannelId = (int)pUser,
                            RgbData = bytes,
                            Width = playInfo.NvrPreviewSize.Width,
                            Height = playInfo.NvrPreviewSize.Height,
                        }).ConfigureAwait(false);
                        playInfo.CaptureSize ??= new Size(pFrameInfo.nWidth, pFrameInfo.nHeight);
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"处理实时回调异常:{e}");
                }
            }
        }

        /// <summary>
        /// 录像回调
        /// </summary>
        private static async void RecordingDecodeChannel() {
            await foreach (var item in _recordingChannel.Reader.ReadAllAsync()) {
                var (port, pFrameDecodeInfo, pFrameInfo, pUser) = item;
                try {
                    var playInfo = _loginDev.Values
                        .SelectMany(nvrDevInfo => nvrDevInfo.DevPlayInfos) // 扁平化 DevPlayInfos 列表
                        .FirstOrDefault(devPlayInfo =>
                            devPlayInfo.PlayChannelId.Equals((int)pUser) &&
                            devPlayInfo.PlayPort == port &&
                            devPlayInfo.PlaybackMode == PlaybackMode.Recording);
                    var callBack = playInfo?.PlayBackCallBack;
                    /*var playInfo = _loginDev.FirstOrDefault().Value?.DevPlayInfos?.FirstOrDefault();
                    var callBack = playInfo?.PlayBackCallBack;*/
                    if (callBack != null && playInfo is not null && !_isChangingViewSize) {
                        var bytes = ConvertFrameInfoToRgbByteArray(pFrameDecodeInfo, playInfo.NvrPreviewSize.Width, playInfo.NvrPreviewSize.Height);

                        _ = callBack(new RealtimePreviewInfo() {
                            ChannelId = (int)pUser,
                            RgbData = bytes,
                            Width = playInfo.NvrPreviewSize.Width,
                            Height = playInfo.NvrPreviewSize.Height,
                        }).ConfigureAwait(false);
                        playInfo.CaptureSize ??= new Size(pFrameInfo.nWidth, pFrameInfo.nHeight);
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"处理实时回调异常:{e}");
                }
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> LogIn(string ipAddress, int port, string userName, string passWord) {
            try {
                await _logInSlim.WaitAsync();
                var tryGetValue = _loginDev.TryGetValue(ipAddress, out var logInInfo);
                if (!tryGetValue || (logInInfo?.LogInHandle == IntPtr.Zero)) {
                    var mDeviceInfo = new NET_DEVICEINFO_Ex();
                    var mLoginId = NETClient.LoginWithHighLevelSecurity(ipAddress
                        , (ushort)port, userName, passWord,
                        EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref mDeviceInfo);
                    if (IntPtr.Zero == mLoginId) {
                        var lastError = NETClient.GetLastError();
                        return new KeyValuePair<bool, object>(false, lastError);
                    }

                    logInInfo = new NvrDevInfo { IpAddress = ipAddress, ChannelCount = mDeviceInfo.nChanNum, LogInHandle = mLoginId, LoggedInDeviceInfo = mDeviceInfo };
                    //添加到字典
                    _loginDev.TryAdd(ipAddress, logInInfo);

                    return new KeyValuePair<bool, object>(true, logInInfo);
                }
                else {
                    return new KeyValuePair<bool, object>(true, logInInfo);
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
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> LogOut(string ipAddress) {
            await Task.Yield();
            try {
                await _logInSlim.WaitAsync();
                _loginDev.TryGetValue(ipAddress, out var mLoginId);
                if (mLoginId != null && mLoginId.LogInHandle != IntPtr.Zero) {
                    var result = NETClient.Logout(mLoginId.LogInHandle);
                    if (!result) {
                        var lastError = NETClient.GetLastError();
                        //退出play
                        return new KeyValuePair<bool, string>(false, lastError);
                    }

                    _loginDev.TryRemove(ipAddress, out mLoginId);
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
        /// 开始预览
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="realtimePreviewCallBack"></param>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> StartRealTimePreview(string ipAddress, int channelId,
            Func<RealtimePreviewInfo, Task>? realtimePreviewCallBack = null,
            Size? viewSize = null) {
            try {
                await _switchRealtimeFrameSlim.WaitAsync();

                var tryGetValue = _loginDev.TryGetValue(ipAddress, out var dev);
                if (tryGetValue && dev is not null) {
                    var playInfo = dev.DevPlayInfos.FirstOrDefault(f =>
                        f.PlayChannelId.Equals(channelId));

                    if (playInfo is not null && playInfo.PlaybackMode == PlaybackMode.RealTime) {
                        return new KeyValuePair<bool, string>(true, "已开启实时预览");
                    }

                    if (playInfo is not null && playInfo.PlaybackMode == PlaybackMode.Recording) {
                        return new KeyValuePair<bool, string>(false, "请先关闭回放流");
                    }
                    var playGetFreePort = DhPlaySdk.PLAY_GetFreePort(out var plPort);
                    if (!playGetFreePort) {
                        return new KeyValuePair<bool, string>(playGetFreePort, "获取端口失败!");
                    }

                    var exists = false;
                    do {
                        exists = _loginDev.Values
                            .Any(nvrDevInfo => nvrDevInfo.DevPlayInfos
                                .Any(devPlayInfo => devPlayInfo.PlayPort == plPort));

                        if (exists) {
                            plPort++;
                        }
                    } while (exists);

                    playInfo = new DevPlayInfo() {
                        PlayChannelId = channelId,
                        PlayPort = plPort,
                        PlaybackMode = PlaybackMode.RealTime,
                    };
                    if (viewSize is not null) {
                        playInfo.NvrPreviewSize = new Size(viewSize.Value.Width, viewSize.Value.Height);
                    }
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

                    var realPlayId = NETClient.RealPlay(dev.LogInHandle, channelId, IntPtr.Zero);
                    if (realPlayId == IntPtr.Zero) {
                        return new KeyValuePair<bool, string>(false, "通道播放失败!");
                    }
                    playInfo.PlayHandle = realPlayId;
                    //设置播放回调
                    var realDataCallBack = NETClient.SetRealDataCallBack(realPlayId,
                        (handle, type, buffer, size, nint, user) => {
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
                    //启用高清图像内部调整策略

                    var picAdjustment = DhPlaySdk.PLAY_EnableLargePicAdjustment(plPort, true);
                    if (!picAdjustment) {
                        return new KeyValuePair<bool, string>(picAdjustment, "启用高清图像内部调整策略失败!");
                    }

                    var playPlay = DhPlaySdk.PLAY_Play(plPort, IntPtr.Zero);

                    if (!playPlay) {
                        return new KeyValuePair<bool, string>(playPlay, "播放失败!");
                    }

                    var playSetDecCallBack = _fCbDecode != null &&
                                             DhPlaySdk.PLAY_SetVisibleDecodeCallBack(plPort, _fCbDecode, channelId);
                    if (playSetDecCallBack) {
                        if (realtimePreviewCallBack is not null) {
                            playInfo.RealtimePreviewCallBack = realtimePreviewCallBack;
                        }
                        dev.DevPlayInfos.Add(playInfo);
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
        /// 停止实时预览
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, string>> StopRealtimePreview(string ipAddress, int channelId) {
            try {
                await _switchRealtimeFrameSlim.WaitAsync();
                var tryGetValue = _loginDev.TryGetValue(ipAddress, out var dev);
                if (tryGetValue && dev is not null) {
                    var playInfo = dev.DevPlayInfos.FirstOrDefault(f =>
                        f.PlayChannelId.Equals(channelId));
                    if (playInfo is null ||
                        playInfo.PlaybackMode != PlaybackMode.RealTime ||
                        playInfo.PlayHandle == IntPtr.Zero) {
                        return new KeyValuePair<bool, string>(false, $"未开启通道预览:地址[{ipAddress}],通道[{channelId}]");
                    }

                    var ret = NETClient.StopRealPlay(playInfo.PlayHandle);
                    if (!ret) {
                        var lastError = NETClient.GetLastError();
                        return new KeyValuePair<bool, string>(false, lastError);
                    }
                    //停止数据回调
                    var playStop = DhPlaySdk.PLAY_Stop(playInfo.PlayPort);
                    if (playStop) {
                        DhPlaySdk.PLAY_ResetSourceBuffer(playInfo.PlayPort);
                        PLAY_CloseStream(playInfo.PlayPort);
                        dev.DevPlayInfos.Remove(playInfo);
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
                _switchRealtimeFrameSlim.Release();
            }
        }

        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="fileName"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> CaptureAsync(string ipAddress, int channelId, string fileName, CancellationToken cancellation = default) {
            try {
                await _captureSlim.WaitAsync(cancellation);
                var playInfo = _loginDev.Values
                    .Where(nvrDevInfo => nvrDevInfo.IpAddress == ipAddress)
                    .SelectMany(nvrDevInfo => nvrDevInfo.DevPlayInfos)
                    .FirstOrDefault(devPlayInfo =>
                        devPlayInfo.PlayChannelId == channelId && devPlayInfo.CaptureSize != null);
                if (playInfo?.CaptureSize != null) {
                    return DhPlaySdk.PLAY_CatchPic(playInfo.PlayPort, fileName);
                }
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"截图异常:{e}");
            }
            finally {
                _captureSlim.Release();
            }
            return false;
        }

        /// <summary>
        /// 添加水印(叠加)
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="packAgeTimestamp"></param>
        /// <param name="content"></param>
        /// <param name="config"></param>
        public async void AddRealTimeWatermark(string ipAddress, int channelId, long packAgeTimestamp, string content, SecurityCameraWatermarkConfig config) {
            //每行间隔是70
            //获取位置坐标
            await Task.Delay(10);

            if (_loginDev.TryGetValue(ipAddress, out var dev)) {
                //添加
                _historicalWatermarkInfos.TryAdd($"{packAgeTimestamp}-{ipAddress}-{channelId}",
                    new HistoricalWatermark(ipAddress, dev.LogInHandle, channelId, content, DateTime.Now, config.Duration,
                        w => {
                            var (key, value) = _historicalWatermarkInfos.FirstOrDefault(f => f.Value != null
                                && f.Value.AddedTime.Equals(w.AddedTime));
                            if (value is not null) {
                                _historicalWatermarkInfos.Remove(key, out var info);
                                if (info is not null && !_historicalWatermarkInfos.Any(a =>
                                        a.Value != null && a.Value.LoginId.Equals(info.LoginId) && a.Value.ChannelId.Equals(info.ChannelId))) {
                                    DeleteAllWatermarks(info.SerialNo, info.ChannelId);
                                }
                                UpDateRealTimeWatermark(_historicalWatermarkInfos);
                            }
                        }) {
                        BackgroundColor = config.BackgroundColor,
                        ForegroundColor = config.ForegroundColor,
                        Position = config.Position,
                        Duration = config.Duration,
                        MaxWatermarks = config.MaxWatermarks,
                    });
                UpDateRealTimeWatermark(_historicalWatermarkInfos);
            }

            //判断是否超过上限
            //获取位置偏移
            //如果成功则添加到列表里面(填写过期移除)
        }

        /// <summary>
        /// 添加单个水印
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="packAgeTimestamp"></param>
        /// <param name="content"></param>
        /// <param name="config"></param>
        public void AddSingleRealTimeWatermark(string ipAddress, int channelId, long packAgeTimestamp,
            string content, SecurityCameraWatermarkConfig config) {
            _historicalWatermarkInfos.Clear();
            AddRealTimeWatermark(ipAddress, channelId, packAgeTimestamp, content, config);
        }

        /// <summary>
        /// 添加水印(叠加)
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="packAgeTimestamp"></param>
        /// <param name="content"></param>
        /// <param name="config"></param>
        public async void AddRealTimeWatermark(List<(string IpAddress, int Channel)> devices, long packAgeTimestamp, string content,
          SecurityCameraWatermarkConfig config) {
            //每行间隔是70
            //获取位置坐标
            await Task.Delay(10);
            if (devices.Any()) {
                foreach (var device in devices) {
                    if (_loginDev.TryGetValue(device.IpAddress, out var dev)) {
                        //添加
                        _historicalWatermarkInfos.TryAdd($"{packAgeTimestamp}-{device.IpAddress}-{device.Channel}",
                            new HistoricalWatermark(device.IpAddress, dev.LogInHandle, device.Channel, content, DateTime.Now, config.Duration,
                                w => {
                                    var (key, value) = _historicalWatermarkInfos.FirstOrDefault(f => f.Value != null
                                        && f.Value.AddedTime.Equals(w.AddedTime));
                                    if (value is not null) {
                                        _historicalWatermarkInfos.Remove(key, out var info);
                                        if (info is not null && !_historicalWatermarkInfos.Any(a =>
                                                a.Value != null && a.Value.LoginId.Equals(info.LoginId) && a.Value.ChannelId.Equals(info.ChannelId))) {
                                            DeleteAllWatermarks(info.SerialNo, info.ChannelId);
                                        }
                                        UpDateRealTimeWatermark(_historicalWatermarkInfos);
                                    }
                                }) {
                                BackgroundColor = config.BackgroundColor,
                                ForegroundColor = config.ForegroundColor,
                                Position = config.Position,
                                Duration = config.Duration,
                                MaxWatermarks = config.MaxWatermarks,
                            });
                    }
                }
                UpDateRealTimeWatermark(_historicalWatermarkInfos);
            }
        }

        /// <summary>
        /// 添加单个水印
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="packAgeTimestamp"></param>
        /// <param name="content"></param>
        /// <param name="config"></param>
        public void AddSingleRealTimeWatermark(List<(string SerialNumber, int Channel)> devices, long packAgeTimestamp,
            string content, SecurityCameraWatermarkConfig config) {
            _historicalWatermarkInfos.Clear();
            AddRealTimeWatermark(devices, packAgeTimestamp, content, config);
        }

        private void UpDateRealTimeWatermark(ConcurrentDictionary<string, HistoricalWatermark> historicalWatermarkInfos) {
            //最小写入间隔是700
            var waitTime = 2300;
            historicalWatermarkInfos.GroupBy(g => new {
                g.Value.LoginId,
                g.Value.ChannelId
            }).Select(s => new RealTimeWatermarkInfo() {
                LoginId = s.Key.LoginId,
                ChannelId = s.Key.ChannelId,
                CustomInfo = GetNET_OSD_CUSTOM_TITLE(historicalWatermarkInfos.Select(s1 => s1.Value).Where(w => w.LoginId.Equals(s.Key.LoginId) && w.ChannelId.Equals(s.Key.ChannelId)).ToList(), 8),
                CustomAlign = GetNET_OSD_CUSTOM_TITLE_TEXT_ALIGN(historicalWatermarkInfos.Select(s1 => s1.Value).Where(w => w.LoginId.Equals(s.Key.LoginId) && w.ChannelId.Equals(s.Key.ChannelId)).ToList(), 8),
            }).Select(ac => new Action(() => {
                var osdConfig = NETClient.SetOSDConfig(ac.LoginId, EM_CFG_OSD_TYPE.CUSTOMTITLE, ac.ChannelId, ac.CustomInfo, waitTime);
                if (!osdConfig) {
                    NLog.LogManager.GetCurrentClassLogger().Error(NETClient.GetLastError());
                }
                /*osdConfig = NETClient.SetOSDConfig(ac.LoginId, EM_CFG_OSD_TYPE.CUSTOMTITLETEXTALIGN, ac.ChannelId, ac.CustomAlign, waitTime);
                if (!osdConfig) {
                    NLog.LogManager.GetCurrentClassLogger().Error(NETClient.GetLastError());
                }*/
            })).ToList().ForEach(action => action.Invoke());
        }

        private NET_OSD_CUSTOM_TITLE GetNET_OSD_CUSTOM_TITLE(List<HistoricalWatermark> historicalWatermarkInfos, int maxWatermarks) {
            var customInfo = new NET_OSD_CUSTOM_TITLE {
                dwSize = (uint)Marshal.SizeOf(typeof(NET_OSD_CUSTOM_TITLE)),
                emOsdBlendType = EM_OSD_BLEND_TYPE.MAIN,
                nCustomTitleNum = maxWatermarks,
                stuCustomTitle = new NET_CUSTOM_TITLE_INFO[8],
            };
            historicalWatermarkInfos.OrderByDescending(o => o.AddedTime).Take(7).Select((s, i) =>
                new Action(() => {
                    customInfo.stuCustomTitle[i + 1].bEncodeBlend = true; //等于false会清除
                    customInfo.stuCustomTitle[i + 1].stuRect.left = 10;
                    customInfo.stuCustomTitle[i + 1].stuRect.top = (i * 70) + 10;
                    customInfo.stuCustomTitle[i + 1].stuBackColor.nAlpha = s.BackgroundColor.A;
                    customInfo.stuCustomTitle[i + 1].stuBackColor.nBlue = s.BackgroundColor.B;
                    customInfo.stuCustomTitle[i + 1].stuBackColor.nGreen = s.BackgroundColor.G;
                    customInfo.stuCustomTitle[i + 1].stuBackColor.nRed = s.BackgroundColor.R;
                    customInfo.stuCustomTitle[i + 1].stuFrontColor.nAlpha = s.ForegroundColor.A;
                    customInfo.stuCustomTitle[i + 1].stuFrontColor.nBlue = s.BackgroundColor.B;
                    customInfo.stuCustomTitle[i + 1].stuFrontColor.nGreen = s.BackgroundColor.G;
                    customInfo.stuCustomTitle[i + 1].stuFrontColor.nRed = s.BackgroundColor.R;
                    customInfo.stuCustomTitle[i + 1].szText = s.Content;
                })).ToList().ForEach(action => action.Invoke());
            return customInfo;
        }

        private NET_OSD_CUSTOM_TITLE_TEXT_ALIGN GetNET_OSD_CUSTOM_TITLE_TEXT_ALIGN(List<HistoricalWatermark> historicalWatermarkInfos, int maxWatermarks) {
            var customAlign = new NET_OSD_CUSTOM_TITLE_TEXT_ALIGN {
                dwSize = (uint)Marshal.SizeOf(typeof(NET_OSD_CUSTOM_TITLE_TEXT_ALIGN)),
                nCustomTitleNum = maxWatermarks,
                emTextAlign = new EM_TITLE_TEXT_ALIGNTYPE[8]
            };

            historicalWatermarkInfos.OrderByDescending(o => o.AddedTime).Take(8).Select((s, i) =>
                new Action(() => {
                    //换行对齐
                    customAlign.emTextAlign[i] = EM_TITLE_TEXT_ALIGNTYPE.CHANGELINE;
                })).ToList().ForEach(action => action.Invoke());
            return customAlign;
        }

        /// <summary>
        /// 删除全部水印
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        public async void DeleteAllWatermarks(string ipAddress, int channelId) {
            await Task.Delay(200);
            var waitTime = 3300;
            if (_loginDev.TryGetValue(ipAddress, out var dev)) {
                var customInfo = new NET_OSD_CUSTOM_TITLE {
                    dwSize = (uint)Marshal.SizeOf(typeof(NET_OSD_CUSTOM_TITLE)),
                    emOsdBlendType = EM_OSD_BLEND_TYPE.MAIN,
                    nCustomTitleNum = 8,
                    stuCustomTitle = new NET_CUSTOM_TITLE_INFO[8],
                };

                var osdConfig = NETClient.SetOSDConfig(dev.LogInHandle, EM_CFG_OSD_TYPE.CUSTOMTITLE, channelId, customInfo, waitTime);
                if (!osdConfig) {
                    NLog.LogManager.GetCurrentClassLogger().Error(NETClient.GetLastError());
                }
            }
        }

        /// <summary>
        /// 调整缩放倍率
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="increase"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public async Task AdjustZoomContinuouslyAsync(string ipAddress, int channelId, bool increase, bool stop) {
            var speed = 4;
            try {
                await _ptzOperationSlim.WaitAsync();
                if (_loginDev.TryGetValue(ipAddress, out var dev)) {
                    var ptzControl = NETClient.PTZControl(dev.LogInHandle, channelId,
                        increase ? EM_EXTPTZ_ControlType.ZOOM_ADD_CONTROL : EM_EXTPTZ_ControlType.ZOOM_DEC_CONTROL, 0,
                        speed, 0, stop, IntPtr.Zero);
                    if (!ptzControl) {
                        NLog.LogManager.GetCurrentClassLogger().Error(NETClient.GetLastError());
                    }
                }
            }
            finally {
                _ptzOperationSlim.Release();
            }
        }

        /// <summary>
        /// 调节焦点
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="increase"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public async Task AdjustPtzFocusContinuouslyAsync(string ipAddress, int channelId, bool increase, bool stop) {
            var speed = 4;
            try {
                await _ptzOperationSlim.WaitAsync();
                if (_loginDev.TryGetValue(ipAddress, out var dev)) {
                    var ptzControl = NETClient.PTZControl(dev.LogInHandle, channelId,
                        increase ? EM_EXTPTZ_ControlType.FOCUS_ADD_CONTROL : EM_EXTPTZ_ControlType.FOCUS_DEC_CONTROL, 0,
                        speed, 0, stop, IntPtr.Zero);
                    if (!ptzControl) {
                        NLog.LogManager.GetCurrentClassLogger().Error(NETClient.GetLastError());
                    }
                }
            }
            finally {
                _ptzOperationSlim.Release();
            }
        }

        /// <summary>
        /// 自动对焦
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task AutoFocusAsync(string ipAddress, int channelId) {
            var speed = 1;
            try {
                await _ptzOperationSlim.WaitAsync();
                if (_loginDev.TryGetValue(ipAddress, out var dev)) {
                    NETClient.PTZControl(dev.LogInHandle, channelId,
                        EM_EXTPTZ_ControlType.ZOOM_ADD_CONTROL, 0,
                        speed, 0, false, IntPtr.Zero);

                    var ptzControl = NETClient.PTZControl(dev.LogInHandle, channelId,
                        EM_EXTPTZ_ControlType.ZOOM_ADD_CONTROL, 0,
                        speed, 0, true, IntPtr.Zero);
                    if (!ptzControl) {
                        NLog.LogManager.GetCurrentClassLogger().Error(NETClient.GetLastError());
                    }
                }
            }
            finally {
                _ptzOperationSlim.Release();
            }
        }

        /// <summary>
        /// 获取NVR设备
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public List<NvrDevInfo>? GetDevLogInInfo(Expression<Func<NvrDevInfo, bool>> @where) {
            var compiledWhere = @where.Compile();

            return _loginDev.Values.Where(compiledWhere)?.ToList();
        }

        /// <summary>
        /// 查询录像文件
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <param name="videoStreamType"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> QueryVideoFile(string ipAddress, int channelId, DateTime startDateTime, DateTime endDateTime, int videoStreamType) {
            await Task.Yield();
            var fileCount = 0;
            //取出登录Id
            var tryGetValue = _loginDev.TryGetValue(ipAddress, out var info);
            if (tryGetValue && info is not null) {
                var streamType = videoStreamType == 0 ? EM_STREAM_TYPE.MAIN : EM_STREAM_TYPE.EXTRA_1;

                var pStream = IntPtr.Zero;
                try {
                    pStream = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));
                    Marshal.StructureToPtr((int)streamType, pStream, true);

                    NETClient.SetDeviceMode(info.LogInHandle, EM_USEDEV_MODE.RECORD_STREAM_TYPE, pStream);

                    var infos = new NET_RECORDFILE_INFO[5000];
                    var ret = NETClient.QueryRecordFile(info.LogInHandle, channelId, EM_QUERY_RECORD_TYPE.ALL, startDateTime, endDateTime, null, ref infos, ref fileCount, 5000, false);

                    if (!ret) {
                        return new KeyValuePair<bool, object>(false, NETClient.GetLastError());
                    }

                    return fileCount <= 0
                        ? new KeyValuePair<bool, object>(false, "None Record file(没有录像文件)!")
                        : new KeyValuePair<bool, object>(true, infos);
                }
                finally {
                    if (pStream != IntPtr.Zero) {
                        Marshal.FreeHGlobal(pStream);
                    }
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }
        }

        /// <summary>
        /// 播放录像
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <param name="playBackCallBack"></param>
        /// <param name="playBackProgressCallBack"></param>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> PlayBackVideo(string ipAddress, int channelId, DateTime startDateTime, DateTime endDateTime,
            Func<RealtimePreviewInfo, Task>? playBackCallBack = null,
            Func<PlayBackProgressInfo, Task>? playBackProgressCallBack = null,
            Size? viewSize = null) {
            await Task.Yield();

            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }

            var playInfo = dev.DevPlayInfos.FirstOrDefault(f => f.PlayChannelId.Equals(channelId));
            if (playInfo is null) {
                var playGetFreePort = DhPlaySdk.PLAY_GetFreePort(out var plPort);
                if (!playGetFreePort) {
                    return new KeyValuePair<bool, object>(playGetFreePort, "获取端口失败!");
                }

                bool exists;
                do {
                    exists = _loginDev.Values
                        .Any(nvrDevInfo => nvrDevInfo.DevPlayInfos
                            .Any(devPlayInfo => devPlayInfo.PlayPort == plPort));
                    if (exists) {
                        plPort++;
                    }
                } while (exists);

                playInfo = new DevPlayInfo() {
                    PlayChannelId = channelId,
                    PlayPort = plPort,
                    PlaybackMode = PlaybackMode.Recording,
                    PlayBackCallBack = playBackCallBack,
                    PlayBackProgressCallBack = playBackProgressCallBack
                };
                var openMode = DhPlaySdk.PLAY_SetStreamOpenMode(plPort, 1);
                if (!openMode) {
                    return new KeyValuePair<bool, object>(openMode, "设置流模式失败!");
                }

                var playSetDecCbStream = DhPlaySdk.PLAY_SetDecCBStream(plPort, 1);
                if (!playSetDecCbStream) {
                    return new KeyValuePair<bool, object>(openMode, "设置缓存区域失败!");
                }

                var playOpenStream = DhPlaySdk.PLAY_OpenStream(plPort, IntPtr.Zero, 0, 1024 * 512 * 6);

                if (!playOpenStream) {
                    return new KeyValuePair<bool, object>(openMode, "开启播放流失败!");
                }
            }
            else {
                if (playInfo.PlaybackMode == PlaybackMode.RealTime) {
                    return new KeyValuePair<bool, object>(false, "请先关闭实时流");
                }
            }

            if (viewSize is not null) {
                playInfo.NvrPreviewSize = new Size(viewSize.Value.Width, viewSize.Value.Height);
            }
            var stuInfo = new NET_IN_PLAY_BACK_BY_TIME_INFO();
            var stuOut = new NET_OUT_PLAY_BACK_BY_TIME_INFO();
            stuInfo.stStartTime = NET_TIME.FromDateTime(startDateTime);
            stuInfo.stStopTime = NET_TIME.FromDateTime(endDateTime);
            stuInfo.cbDownLoadPos = null;
            stuInfo.dwPosUser = playInfo.PlayPort;
            stuInfo.fDownLoadDataCallBack = (handle, type, buffer, size, user) => {
                if (type == 0) {
                    NETClient.PlayInputData((int)user, buffer, size);
                }
                return (int)size;
            };
            stuInfo.nPlayDirection = 0;
            stuInfo.nWaittime = 5000;
            stuInfo.dwDataUser = playInfo.PlayPort;
            if (playInfo.PlayBackProgressCallBack != null) {
                stuInfo.cbDownLoadPos = (handle, size, loadSize, user) => {
                    var info = _loginDev.Values
                        .SelectMany(nvrDevInfo => nvrDevInfo.DevPlayInfos) // 扁平化 DevPlayInfos 列表
                        .FirstOrDefault(devPlayInfo =>
                            devPlayInfo.PlayPort == user &&
                            devPlayInfo.PlaybackMode == PlaybackMode.Recording);
                    // 同步调用回调函数
                    info?.PlayBackProgressCallBack?.Invoke(new PlayBackProgressInfo() {
                        ChannelId = info.PlayChannelId,
                        LoadSize = (int)loadSize,
                        //IpAddress = dev.IpAddress,
                        Size = (int)size
                    });
                };
            }
            var realPlayId = NETClient.PlayBackByTime(dev.LogInHandle, channelId, stuInfo, ref stuOut);
            if (IntPtr.Zero == realPlayId) {
                return new KeyValuePair<bool, object>(false, "播放失败");
            }
            playInfo.PlayHandle = realPlayId;
            if (!dev.DevPlayInfos.Exists(e => e.PlayPort.Equals(playInfo.PlayPort))) {
                var playSetEngine = DhPlaySdk.PLAY_SetEngine(playInfo.PlayPort, DecodeType.Hevc, 0);

                if (!playSetEngine) {
                    return new KeyValuePair<bool, object>(playSetEngine, "设置解码模块失败!");
                }
                /*//设置图片质量
                var playSetPicQuality = DhPlaySdk.PLAY_SetPicQuality(playInfo.PlayPort, true);

                if (!playSetPicQuality) {
                    return new KeyValuePair<bool, object>(playSetEngine, "设置图片质量失败!");
                }
                //启用高清图像内部调整策略

                var picAdjustment = DhPlaySdk.PLAY_EnableLargePicAdjustment(playInfo.PlayPort, true);
                if (!picAdjustment) {
                    return new KeyValuePair<bool, object>(picAdjustment, "启用高清图像内部调整策略失败!");
                }*/

                var playPlay = DhPlaySdk.PLAY_Play(playInfo.PlayPort, IntPtr.Zero);

                if (!playPlay) {
                    return new KeyValuePair<bool, object>(playPlay, "播放失败!");
                }

                var playSetDecCallBack = _recordingfCbDecode != null &&
                                         DhPlaySdk.PLAY_SetVisibleDecodeCallBack(playInfo.PlayPort, _recordingfCbDecode, channelId);

                if (!playSetDecCallBack) {
                    return new KeyValuePair<bool, object>(false, "设置播放回调失败");
                }
                dev.DevPlayInfos.Add(playInfo);
            }
            return new KeyValuePair<bool, object>(true, "播放成功");
        }

        public async Task<KeyValuePair<bool, object>> ClosePlayBackVideo(string ipAddress, int channelId) {
            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }
            var playInfo = dev.DevPlayInfos.FirstOrDefault(f => f.PlayChannelId.Equals(channelId));
            if (playInfo is not null) {
                var playStop = DhPlaySdk.PLAY_Stop(playInfo.PlayPort);
                if (playStop) {
                    DhPlaySdk.PLAY_ResetSourceBuffer(playInfo.PlayPort);
                    PLAY_CloseStream(playInfo.PlayPort);
                    dev.DevPlayInfos.Remove(playInfo);
                }
            }

            return new KeyValuePair<bool, object>(true, "关闭成功");
        }

        /// <summary>
        /// 设置分辨率
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public async void SetResolution(string ipAddress, int channelId, int width, int height) {
            try {
                await _changingViewSizeSlim.WaitAsync();
                _isChangingViewSize = true;
                var playInfo = _loginDev.Values
                    .Where(nvrDevInfo => nvrDevInfo.IpAddress == ipAddress)
                    .SelectMany(nvrDevInfo => nvrDevInfo.DevPlayInfos)
                    .FirstOrDefault(devPlayInfo => devPlayInfo.PlayChannelId == channelId);
                if (playInfo is not null) {
                    playInfo.NvrPreviewSize = new Size(width, height);
                }

                _isChangingViewSize = false;
            }
            finally {
                _changingViewSizeSlim.Release();
            }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> StopPlayback(string ipAddress, int channelId) {
            await Task.Yield();

            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }
            var playInfo = dev.DevPlayInfos.FirstOrDefault(f => f.PlayChannelId.Equals(channelId));
            if (playInfo is null || playInfo.PlaybackMode != PlaybackMode.Recording) {
                return new KeyValuePair<bool, object>(false, "还未开启回放");
            }

            var control = NETClient.PlayBackControl(playInfo.PlayHandle, PlayBackType.Stop);
            return new KeyValuePair<bool, object>(control, control ? "停止成功" : "停止失败");
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        public async Task<KeyValuePair<bool, object>> ResumePlayback(string ipAddress, int channelId) {
            await Task.Yield();

            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }
            var playInfo = dev.DevPlayInfos.FirstOrDefault(f => f.PlayChannelId.Equals(channelId));
            if (playInfo is null || playInfo.PlaybackMode != PlaybackMode.Recording) {
                return new KeyValuePair<bool, object>(false, "还未开启回放");
            }
            NETClient.PlayBackControl(playInfo.PlayHandle, PlayBackType.Normal);
            var control = NETClient.PlayBackControl(playInfo.PlayHandle, PlayBackType.Play);

            return new KeyValuePair<bool, object>(control, control ? "恢复成功" : "恢复失败");
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> PausePlayback(string ipAddress, int channelId) {
            await Task.Yield();

            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }
            var playInfo = dev.DevPlayInfos.FirstOrDefault(f => f.PlayChannelId.Equals(channelId));
            if (playInfo is null || playInfo.PlaybackMode != PlaybackMode.Recording) {
                return new KeyValuePair<bool, object>(false, "还未开启回放");
            }

            var control = NETClient.PlayBackControl(playInfo.PlayHandle, PlayBackType.Pause);

            return new KeyValuePair<bool, object>(control, control ? "暂停成功" : "暂停失败");
        }

        /// <summary>
        /// 快进
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> FastForward(string ipAddress, int channelId, FastForwardSpeed speed) {
            await Task.Yield();

            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }
            var playInfo = dev.DevPlayInfos.FirstOrDefault(f => f.PlayChannelId.Equals(channelId));
            if (playInfo is null || playInfo.PlaybackMode != PlaybackMode.Recording) {
                return new KeyValuePair<bool, object>(false, "还未开启回放");
            }
            switch (speed) {
                case FastForwardSpeed.X2:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.FAST_2);
                    break;

                case FastForwardSpeed.X4:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.FAST_4);
                    break;

                case FastForwardSpeed.X8:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.FAST_8);
                    break;

                case FastForwardSpeed.X16:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.FAST_16);
                    break;

                case FastForwardSpeed.Normal:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.NORMAL);
                    break;
            }
            /*var control = NETClient.PlayBackControl(playInfo.PlayHandle, PlayBackType.Fast);
            return new KeyValuePair<bool, object>(control, control ? "快进成功" : "快进失败");*/
            return new KeyValuePair<bool, object>(true, string.Empty);
        }

        /// <summary>
        /// 慢放
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> Slow(string ipAddress, int channelId, SlowSpeed speed) {
            await Task.Yield();

            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }
            var playInfo = dev.DevPlayInfos.FirstOrDefault(f => f.PlayChannelId.Equals(channelId));
            if (playInfo is null || playInfo.PlaybackMode != PlaybackMode.Recording) {
                return new KeyValuePair<bool, object>(false, "还未开启回放");
            }

            var control = NETClient.PlayBackControl(playInfo.PlayHandle, PlayBackType.Slow);
            switch (speed) {
                case SlowSpeed.X2:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.SLOW_2);
                    break;

                case SlowSpeed.X4:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.SLOW_4);
                    break;

                case SlowSpeed.X8:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.SLOW_8);
                    break;

                case SlowSpeed.X16:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.SLOW_16);
                    break;

                case SlowSpeed.Normal:
                    NETClient.SetPlayBackSpeed(playInfo.PlayHandle, EM_PLAY_BACK_SPEED.NORMAL);
                    break;
            }
            return new KeyValuePair<bool, object>(control, control ? "慢放成功" : "慢放失败");
        }

        /// <summary>
        /// 下载录像
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="channelId"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <param name="videoStreamType"></param>
        /// <param name="savePath"></param>
        /// <param name="downLoadProgressCallBack"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<bool, object>> DownloadRecording(string ipAddress, int channelId, DateTime startDateTime,
            DateTime endDateTime, int videoStreamType, string savePath, Func<DownLoadProgressInfo, Task>? downLoadProgressCallBack = null) {
            await Task.Yield();
            var b = _loginDev.TryGetValue(ipAddress, out var dev);
            if (!b || dev is null) {
                return new KeyValuePair<bool, object>(false, "设备未登录");
            }

            var playInfo = _loginDev.Values
                .SelectMany(nvrDevInfo => nvrDevInfo.DevPlayInfos) // 扁平化 DevPlayInfos 列表
                .FirstOrDefault(devPlayInfo =>
                    devPlayInfo.PlayChannelId == channelId);
            if (playInfo is null) {
                return new KeyValuePair<bool, object>(false, "通道未打开");
            }
            if (downLoadProgressCallBack is not null) {
                playInfo.DownLoadProgressCallBack = downLoadProgressCallBack;
            }
            //暂时不拦截
            var downloadByTime = NETClient.DownloadByTime(dev.LogInHandle, channelId, EM_QUERY_RECORD_TYPE.ALL,
                startDateTime, endDateTime, savePath, async (handle, size, loadSize, index, recordfileinfo, user) => {
                    var info = _loginDev.Values
                        .SelectMany(nvrDevInfo => nvrDevInfo.DevPlayInfos) // 扁平化 DevPlayInfos 列表
                        .FirstOrDefault(devPlayInfo =>
                            devPlayInfo.PlayChannelId == user);
                    // 同步调用回调函数
                    info?.DownLoadProgressCallBack?.Invoke(new DownLoadProgressInfo() {
                        LoadSize = (int)loadSize,
                        Index = index,
                        TotalSize = (int)size,
                        IsDownloadComplete = (int)loadSize == -1 || loadSize >= size,
                        IsDownloadError = (int)loadSize == -2,
                        RecordFileInfo = recordfileinfo
                    });
                },
                channelId, null, IntPtr.Zero, IntPtr.Zero);
            if (downloadByTime == IntPtr.Zero) {
                return new KeyValuePair<bool, object>(false, NETClient.GetLastError());
            }
            return new KeyValuePair<bool, object>(true, "开启下载成功");
        }

        /// <summary>
        /// 转码
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>

        public async Task<KeyValuePair<bool, object>> ConvertDavToMp4(string inputFilePath, string outputFilePath, Func<int, int, bool> progressCallback) {
            await Task.Yield();
            var ffmpegPath = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg\\bin\\ffmpeg.exe";
            if (File.Exists(ffmpegPath)) {
                try {
                    // 设置 ProcessStartInfo
                    var startInfo = new ProcessStartInfo {
                        FileName = ffmpegPath,
                        Arguments = $"-f dhav -i {inputFilePath} -an -c:v copy {outputFilePath}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = new Process { StartInfo = startInfo }) {
                        var totalFileSize = new FileInfo(inputFilePath).Length;

                        // 异步读取错误输出
                        process.ErrorDataReceived += (sender, args) => {
                            if (args.Data != null) {
                                // 解析 FFmpeg 的输出信息
                                ParseProgress(args.Data, totalFileSize, progressCallback);
                            }
                        };

                        // 可选：异步读取标准输出
                        process.OutputDataReceived += (sender, args) => {
                            Console.WriteLine(args.Data); // 输出到控制台（或根据需要处理）
                        };

                        process.Start();  // 启动进程
                        process.BeginErrorReadLine();  // 开始异步读取错误输出
                        process.BeginOutputReadLine(); // 开始异步读取标准输出
                        process.StandardInput.Close(); // 关闭标准输入，避免挂起

                        await process.WaitForExitAsync();  // 异步等待进程结束

                        if (process.ExitCode != 0) {
                            var error = await process.StandardError.ReadToEndAsync();
                            throw new Exception($"FFmpeg failed with error: {error}");
                        }
                    }
                    return new KeyValuePair<bool, object>(true, "转码启动成功");
                }
                catch (Exception e) {
                    return new KeyValuePair<bool, object>(false, e.Message);
                }
            }
            else {
                return new KeyValuePair<bool, object>(false, "ffmpeg文件不存在");
            }
        }

        private bool ParseProgress(string output, long totalFileSize, Func<int, int, bool>? progressCallback) {
            var match = Regex.Match(output, @"size=\s*(\d+)kB");
            if (match.Success) {
                var processedSize = long.Parse(match.Groups[1].Value) * 1024; // 转换为字节数
                var currentProgress = (int)processedSize;
                var totalProgress = (int)totalFileSize;

                // 回调进度
                if (progressCallback != null) {
                    bool continueProcessing = progressCallback(currentProgress, totalProgress);
                    // 如果当前进度达到或超过总大小，则认为转换完成
                    if (currentProgress >= totalProgress) {
                        // 在转换完成时回调，通知完成状态
                        progressCallback(totalProgress, totalProgress);
                    }
                    return continueProcessing;
                }
            }
            return true;
        }

        public async Task<KeyValuePair<bool, object>> MergeVideos(string[] inputFiles, string outputFile, double totalDuration, Action<double> progressCallback, Func<bool> cancelCallback) {
            if (inputFiles.Length is < 2 and <= 9) {
                return new KeyValuePair<bool, object>(false, "合并的视频需要大于1并且小于10");
            }

            foreach (var file in inputFiles) {
                var exists = File.Exists(file);
                if (!exists) {
                    return new KeyValuePair<bool, object>(false, $"{file}--文件不存在");
                }
            }

            try {
                var ffmpegPath = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg\\bin\\ffmpeg.exe";
                if (File.Exists(ffmpegPath)) {
                    var arguments = BuildFfmpegArguments(inputFiles, outputFile, totalDuration);

                    var process = new Process {
                        StartInfo = new ProcessStartInfo {
                            FileName = ffmpegPath,
                            Arguments = arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                    process.ErrorDataReceived += (sender, e) => {
                        if (e.Data != null) {
                            if (e.Data.Contains("Duration") && inputFiles.Length == 2) {
                                totalDuration = ParseDurationFromOutput(e.Data);
                            }
                            /*Console.WriteLine(e.Data);*/
                            ParseProgress(e.Data, progressCallback, cancelCallback, process, totalDuration);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();
                }
                else {
                    return new KeyValuePair<bool, object>(false, "ffmpeg文件不存在");
                }
            }
            catch (Exception e) {
                return new KeyValuePair<bool, object>(false, e.Message);
            }

            return new KeyValuePair<bool, object>(true, "保存成功");
        }

        private string BuildFfmpegArguments(string[] inputFiles, string outputFile, double totalDuration) {
            var videoCount = inputFiles.Length;
            // 添加输入文件
            var arguments = inputFiles.Select(file => $"-i \"{file}\"").ToList();
            arguments.Insert(0, "-y");

            List<string> videoIndexList = new();

            // 生成 scale 和 fps 过滤器
            var scaleFilters = new List<string>();
            for (var i = 0; i < videoCount; i++) {
                scaleFilters.Add($"[{i}:v]scale=1920:1080,fps=30[v{i}];");
                videoIndexList.Add($"[v{i}]");
            }

            // 生成占位符
            var blankPlaceholder = "color=black:size=1920x1080[blank]; ";

            var isUseBlankPlaceHolder = scaleFilters.Count is 2 or 4 or 6 or 9;

            while (!isUseBlankPlaceHolder) {
                scaleFilters.Add(blankPlaceholder);
                videoIndexList.Add($"[blank]");
                isUseBlankPlaceHolder = scaleFilters.Count is 2 or 4 or 6 or 9;
            }

            // 生成布局过滤器
            var layoutFilters = new List<string>();
            switch (videoCount) {
                case 2:
                    layoutFilters.Add($"{string.Join("", videoIndexList)}hstack=inputs=2[vout]");
                    break;

                case 3:
                case 4:
                    layoutFilters.Add($"{string.Join("", videoIndexList.Take(2))}hstack=inputs=2[row1]; {string.Join("", videoIndexList.Skip(2).Take(2))}hstack=inputs=2[row2]; [row1][row2]vstack=inputs=2[vout]");
                    break;

                case 5:
                case 6:
                    layoutFilters.Add($"{string.Join("", videoIndexList.Take(2))}hstack=inputs=2[row1]; {string.Join("", videoIndexList.Skip(2).Take(2))}hstack=inputs=2[row2]; {string.Join("", videoIndexList.Skip(4).Take(2))}hstack=inputs=2[row3]; [row1][row2]vstack=inputs=2[rowFinal]; [rowFinal][row3]vstack=inputs=2[vout]");
                    break;

                case 7:
                case 8:
                case 9:
                    layoutFilters.Add($"{string.Join("", videoIndexList.Take(3))}hstack=inputs=3[row1]; {string.Join("", videoIndexList.Skip(3).Take(3))}hstack=inputs=3[row2]; {string.Join("", videoIndexList.Skip(6).Take(3))}hstack=inputs=3[row3]; [row1][row2]vstack=inputs=2[rowFinal]; [rowFinal][row3]vstack=inputs=2[vout]");
                    break;

                default:
                    throw new ArgumentOutOfRangeException("视频数量超出范围，支持的最大数量为 9。");
            }

            // 合并过滤器
            var filterComplex = string.Join(" ", scaleFilters) + string.Join(" ", layoutFilters);

            // 添加 filter_complex 和输出参数
            arguments.Add($"-filter_complex \"{filterComplex}\"");
            arguments.Add("-map [vout]");
            arguments.Add($"-c:v libx264 -preset ultrafast -crf 18 -r 30 -t {totalDuration} -shortest -pix_fmt yuv420p");
            arguments.Add($"\"{outputFile}\"");

            return string.Join(" ", arguments);
        }

        private void ParseProgress(string data, Action<double> progressCallback, Func<bool> cancelCallback, Process process, double totalDuration) {
            var match = Regex.Match(data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (match.Success) {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var milliseconds = int.Parse(match.Groups[4].Value);

                var currentSeconds = hours * 3600 + minutes * 60 + seconds + milliseconds / 100.0;

                // 使用传入的 totalDuration 进行进度计算
                var progress = currentSeconds / totalDuration * 100;

                progressCallback(progress);

                // 检查是否需要取消
                if (cancelCallback()) {
                    process.Kill();
                    Console.WriteLine("合并已取消");
                }
            }
        }

        private double ParseDurationFromOutput(string ffmpegOutput) {
            var durationRegex = new Regex(@"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            var match = durationRegex.Match(ffmpegOutput);

            if (match.Success) {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var milliseconds = int.Parse(match.Groups[4].Value);

                return hours * 3600 + minutes * 60 + seconds + milliseconds / 100.0;
            }

            return 0;
        }

        public enum FastForwardSpeed {
            X2 = 0,
            X4 = 1,
            X8 = 2,
            X16 = 3,
            Normal
        }

        public enum SlowSpeed {
            X2 = 0,
            X4 = 1,
            X8 = 2,
            X16 = 3,
            Normal
        }
    }
}