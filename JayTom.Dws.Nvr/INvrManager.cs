using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace JayTom.Dws.Nvr {

    public interface INvrManager {

        /// <summary>
        /// 实时预览回调
        /// </summary>
        event EventHandler<RealTimePreviewEventArgs> RealTimePreviewCallback;

        /// <summary>
        /// 远程回放回调
        /// </summary>
        event EventHandler<RemotePlaybackEventArgs> RemotePlaybackCallback;

        /// <summary>
        /// 下载进度回调
        /// </summary>
        event EventHandler<DownloadProgressEventArgs> DownloadProgressCallback;

        /// <summary>
        /// 远程回放进度回调
        /// </summary>
        event EventHandler<RemotePlaybackProgressEventArgs> RemotePlaybackProgressCallback;

        /// <summary>
        /// 设备断开
        /// </summary>
        event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;

        /// <summary>
        /// 设备重连
        /// </summary>
        event EventHandler<DeviceReconnectedEventArgs> DeviceReconnected;

        /// <summary>
        /// 开启远程预览
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="tempFileName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> StartRemotePreview(int channel, string tempFileName, CancellationToken token = default);

        /// <summary>
        /// 关闭远程预览
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> StopRemotePreview(int channel, CancellationToken token = default);

        /// <summary>
        /// 开始远程回放
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> StartRemotePlayback(int channel, DateTime startTime, DateTime endTime, CancellationToken token = default);

        /// <summary>
        /// 停止远程回放
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> StopRemotePlayback(int channel, CancellationToken token = default);

        /// <summary>
        /// 暂停远程回放
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> PauseRemotePlayback(int channel, CancellationToken token = default);

        /// <summary>
        /// 枚举通道
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, List<int>>> EnumerateChannels();

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Login(string ip, int port, string username, string password, CancellationToken token = default);

        /// <summary>
        /// 登出
        /// </summary>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Logout(CancellationToken token = default);

        /// <summary>
        /// 下载回放录像
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="savePath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> DownloadPlaybackVideo(int channel, DateTime startTime, DateTime endTime, string savePath, CancellationToken token = default);
    }

}
