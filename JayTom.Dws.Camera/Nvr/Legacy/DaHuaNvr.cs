using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace JayTom.Dws.Camera.Nvr.Legacy {

    public class DaHuaNvr : INvrManager {
        private static bool _isLoad;
        private static readonly System.Threading.Lock SdkInitializationLock = new();
        private static readonly ConcurrentDictionary<IntPtr, WeakReference<DaHuaNvr>> LoginOwners = new();
        private readonly SemaphoreSlim _loginSlim = new(1, 1);
        private readonly SemaphoreSlim _remotePreviewSlim = new(1, 1);
        private readonly fRealDataCallBackEx2 _mRealDataCallBackEx2;

        public event EventHandler<RealTimePreviewEventArgs>? RealTimePreviewCallback;

        public event EventHandler<RemotePlaybackEventArgs>? RemotePlaybackCallback;

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressCallback;

        public event EventHandler<RemotePlaybackProgressEventArgs>? RemotePlaybackProgressCallback;

        public event EventHandler<DeviceDisconnectedEventArgs>? DeviceDisconnected;

        public event EventHandler<DeviceReconnectedEventArgs>? DeviceReconnected;

        private NET_DEVICEINFO_Ex _mDeviceInfo;
        private IntPtr _loginHandle = IntPtr.Zero;
        private IntPtr _realPlay = IntPtr.Zero;

        public DaHuaNvr() {
            EnsureSdkInitialized();
            _mRealDataCallBackEx2 = OnRealtimeData;
        }

        public async Task<KeyValuePair<bool, string>> StartRemotePreview(int channel, string tempFileName, CancellationToken token = default) {
            var lockTaken = false;
            try {
                await _remotePreviewSlim.WaitAsync(token);
                lockTaken = true;
                if (_loginHandle == IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "设备未登录!");
                }
                if (_realPlay != IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "请先停止预览!");
                }

                _realPlay = NETClient.RealPlay(_loginHandle, channel, IntPtr.Zero, EM_RealPlayType.Realplay);
                if (IntPtr.Zero == _realPlay) {
                    return new KeyValuePair<bool, string>(false, NETClient.GetLastError());
                }

                var realDataCallBack = NETClient.SetRealDataCallBack(_realPlay, _mRealDataCallBackEx2, IntPtr.Zero, EM_REALDATA_FLAG.DATA_WITH_FRAME_INFO | EM_REALDATA_FLAG.PCM_AUDIO_DATA | EM_REALDATA_FLAG.RAW_DATA | EM_REALDATA_FLAG.YUV_DATA);
                return new KeyValuePair<bool, string>(realDataCallBack,
                    realDataCallBack ? string.Empty : NETClient.GetLastError());
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                if (lockTaken) {
                    _remotePreviewSlim.Release();
                }
            }
        }

        public async Task<KeyValuePair<bool, string>> StopRemotePreview(int channel, CancellationToken token = default) {
            var lockTaken = false;
            try {
                await _remotePreviewSlim.WaitAsync(token);
                lockTaken = true;
                if (_loginHandle == IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "设备未登录!");
                }
                if (_realPlay == IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "请先开始预览!");
                }

                var stopRealPlay = NETClient.StopRealPlay(_realPlay);
                if (stopRealPlay) {
                    _realPlay = IntPtr.Zero;
                }
                return new KeyValuePair<bool, string>(stopRealPlay,
                    stopRealPlay ? string.Empty : NETClient.GetLastError());
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                return new KeyValuePair<bool, string>(false, e.Message);
            }
            finally {
                if (lockTaken) {
                    _remotePreviewSlim.Release();
                }
            }
        }

        public Task<KeyValuePair<bool, string>> StartRemotePlayback(int channel, DateTime startTime, DateTime endTime, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> StopRemotePlayback(int channel, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> PauseRemotePlayback(int channel, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, List<int>>> EnumerateChannels() {
            if (IntPtr.Zero == _loginHandle) {
                return Task.FromResult(new KeyValuePair<bool, List<int>>(false, new List<int>()));
            }
            return Task.FromResult(new KeyValuePair<bool, List<int>>(true,
                [.. Enumerable.Range(0, _mDeviceInfo.nChanNum).Select(s => s + 1)]));
        }

        public async Task<KeyValuePair<bool, string>> Login(string ip, int port, string username, string password, CancellationToken token = default) {
            var lockTaken = false;
            try {
                await _loginSlim.WaitAsync(token);
                lockTaken = true;
                if (_loginHandle != IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(true, $"已登录,Id:{_loginHandle:X}");
                }

                _loginHandle = NETClient.LoginWithHighLevelSecurity(ip, (ushort)port, username, password, EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref _mDeviceInfo);
                if (IntPtr.Zero == _loginHandle) {
                    return new KeyValuePair<bool, string>(false, NETClient.GetLastError());
                }
                LoginOwners[_loginHandle] = new WeakReference<DaHuaNvr>(this);
                return new KeyValuePair<bool, string>(true, $"登录成功,Id:{_loginHandle:X}");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, $"{e.Message}");
            }
            finally {
                if (lockTaken) {
                    _loginSlim.Release();
                }
            }
        }

        public async Task<KeyValuePair<bool, string>> Logout(CancellationToken token = default) {
            var lockTaken = false;
            try {
                await _loginSlim.WaitAsync(token);
                lockTaken = true;
                if (_loginHandle == IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(true, string.Empty);
                }

                if (_realPlay != IntPtr.Zero) {
                    NETClient.StopRealPlay(_realPlay);
                    _realPlay = IntPtr.Zero;
                }

                var loginHandle = _loginHandle;
                if (!NETClient.Logout(loginHandle)) {
                    return new KeyValuePair<bool, string>(false, NETClient.GetLastError());
                }

                LoginOwners.TryRemove(loginHandle, out _);
                _loginHandle = IntPtr.Zero;
                return new KeyValuePair<bool, string>(true, string.Empty);
            }
            catch (Exception exception) {
                return new KeyValuePair<bool, string>(false, exception.Message);
            }
            finally {
                if (lockTaken) {
                    _loginSlim.Release();
                }
            }
        }

        public Task<KeyValuePair<bool, string>> DownloadPlaybackVideo(int channel, DateTime startTime, DateTime endTime, string savePath, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        private static void EnsureSdkInitialized() {
            lock (SdkInitializationLock) {
                if (_isLoad) {
                    return;
                }

                if (!NETClient.Init(HandleDisconnected, IntPtr.Zero, null)) {
                    throw new InvalidOperationException($"大华 NVR SDK 初始化失败：{NETClient.GetLastError()}");
                }
                NETClient.SetAutoReconnect(HandleReconnected, IntPtr.Zero);
                _isLoad = true;
            }
        }

        private static void HandleDisconnected(IntPtr loginHandle, IntPtr _, int __, IntPtr ___) {
            if (LoginOwners.TryGetValue(loginHandle, out var owner) && owner.TryGetTarget(out var nvr)) {
                nvr.OnDeviceDisconnected(new DeviceDisconnectedEventArgs {
                    LoginHandle = loginHandle,
                    Message = "设备断开连接"
                });
            }
        }

        private static void HandleReconnected(IntPtr loginHandle, IntPtr _, int __, IntPtr ___) {
            if (LoginOwners.TryGetValue(loginHandle, out var owner) && owner.TryGetTarget(out var nvr)) {
                nvr.OnDeviceReconnected(new DeviceReconnectedEventArgs {
                    LoginHandle = loginHandle,
                    Message = "设备已重连"
                });
            }
        }

        private void OnRealtimeData(IntPtr handle, uint type, IntPtr buffer, uint size, IntPtr _, IntPtr __) {
            if (size == 0 || buffer == IntPtr.Zero || RealTimePreviewCallback is null) {
                return;
            }

            var data = GC.AllocateUninitializedArray<byte>(checked((int)size));
            Marshal.Copy(buffer, data, 0, data.Length);
            OnRealTimePreviewCallback(new RealTimePreviewEventArgs {
                YuvData = data,
                Data = new MemoryStream(data, writable: false)
            });
        }

        protected virtual void OnDeviceDisconnected(DeviceDisconnectedEventArgs e) {
            DeviceDisconnected?.Invoke(this, e);
        }

        protected virtual void OnDeviceReconnected(DeviceReconnectedEventArgs e) {
            DeviceReconnected?.Invoke(this, e);
        }

        protected virtual void OnRealTimePreviewCallback(RealTimePreviewEventArgs e) {
            RealTimePreviewCallback?.Invoke(this, e);
        }
    }
}
