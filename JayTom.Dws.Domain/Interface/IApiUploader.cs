using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using JayTom.Dws.Domain.Interface.Attributes;

namespace JayTom.Dws.Domain.Interface {

    public interface IApiUploader<out T> where T : BaseApiParameters, new() {

        /// <summary>
        /// 参数
        /// </summary>
        T Parameters { get; }

        //设置参数
        /// <summary>
        /// 设置参数
        /// </summary>
        bool SetParameters(object parameters);

        /// <summary>
        /// 上传信息请求接口
        /// </summary>
        Task<UploadResponse> UploadInformation([NotNull] string barcode, [NotNull] double weight, DateTime scanTime = default, double length = default, double width = default, double height = default,
            double volume = default, long packageId = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 扫描包裹
        /// </summary>
        void ScanPackage([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default, double width = default, double height = default,
            double volume = default, long packageId = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 发送分拣报告
        /// </summary>
        Task<UploadResponse> SendSortingReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default, double width = default, double height = default,
            double volume = default, long packageId = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 发送揽件报告
        /// </summary>
        Task<UploadResponse> SendPickupReport([NotNull] string barcode, [NotNull] double weight = default, DateTime scanTime = default, double length = default, double width = default, double height = default,
            double volume = default, long packageId = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 发送集包报告
        /// </summary>
        Task<UploadResponse> SendConsolidationReport(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 发送图片
        /// </summary>
        Task<UploadResponse> SendImage(
            [NotNull] string barcode,
            List<UploadImageInfo> uploadImagesInfos,
            CancellationToken token = default);

        /// <summary>
        /// 发送锁格指令
        /// </summary>
        Task<UploadResponse> SendLockCommand(
            [NotNull] string lockIdentifier,
            object? other = null,
            CancellationToken token = default);

        /// <summary>
        /// 发送解除锁格指令
        /// </summary>
        Task<UploadResponse> SendUnlockCommand(
            [NotNull] string lockIdentifier,
            object? other = null,
            CancellationToken token = default);

        /// <summary>
        /// 发送设备信息报告
        /// </summary>
        Task<UploadResponse> SendDeviceReport(
            string deviceIdentifier,
            string deviceStatus,
            object? other = null,
            CancellationToken token = default);
    }

    public class UploadResponse {

        /// <summary>
        /// 请求内容
        /// </summary>
        public string RequestContent { get; set; } = string.Empty; // 请求内容

        /// <summary>
        /// 响应内容
        /// </summary>
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 返回时间
        /// </summary>
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 耗时(秒)
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// 接口参数
        /// </summary>
        public string ApiParameters { get; set; } = string.Empty;

        /// <summary>
        /// 请求地址
        /// </summary>
        public string RequestUrl { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ExceptionMsg { get; set; } = string.Empty;

        /// <summary>
        /// Api异常类型
        /// </summary>
        public ApiExceptionType ApiExceptionType { get; set; } = ApiExceptionType.None;

        /// <summary>
        /// 执行类型
        /// </summary>
        public ExecutionType ExecutionType { get; set; }
    }

    public enum ApiExceptionType {

        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 访问超时
        /// </summary>
        Timeout = 1,

        /// <summary>
        /// Url无法访问
        /// </summary>
        UnreachableUrl = 2,

        /// <summary>
        /// 未通过逻辑效验
        /// </summary>
        LogicValidationFailed = 3,

        /// <summary>
        /// 内容解析异常
        /// </summary>
        ContentParsingException = 4,

        /// <summary>
        /// 其他
        /// </summary>
        Other = 5
    }

    public class UploadImageInfo {

        /// <summary>
        /// 图片
        /// </summary>
        public Image? Image { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 相机自定义名称
        /// </summary>
        public string CameraCustomName { get; set; } = string.Empty;
    }
}