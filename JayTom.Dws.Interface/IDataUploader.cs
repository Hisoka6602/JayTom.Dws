using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LegacyUploadMeasurement = System.Double;

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
        Task<UploadResponse> UploadData(string barcode, LegacyUploadMeasurement weight,
            LegacyUploadMeasurement length = default, LegacyUploadMeasurement width = default, LegacyUploadMeasurement height = default,
            LegacyUploadMeasurement volume = default, UploadImageInfo? imageInfo = default,
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
        Task<UploadResponse> UploadData(string barcode, LegacyUploadMeasurement weight, DateTime scanTime, LegacyUploadMeasurement length = default, LegacyUploadMeasurement width = default, LegacyUploadMeasurement height = default,
            LegacyUploadMeasurement volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null, CancellationToken token = default);

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
        Task UploadInBackground(string barcode, LegacyUploadMeasurement weight, DateTime scanTime, LegacyUploadMeasurement length = default, LegacyUploadMeasurement width = default, LegacyUploadMeasurement height = default,
            LegacyUploadMeasurement volume = default, UploadImageInfo? imageInfo = default, List<UploadImageInfo>? panoramaImageInfos = default, object? other = null,
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
        Task PackageAggregation(string packageExit, string aggregatePackageCode, DateTime packagingTime, List<string> packageItems, object? other = null, CancellationToken token = default);
    }

    /// <summary>
    /// 将 IDataUploader 遗留浮点边界立即转换为定点数的适配基类。
    /// </summary>
    public abstract class FixedPointDataUploaderBase : IDataUploader {
        /// <summary>
        /// 使用定点测量值上传数据，供禁止浮点数的新代码直接调用。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传响应。</returns>
        public Task<UploadResponse> UploadFixedPointData(
            string barcode,
            decimal weight,
            decimal length = default,
            decimal width = default,
            decimal height = default,
            decimal volume = default,
            UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null,
            CancellationToken token = default) {
            return UploadFixedPointDataAsync(
                barcode,
                weight,
                length,
                width,
                height,
                volume,
                imageInfo,
                panoramaImageInfos,
                other,
                token);
        }

        /// <summary>
        /// 将遗留测量值转换为定点数并上传数据。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传响应。</returns>
        public Task<UploadResponse> UploadData(
            string barcode,
            LegacyUploadMeasurement weight,
            LegacyUploadMeasurement length = default,
            LegacyUploadMeasurement width = default,
            LegacyUploadMeasurement height = default,
            LegacyUploadMeasurement volume = default,
            UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null,
            CancellationToken token = default) {
            return UploadFixedPointDataAsync(
                barcode,
                Convert.ToDecimal(weight),
                Convert.ToDecimal(length),
                Convert.ToDecimal(width),
                Convert.ToDecimal(height),
                Convert.ToDecimal(volume),
                imageInfo,
                panoramaImageInfos,
                other,
                token);
        }

        /// <summary>
        /// 将带扫码时间的遗留测量值转换为定点数并上传数据。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传响应。</returns>
        public Task<UploadResponse> UploadData(
            string barcode,
            LegacyUploadMeasurement weight,
            DateTime scanTime,
            LegacyUploadMeasurement length = default,
            LegacyUploadMeasurement width = default,
            LegacyUploadMeasurement height = default,
            LegacyUploadMeasurement volume = default,
            UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null,
            CancellationToken token = default) {
            return UploadFixedPointDataAsync(
                barcode,
                Convert.ToDecimal(weight),
                scanTime,
                Convert.ToDecimal(length),
                Convert.ToDecimal(width),
                Convert.ToDecimal(height),
                Convert.ToDecimal(volume),
                imageInfo,
                panoramaImageInfos,
                other,
                token);
        }

        /// <summary>
        /// 设置接口参数。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="parameters">接口参数。</param>
        /// <returns>参数是否设置成功及失败原因。</returns>
        public abstract Task<KeyValuePair<bool, string>> SetParameters<T>(
            T parameters);

        /// <summary>
        /// 将遗留测量值转换为定点数并执行后台上传。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>后台上传任务。</returns>
        public Task UploadInBackground(
            string barcode,
            LegacyUploadMeasurement weight,
            DateTime scanTime,
            LegacyUploadMeasurement length = default,
            LegacyUploadMeasurement width = default,
            LegacyUploadMeasurement height = default,
            LegacyUploadMeasurement volume = default,
            UploadImageInfo? imageInfo = default,
            List<UploadImageInfo>? panoramaImageInfos = default,
            object? other = null,
            CancellationToken token = default) {
            return UploadFixedPointDataInBackgroundAsync(
                barcode,
                Convert.ToDecimal(weight),
                scanTime,
                Convert.ToDecimal(length),
                Convert.ToDecimal(width),
                Convert.ToDecimal(height),
                Convert.ToDecimal(volume),
                imageInfo,
                panoramaImageInfos,
                other,
                token);
        }

        /// <summary>
        /// 执行集包上传。
        /// </summary>
        /// <param name="packageExit">格口。</param>
        /// <param name="aggregatePackageCode">集包码。</param>
        /// <param name="packagingTime">集包时间。</param>
        /// <param name="packageItems">包裹列表。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>集包上传任务。</returns>
        public abstract Task PackageAggregation(
            string packageExit,
            string aggregatePackageCode,
            DateTime packagingTime,
            List<string> packageItems,
            object? other = null,
            CancellationToken token = default);

        /// <summary>
        /// 使用定点测量值上传数据。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传响应。</returns>
        protected abstract Task<UploadResponse> UploadFixedPointDataAsync(
            string barcode,
            decimal weight,
            decimal length,
            decimal width,
            decimal height,
            decimal volume,
            UploadImageInfo? imageInfo,
            List<UploadImageInfo>? panoramaImageInfos,
            object? other,
            CancellationToken token);

        /// <summary>
        /// 使用带扫码时间的定点测量值上传数据。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>上传响应。</returns>
        protected abstract Task<UploadResponse> UploadFixedPointDataAsync(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length,
            decimal width,
            decimal height,
            decimal volume,
            UploadImageInfo? imageInfo,
            List<UploadImageInfo>? panoramaImageInfos,
            object? other,
            CancellationToken token);

        /// <summary>
        /// 使用定点测量值执行后台上传。
        /// </summary>
        /// <param name="barcode">条码。</param>
        /// <param name="weight">重量。</param>
        /// <param name="scanTime">扫码时间。</param>
        /// <param name="length">长度。</param>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="volume">体积。</param>
        /// <param name="imageInfo">扫码图片。</param>
        /// <param name="panoramaImageInfos">全景图片。</param>
        /// <param name="other">扩展信息。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>后台上传任务。</returns>
        protected abstract Task UploadFixedPointDataInBackgroundAsync(
            string barcode,
            decimal weight,
            DateTime scanTime,
            decimal length,
            decimal width,
            decimal height,
            decimal volume,
            UploadImageInfo? imageInfo,
            List<UploadImageInfo>? panoramaImageInfos,
            object? other,
            CancellationToken token);
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
