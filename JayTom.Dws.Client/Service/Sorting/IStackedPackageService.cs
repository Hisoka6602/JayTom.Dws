using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Service.BackgroundService;

namespace JayTom.Dws.Client.Service.Sorting {

    public interface IStackedPackageService {

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<EventArgs> Connected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        event EventHandler<EventArgs> Disconnected;

        /// <summary>
        /// 叠包返回事件
        /// </summary>
        event EventHandler<StackedPackageEventArgs> StackedPackageReturned;

        /// <summary>
        /// 异常事件
        /// </summary>
        event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 启动监控服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Start(CancellationToken token = default);

        /// <summary>
        /// 停止监控服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default);

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> SetParameters<T>(T parameters);
    }

    public class StackedPackageEventArgs : EventArgs {

        /// <summary>
        /// 包裹时间
        /// </summary>
        public DateTime PackageTime { get; set; }

        /// <summary>
        /// 包裹信息
        /// </summary>
        public PackageInfo? PackageInfo { get; set; }

        /// <summary>
        /// 接收内容
        /// </summary>
        public string StackedContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否重叠
        /// </summary>
        public bool IsStacked { get; set; }

        /// <summary>
        /// 接收时间
        /// </summary>
        public DateTime ReceivedTime { get; set; } = DateTime.Now;
    }
}