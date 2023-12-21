using System;
using NetSDKCS;
using System.Linq;
using System.Text;
using FFmpeg.AutoGen;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JayTom.Dws.Nvr.Nvr {

    public class DaHuaNvr : INvrManager {
        private static bool _isLoad;
        private SemaphoreSlim _loginSlim = new(1);
        private SemaphoreSlim _remotePreviewSlim = new(1);
        private SemaphoreSlim _dataCallBack = new(1);
        private static fRealDataCallBackEx2 _mRealDataCallBackEx2;

        public event EventHandler<RealTimePreviewEventArgs>? RealTimePreviewCallback;

        public event EventHandler<RemotePlaybackEventArgs>? RemotePlaybackCallback;

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressCallback;

        public event EventHandler<RemotePlaybackProgressEventArgs>? RemotePlaybackProgressCallback;

        public event EventHandler<DeviceDisconnectedEventArgs>? DeviceDisconnected;

        public event EventHandler<DeviceReconnectedEventArgs>? DeviceReconnected;

        private NET_DEVICEINFO_Ex _mDeviceInfo;
        private static IntPtr _loginId = IntPtr.Zero;
        private IntPtr _realPlay = IntPtr.Zero;
        private static string _tempFileName = string.Empty;

        public DaHuaNvr() {
            if (!_isLoad) {
                _isLoad = true;
                try {
                    //判断文件是否存在
                    //否则解压文件
                    var destinationDir = AppDomain.CurrentDomain.BaseDirectory;
                    var files = Directory.GetFiles($"{destinationDir}Cameras\\SecurityCamera\\DaHuatech\\Dll")?.ToList();
                    if (files?.Any() == true) {
                        foreach (var s in files) {
                            if (!File.Exists($"{destinationDir}\\{new FileInfo(s).Name}")) {
                                File.Copy(s, $"{destinationDir}\\{new FileInfo(s).Name}", true);
                            }
                        }
                    }

                    NETClient.Init((loginId, chDvrip, dvrPort, dwUser) => {
                        OnDeviceDisconnected(new DeviceDisconnectedEventArgs() {
                            LoginId = loginId,
                            Message = "设备断开连接"
                        });
                    }, IntPtr.Zero, null);
                    NETClient.SetAutoReconnect((loginId, chDvrip, dvrPort, dwUser) => {
                        OnDeviceReconnected(new DeviceReconnectedEventArgs() {
                            LoginId = loginId,
                            Message = "设备重连中.."
                        });
                    }, IntPtr.Zero);
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }

                string videoStreamFilePath = _tempFileName; // 视频流保存的文件路径
                _mRealDataCallBackEx2 = delegate (IntPtr handle, uint type, IntPtr buffer, uint size, IntPtr nint,
                    IntPtr user) {
                        if (type == 0) {
                            try {
                                /*//var videoMemoryStream = new MemoryStream();
                                var data = new byte[size];
                                Marshal.Copy(buffer, data, 0, (int)size);
                                /*videoMemoryStream.Write(data, 0, data.Length);
                                OnRealTimePreviewCallback(new RealTimePreviewEventArgs() {
                                    YuvData = data,
                                    Data = videoMemoryStream
                                });
                                Debug.WriteLine($"{size}-{buffer:X}");#1#
                                File.WriteAllBytes(videoStreamFilePath, data);*/

                                /*if (File.Exists(_tempFileName)) {
                                    using (var fileStream = new FileStream(_tempFileName, FileMode.Append, FileAccess.Write)) {
                                        var data = new byte[size];
                                        Marshal.Copy(buffer, data, 0, (int)size);

                                        // 将数据写入文件末尾
                                        fileStream.Write(data, 0, data.Length);
                                        var videoMemoryStream = new MemoryStream();
                                        videoMemoryStream.Write(data, 0, data.Length);
                                        OnRealTimePreviewCallback(new RealTimePreviewEventArgs() {
                                            YuvData = data,
                                            Data = videoMemoryStream
                                        });
                                    }
                                }*/
                            }
                            catch (Exception e) {
                                Console.WriteLine(e);
                            }
                        }
                        else {
                            Debug.WriteLine(type);
                        }
                    };

                var callbackHandle = GCHandle.Alloc(_mRealDataCallBackEx2);
            }
        }

        public async Task<KeyValuePair<bool, string>> StartRemotePreview(int channel, string tempFileName, CancellationToken token = default) {
            try {
                await _remotePreviewSlim.WaitAsync(token);
                if (_loginId == IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "设备未登录!");
                }
                if (_realPlay != IntPtr.Zero) {
                    return new KeyValuePair<bool, string>(false, "请先停止预览!");
                }

                if (!File.Exists(tempFileName)) {
                    File.Create(tempFileName);
                }
                _tempFileName = tempFileName;
                _realPlay = NETClient.RealPlay(_loginId, channel, IntPtr.Zero, EM_RealPlayType.Realplay);
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
                _remotePreviewSlim.Release();
            }
        }

        public async Task<KeyValuePair<bool, string>> StopRemotePreview(int channel, CancellationToken token = default) {
            try {
                await _remotePreviewSlim.WaitAsync(token);
                if (_loginId == IntPtr.Zero) {
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
                _remotePreviewSlim.Release();
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

        public async Task<KeyValuePair<bool, List<int>>> EnumerateChannels() {
            await Task.Yield();
            if (IntPtr.Zero == _loginId) {
                return new KeyValuePair<bool, List<int>>(false, new List<int>());
            }
            return new KeyValuePair<bool, List<int>>(true, Enumerable.Range(0, _mDeviceInfo.nChanNum)?
                .Select(s => s + 1)?.ToList() ?? new List<int>());
        }

        public async Task<KeyValuePair<bool, string>> Login(string ip, int port, string username, string password, CancellationToken token = default) {
            try {
                await _loginSlim.WaitAsync();
                _loginId = NETClient.LoginWithHighLevelSecurity(ip, (ushort)port, username, password, EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref _mDeviceInfo);
                if (IntPtr.Zero == _loginId) {
                    return new KeyValuePair<bool, string>(false, NETClient.GetLastError());
                }
                return new KeyValuePair<bool, string>(true, $"登录成功,Id:{_loginId:X}");
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, $"{e.Message}");
            }
            finally {
                _loginSlim.Release();
            }
        }

        public Task<KeyValuePair<bool, string>> Logout(CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task<KeyValuePair<bool, string>> DownloadPlaybackVideo(int channel, DateTime startTime, DateTime endTime, string savePath, CancellationToken token = default) {
            throw new NotImplementedException();
        }

        private Task<int> RunFFmpegCommand(string arguments) {
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = "path/to/ffmpeg", // 替换为你的FFmpeg可执行文件路径
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (Process process = new Process { StartInfo = psi }) {
                process.Start();
                return (Task<int>)process.WaitForExitAsync();
            }
        }

        protected virtual async void OnDeviceDisconnected(DeviceDisconnectedEventArgs e) {
            await Task.Yield();
            DeviceDisconnected?.Invoke(this, e);
        }

        protected virtual async void OnDeviceReconnected(DeviceReconnectedEventArgs e) {
            await Task.Yield();
            DeviceReconnected?.Invoke(this, e);
        }

        protected virtual async void OnRealTimePreviewCallback(RealTimePreviewEventArgs e) {
            await Task.Yield();
            RealTimePreviewCallback?.Invoke(this, e);
        }
    }

    public static class ProcessExtensions {

        public static Task<int> WaitForExitAsync(this Process process) {
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(process.ExitCode);
            return tcs.Task;
        }
    }
}