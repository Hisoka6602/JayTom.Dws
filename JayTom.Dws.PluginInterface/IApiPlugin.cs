using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Collections.Generic;
using Image = System.Drawing.Image;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.PluginInterface {

    /// <summary>
    /// 接口插件
    /// </summary>
    public interface IApiPlugin : IPlugin {

        /// <summary>
        /// 数据上传
        /// </summary>
        Task<UploadResponse> UploadData([NotNull] string barcode, [NotNull] double weight, double length = default, double width = default, double height = default,
            double volume = default, Image? image = default, Image? panoramaImage = default, CancellationToken token = default);

        /// <summary>
        /// 设置参数
        /// </summary>
        Task<KeyValuePair<bool, string>> SetParameters(CancellationToken token = default);
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