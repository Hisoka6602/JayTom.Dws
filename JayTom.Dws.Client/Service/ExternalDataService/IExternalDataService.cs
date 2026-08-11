using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Service.ExternalDataService
{

    public interface IExternalDataService : IDisposable
    {

        /// <summary>
        /// 输出失败回调事件
        /// </summary>
        event EventHandler<Exception> ExternalDataException;

        /// <summary>
        /// 外部数据源生效事件
        /// </summary>
        event EventHandler<ExternalDataSourceEventArgs> DataSourceEnabled;

        /// <summary>
        /// 获取到外部体积事件
        /// </summary>
        event EventHandler<ExternalVolumeInputEventArgs> VolumeReceived;

        /// <summary>
        /// 获取外部数据输入事件
        /// </summary>
        event EventHandler<ExternalContentInputEventArgs> ContentInputReceived;

        /// <summary>
        /// 获取到外部重量事件
        /// </summary>
        event EventHandler<KeyValuePair<bool, string>> WeightReceived;

        /// <summary>
        /// 获取到外部图片路径事件
        /// </summary>
        event EventHandler<KeyValuePair<bool, string>> ImagePathReceived;

        /// <summary>
        /// 获取到外部接口响应内容事件
        /// </summary>
        event EventHandler<KeyValuePair<bool, string>> ResponseContentReceived;

        /// <summary>
        /// 获取外部体积
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> GetVolume(string barcode, CancellationToken token = default);

        /// <summary>
        /// 获取外部重量
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> GetWeight(string barcode, CancellationToken token = default);

        /// <summary>
        /// 获取外部图片路径
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> GetImagePath(string barcode, CancellationToken token = default);

        /// <summary>
        /// 获取外部响应内容
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> GetResponseContent(string barcode, CancellationToken token = default);

        /// <summary>
        /// 启动设备服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Start(CancellationToken token = default);

        /// <summary>
        /// 停止设备服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default);
    }

    /// <summary>
    /// 外部体积输入类型
    /// </summary>
    public class ExternalVolumeInputEventArgs : EventArgs
    {

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 长
        /// </summary>
        public decimal Length { get; set; }

        /// <summary>
        /// 宽
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// 高
        /// </summary>
        public decimal Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 接收时间
        /// </summary>
        public DateTime ReceiveTime { get; set; }

        /// <summary>
        /// 接收源
        /// </summary>
        public string ReceiveSource { get; set; } = string.Empty;
    }

    /// <summary>
    /// 外部输入源
    /// </summary>
    public class ExternalDataSourceEventArgs : EventArgs
    {

        /// <summary>
        /// 是否输入体积
        /// </summary>
        public bool IsVolumeInput { get; set; }

        /// <summary>
        /// 是否输入重量
        /// </summary>
        public bool IsWeightInput { get; set; }

        /// <summary>
        /// 是否输入体积
        /// </summary>
        public bool IsResponseContentInput { get; set; }

        /// <summary>
        /// 是否输入图片路径
        /// </summary>
        public bool IsImagePathInput { get; set; }
    }

    public class ExternalContentInputEventArgs : EventArgs
    {

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 重量
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public decimal Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public decimal Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public decimal Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 源内容
        /// </summary>
        public string SourceContent { get; set; } = string.Empty;
    }
}