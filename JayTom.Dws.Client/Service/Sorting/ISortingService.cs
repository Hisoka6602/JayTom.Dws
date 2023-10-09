using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    public interface ISortingService {

        /// <summary>
        /// 异常事件
        /// </summary>
        event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// 重试事件
        /// </summary>
        event EventHandler<RetryEventArgs> RetryOccurred;

        /// <summary>
        /// 收发日志
        /// </summary>
        event EventHandler<LogEventArgs> LogReceived;

        /// <summary>
        /// 心跳包异常事件
        /// </summary>
        public event EventHandler<Exception> HeartbeatError;

        /// <summary>
        /// 发送异常
        /// </summary>
        event EventHandler<ExceptionEventArgs> SendError;

        /// <summary>
        /// 执行分拣
        /// </summary>
        /// <param name="barCode"></param>
        /// <param name="scanTime"></param>
        /// <param name="sortingType"></param>
        /// <param name="apiResponseContent"></param>
        void ExecuteSorting(string barCode, DateTime scanTime, object sortingType, string apiResponseContent);

        /// <summary>
        /// 发送一组指令
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        void SendInstructions(List<string> instructions, TimeSpan interval);

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 是否需要分拣
        /// </summary>
        bool IsSortingEnabled { get; }

        /// <summary>
        /// 获取物流信息
        /// </summary>
        /// <param name="barCode"></param>
        /// <returns></returns>
        Task<LogisticsCodeRecognitionInfoModel?> GetLogisticsInfo(string barCode);

        /// <summary>
        /// 运行状态
        /// </summary>
        public bool RunningStatus { get; }

        /// <summary>
        /// 启动分拣服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Start(CancellationToken token = default);

        /// <summary>
        /// 停止分拣服务
        /// </summary>
        Task<KeyValuePair<bool, string>> Stop(CancellationToken token = default);
    }

    public class ExceptionEventArgs : EventArgs {
        public string ExceptionMessage { get; set; } = string.Empty;
    }

    public class RetryEventArgs : EventArgs {

        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailureReason { get; set; } = string.Empty;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 指令组
        /// </summary>
        public List<string> Instructions { get; set; } = new();
    }

    public class LogEventArgs : EventArgs {
        public string LogContent { get; set; } = string.Empty;

        //接收的内容
    }

    //需要实现数据库映射
    public class SendInstructionsLog {
        //发送的内容
        //发送时间
        //发送的内容Guid
        //目标
        //发送间隔
        //发送总耗时
        //绑定的格口
        //扫码时间
        //条码
        //效验协议
        //通讯协议
        //是否心跳包
    }

    //需要实现数据库映射
    public class ReceiveInstructionsLog {
        //接收的内容
        //接收时间
        //对应的发送指令
        //对应的发送指令Guid
        //目标
        //效验协议
        //通讯协议
        //是否心跳包
    }
}