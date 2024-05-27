using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Mime;
using System.Threading.Tasks;
using JayTom.Dws.Domain.Service;
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