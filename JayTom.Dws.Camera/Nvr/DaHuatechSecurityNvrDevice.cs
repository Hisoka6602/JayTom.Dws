using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

namespace JayTom.Dws.Camera.Nvr {

    public class DaHuatechSecurityNvrDevice : INvrDeviceService {
        private BaseDaHuatech? _baseDaHuatech;
        private readonly ConcurrentDictionary<string, NvrDeviceInfo> _devInfo = new();

        public DaHuatechSecurityNvrDevice() {
        }

        public event EventHandler<NvrDeviceRealtimeImageEventArgs>? RealTimePreviewCallback;

        public event EventHandler<RemotePlaybackEventArgs>? RemotePlaybackCallback;

        public event EventHandler<float>? DownloadProgressCallback;

        public event EventHandler<float>? RemotePlaybackProgressCallback;

        public event EventHandler<NvrDeviceDisconnectedEventArgs>? DeviceDisconnected;

        public event EventHandler<NvrDeviceConnectedEventArgs>? DeviceConnected;

        public event EventHandler<NvrDeviceReconnectedEventArgs>? DeviceReconnected;

        public event EventHandler<Exception>? DeviceExcepted;

        public Task<KeyValuePair<bool, string>> Initialize(object param) {
            _baseDaHuatech = BaseDaHuatech.CreateInstance();
            if (_baseDaHuatech is null) {
                return Task.FromResult(new KeyValuePair<bool, string>(false, "创建NVR对象失败,请检查是否使用了对应的SDK和文件是否齐全"));
            }
            //定义各种回调
            //_baseDaHuatech.StartRealtimePlay()
            return Task.FromResult(new KeyValuePair<bool, string>(true, "初始化成功"));
        }

        public Task<KeyValuePair<bool, string>> StartRealTimePreview(string serialNo, int channelId) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> StopRealTimePreview(string serialNo, int channelId) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> StartRemotePlayback(string serialNo, int channelId, DateTime startTime, DateTime endTime) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> StopRemotePlayback(string serialNo, int channelId) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> PauseRemotePlayback(string serialNo, int channelId) {
            throw new NotImplementedException();
        }

        public Task<List<NvrDeviceInfo>?> EnumerateDevices() {
            throw new NotImplementedException();
        }

        public void AddWatermark(string serialNo, int channelId, long packAgeTimestamp, string content,
            SecurityCameraWatermarkConfig config) {
            throw new NotImplementedException();
        }

        public void ClearWatermark(string serialNo, int channelId) {
            throw new NotImplementedException();
        }

        public async Task<KeyValuePair<bool, string>> Login(string serialNo, string userName, string passWord, int playChannelId = 0) {
            if (_baseDaHuatech is not null) {
                var (key, value) = await _baseDaHuatech.LogIn(serialNo, userName, passWord);
                if (!key) {
                    OnDeviceExcepted(new Exception(value));
                }
                return new KeyValuePair<bool, string>(key, value);
            }
            return new KeyValuePair<bool, string>(false, "未初始化");
        }

        public async Task<KeyValuePair<bool, string>> Logout(string serialNo) {
            if (_baseDaHuatech is not null) {
                var (key, value) = await _baseDaHuatech.LogOut(serialNo);
                if (!key) {
                    OnDeviceExcepted(new Exception(value));
                }
                return new KeyValuePair<bool, string>(key, value);
            }
            return new KeyValuePair<bool, string>(false, "未初始化");
        }

        public void DownloadPlaybackVideo(string serialNo, int channelId, DateTime startTime, DateTime endTime, string savePath) {
            //throw new NotImplementedException();
        }

        public void Dispose() {
            foreach (var info in _devInfo) {
                if (info.Value is not null && _baseDaHuatech is not null) {
                    //停止实时预览
                    //停止下载
                    //停止回放
                    //登出
                    _baseDaHuatech.LogOut(info.Key).GetAwaiter().GetResult();
                }
            }
        }

        protected virtual void OnDeviceExcepted(Exception e) {
            DeviceExcepted?.Invoke(this, e);
        }
    }
}
