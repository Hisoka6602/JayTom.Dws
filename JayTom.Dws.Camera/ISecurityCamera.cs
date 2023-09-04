using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera {

    public interface ISecurityCamera : ICamera {

        /// <summary>
        /// 相机连接参数
        /// </summary>
        string CameraConnectionParameters { get; set; }

        /// <summary>
        /// 拍照延迟
        /// </summary>
        public int TakePhotoDelay { get; set; }

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
        /// 照片回调
        /// </summary>
        public event EventHandler<PhotoTakenEventArgs> PhotoTaken;

        /// <summary>
        /// 拍照
        /// </summary>
        /// <returns></returns>
        Task TakePhotoAsync(string barcode, long barcodeTimestamp, CancellationToken cancellation = default);

        /// <summary>
        /// 拍照
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="barcodeTimestamp"></param>
        /// <param name="delay"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task TakePhotoAsync(string barcode, long barcodeTimestamp, TimeSpan delay, CancellationToken cancellation = default);
    }

    public class RealPreviewEventArgs {
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