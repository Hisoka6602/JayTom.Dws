using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Interface {

    public interface IDataUploader {

        /// <summary>
        /// 数据上传
        /// </summary>
        /// <param name="barcode">条码</param>
        /// <param name="weight">重量</param>
        /// <param name="length">长</param>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        /// <param name="volume">体积</param>
        /// <param name="imageInfo">图片信息</param>
        /// <param name="panoramaImageInfos"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight,
            double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null, CancellationToken token = default);

        /// <summary>
        /// 数据上传
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="weight"></param>
        /// <param name="scanTime"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="volume"></param>
        /// <param name="panoramaImageInfos"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        /// <param name="imageInfo"></param>
        /// <returns></returns>
        Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, DateTime scanTime, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default);

        /// <summary>
        /// 设置接口参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters);

        /// <summary>
        /// 包裹结束后上传(无返回接收)
        /// </summary>
        void UploadInBackground([NotNull] string barcode, [NotNull] double weight, DateTime scanTime, double length = default, double width = default, double height = default,
            double volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
            CancellationToken token = default);

        /// <summary>
        /// 集包上传
        /// </summary>
        /// <param name="packageExit"></param>
        /// <param name="aggregatePackageCode"></param>
        /// <param name="packagingTime"></param>
        /// <param name="packageItems"></param>
        /// <param name="other"></param>
        /// <param name="token"></param>
        void PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems, object? other = null, CancellationToken token = default);
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