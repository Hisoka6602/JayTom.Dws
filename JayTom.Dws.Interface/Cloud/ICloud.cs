using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Interface.Cloud {

    public interface ICloud {

        /// <summary>
        /// 上传数据
        /// </summary>
        /// <param name="barcode">条码</param>
        /// <param name="scanTime">扫码时间</param>
        /// <param name="weight">重量</param>
        /// <param name="scanNodName">节点</param>
        /// <param name="volumeInfo">体积信息</param>
        /// <param name="imageInfos">图片信息</param>
        /// <param name="ocrInfo">Ocr信息</param>
        /// <param name="uploadApiInfo">Api上传信息</param>
        /// <param name="sortingInfo">分拣信息</param>
        /// <param name="nvrCameraBindingInfo"></param>
        /// <param name="other">其他</param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<CloudUploadResponse> UploadData([NotNull] string barcode,
            [NotNull] DateTime scanTime,
            [NotNull] double weight,
            [NotNull] string scanNodName,
            CloudUploadVolumeInfo? volumeInfo = default,
            List<CloudUploadImageInfo>? imageInfos = default,
            CloudUploadOcrInfo? ocrInfo = default,
            CloudUploadApiInfo? uploadApiInfo = default,
            CloudUploadSortingInfo? sortingInfo = default,
            CloudNvrCameraBindingInfo? nvrCameraBindingInfo = default,
            object? other = null, CancellationToken token = default);

        /// <summary>
        /// 设置接口参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters);

        /// <summary>
        /// 设置接口参数
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetParameters(Dictionary<string, object> parameters);
    }

    public class CloudVideoUploadMessage {

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>

        public bool IsSuccessful { get; set; }

        /// <summary>
        /// 全景图数量
        /// </summary>
        public int PanoramaImageCount { get; set; }

        /// <summary>
        /// 扫码图数量
        /// </summary>
        public int ScanImageCount { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }
    }

    public class CloudVideoUploadRetryMessage {

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }
    }

    public class CloudUploadResponse {

        /// <summary>
        /// 上传内容
        /// </summary>
        public string? UploadContent { get; set; }

        /// <summary>
        /// 响应内容
        /// </summary>
        public string? ResponseContent { get; set; }

        /// <summary>
        /// 上传耗时(毫秒)
        /// </summary>
        public int? UploadDuration { get; set; }

        /// <summary>
        /// 目标地址
        /// </summary>
        public string? TargetAddress { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime? UploadTime { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ExceptionMsg { get; set; } = string.Empty;
    }

    /// <summary>
    /// 体积信息
    /// </summary>
    public class CloudUploadVolumeInfo {

        /// <summary>
        /// 长
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// 宽
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// 高
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public float Volume { get; set; }
    }

    /// <summary>
    /// 图片信息
    /// </summary>
    public class CloudUploadImageInfo {

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机自定义名称
        /// </summary>
        public string CustomCameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 图片类型(0=扫码、1=全景、2=体积云点、3=面单抠图)
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public Image? Image { get; set; }
    }

    /// <summary>
    /// Ocr信息
    /// </summary>

    public class CloudUploadOcrInfo {
    }

    /// <summary>
    /// 接口上传信息
    /// </summary>
    public class CloudUploadApiInfo {
    }

    /// <summary>
    /// 分拣信息
    /// </summary>
    public class CloudUploadSortingInfo {
    }

    /// <summary>
    /// NVR绑定信息
    /// </summary>
    public class CloudNvrCameraBindingInfo {

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 通道
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// 扫码相机序列号
        /// </summary>
        public string BarcodeScannerSerialNumber { get; set; } = string.Empty;
    }
}