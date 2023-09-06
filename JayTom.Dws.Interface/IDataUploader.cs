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
        /// <param name="image">图片</param>
        /// <param name="panoramaImage">全景图片</param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, double length = default, double width = default, double height = default,
            double volume = default, Image? image = default, Image? panoramaImage = default, CancellationToken token = default);

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
        /// <param name="image"></param>
        /// <param name="panoramaImage"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, DateTime scanTime, double length = default, double width = default, double height = default,
            double volume = default, Image? image = default, Image? panoramaImage = default, CancellationToken token = default);

        /// <summary>
        /// 设置接口参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters);
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
    }
}