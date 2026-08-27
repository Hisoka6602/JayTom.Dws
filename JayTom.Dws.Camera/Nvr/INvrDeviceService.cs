using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera.Nvr {

    /// <summary>
    /// NVR设备管理
    /// </summary>
    public interface INvrDeviceService : IDisposable {

        /// <summary>
        /// 实时预览回调
        /// </summary>
        event EventHandler<NvrDeviceRealtimeImageEventArgs> RealTimePreviewCallback;

        /// <summary>
        /// 远程回放回调
        /// </summary>
        event EventHandler<RemotePlaybackEventArgs> RemotePlaybackCallback;

        /// <summary>
        /// 下载进度回调
        /// </summary>
        event EventHandler<float> DownloadProgressCallback;

        /// <summary>
        /// 远程回放进度回调
        /// </summary>
        event EventHandler<float> RemotePlaybackProgressCallback;

        /// <summary>
        /// 设备断开
        /// </summary>
        event EventHandler<NvrDeviceDisconnectedEventArgs> DeviceDisconnected;

        /// <summary>
        /// 设备连接
        /// </summary>
        event EventHandler<NvrDeviceConnectedEventArgs> DeviceConnected;

        /// <summary>
        /// 设备重连
        /// </summary>
        event EventHandler<NvrDeviceReconnectedEventArgs> DeviceReconnected;

        /// <summary>
        /// 设备异常事件
        /// </summary>
        event EventHandler<Exception> DeviceExcepted;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Initialize(CancellationToken cancellationToken = default);

        /// <summary>
        /// 开启实时预览
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber">通道ID</param>
        Task<KeyValuePair<bool, string>> StartRealTimePreview(string serialNo, int channelNumber);

        /// <summary>
        /// 关闭实时预览
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber">通道ID</param>
        Task<KeyValuePair<bool, string>> StopRealTimePreview(string serialNo, int channelNumber);

        /// <summary>
        /// 开始远程回放
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber">通道ID</param>
        /// <param name="startTime">回放开始时间</param>
        /// <param name="endTime">回放结束时间</param>
        Task<KeyValuePair<bool, string>> StartRemotePlayback(string serialNo, int channelNumber, DateTime startTime, DateTime endTime);

        /// <summary>
        /// 停止远程回放
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber">通道ID</param>
        Task<KeyValuePair<bool, string>> StopRemotePlayback(string serialNo, int channelNumber);

        /// <summary>
        /// 暂停远程回放
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber">通道ID</param>
        Task<KeyValuePair<bool, string>> PauseRemotePlayback(string serialNo, int channelNumber);

        /// <summary>
        /// 枚举设备
        /// </summary>
        /// <returns>返回设备列表</returns>
        Task<List<NvrDeviceInfo>?> EnumerateDevices();

        /// <summary>
        /// 添加水印
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber"></param>
        /// <param name="packAgeTimestamp"></param>
        /// <param name="content"></param>
        /// <param name="config"></param>
        void AddWatermark(string serialNo, int channelNumber, long packAgeTimestamp,
            string content, SecurityCameraWatermarkConfig config);

        /// <summary>
        /// 清空水印
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber">通道ID</param>
        void ClearWatermark(string serialNo, int channelNumber);

        /// <summary>
        /// 登录设备
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="userName"></param>
        /// <param name="passWord"></param>
        /// <param name="playChannelNumber"></param>
        /// <returns>是否登录成功</returns>
        Task<KeyValuePair<bool, string>> Login(string serialNo, string userName, string passWord, int playChannelNumber = 0);

        /// <summary>
        /// 登出设备
        /// </summary>
        /// <param name="serialNo"></param>
        Task<KeyValuePair<bool, string>> Logout(string serialNo);

        /// <summary>
        /// 下载回放录像
        /// </summary>
        /// <param name="serialNo"></param>
        /// <param name="channelNumber">通道ID</param>
        /// <param name="startTime">录像开始时间</param>
        /// <param name="endTime">录像结束时间</param>
        /// <param name="savePath">保存路径</param>
        void DownloadPlaybackVideo(string serialNo, int channelNumber, DateTime startTime, DateTime endTime, string savePath);
    }

    public class NvrDeviceInfo {

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 设备通道数量
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// 登录用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 登录密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 设备IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 设备端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 登录句柄，用于标识设备的登录状态
        /// </summary>
        public nint LoginHandle { get; set; }
    }

    public class RemotePlaybackEventArgs : EventArgs {
        public Bitmap? RealtimeImage { get; set; }
        public int PlaybackSpeed { get; set; }

        /// <summary>
        /// 通道Id
        /// </summary>
        public int ChannelNumber { get; set; }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;
    }

    public class NvrDeviceRealtimeImageEventArgs : EventArgs {

        /// <summary>
        /// 图像帧时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 缩略图
        /// </summary>
        public Bitmap? ThumbImage { get; set; }

        /// <summary>
        /// 通道Id
        /// </summary>
        public int ChannelNumber { get; set; }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;
    }

    public class NvrDeviceDisconnectedEventArgs : EventArgs {
        public DateTime DisConnectedTime { get; set; }
        public NvrDeviceInfo? NvrDeviceInfo { get; set; }
    }

    public class NvrDeviceConnectedEventArgs : EventArgs {
        public DateTime ConnectedTime { get; set; }
        public NvrDeviceInfo? NvrDeviceInfo { get; set; }
    }

    public class NvrDeviceReconnectedEventArgs : EventArgs {
        public DateTime ReConnectedTime { get; set; }
        public NvrDeviceInfo? NvrDeviceInfo { get; set; }
    }
}
