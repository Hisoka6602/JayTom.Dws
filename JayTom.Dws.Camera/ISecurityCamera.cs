using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera {

    public interface ISecurityCamera : ICamera {

        /// <summary>
        /// 相机连接参数
        /// </summary>
        string CameraConnectionParameters { get; set; }

        /// <summary>
        /// 实时预览事件
        /// </summary>
        event EventHandler<RealPreviewEventArgs> RealPreview;

        /// <summary>
        /// 保存流
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SaveStream(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 缩放
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> Zoom(double zoomFactor, CancellationToken cancellationToken = default);

        /// <summary>
        /// 云台控制
        /// </summary>
        /// <param name="panAngle"></param>
        /// <param name="tiltAngle"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> ControlPtz(double panAngle, double tiltAngle, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置步长
        /// </summary>
        /// <param name="stepSize"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetStepSize(int stepSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置焦距
        /// </summary>
        /// <param name="focalLength"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetFocalLength(double focalLength, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置光圈
        /// </summary>
        /// <param name="aperture"></param>
        /// <param name="cancellationToken"></param>
        Task<KeyValuePair<bool, string>> SetAperture(double aperture, CancellationToken cancellationToken = default);

        /// <summary>
        /// 开启实时预览
        /// </summary>
        Task<KeyValuePair<bool, string>> StartPreview(CancellationToken cancellationToken = default);

        /// <summary>
        /// 关闭实时预览
        /// </summary>
        void StopPreview(CancellationToken cancellationToken = default);

        /// <summary>
        /// 远程回放实时画面事件
        /// </summary>
        public event EventHandler<RemotePlaybackEventArgs> RemotePlaybackRealtimeImage;

        /// <summary>
        /// 开始远程回放
        /// </summary>
        /// <param name="playbackSpeed"></param>
        public void StartRemotePlayback(int playbackSpeed);

        /// <summary>
        /// 停止远程回放
        /// </summary>
        public void StopRemotePlayback();

        /// <summary>
        /// 暂停远程回放
        /// </summary>
        public void PauseRemotePlayback();
    }

    public class RealPreviewEventArgs {
    }

    public class RemotePlaybackEventArgs : EventArgs {
        public Bitmap? RealtimeImage { get; set; }
        public int PlaybackSpeed { get; set; }
    }

    /// <summary>
    /// 安防相机连接参数
    /// </summary>
    public class SecurityCameraConnectionParameters {

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    public class CameraImageMessageInfo {
        public string Barcode { get; set; } = string.Empty;

        public long BarcodeTimestamp { get; set; }
    }

    public class SecurityCameraInfo : CameraInfo {

        /// <summary>
        /// 相机是否已初始化
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Ip版本
        /// </summary>
        public string IpVersion { get; set; }

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 子网掩码
        /// </summary>
        public string SubnetMask { get; set; }

        /// <summary>
        /// 网关
        /// </summary>
        public string Gateway { get; set; }

        /// <summary>
        /// Mac地址
        /// </summary>
        public string MacAddress { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string DeviceType { get; set; }

        /// <summary>
        /// 详细类型
        /// </summary>
        public string DetailedType { get; set; }

        /// <summary>
        /// Http端口
        /// </summary>
        public int HttpPort { get; set; }
    }
}