using System;
using System.Threading;
using JayTom.Dws.Interface;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using JayTom.Dws.Client.Service.BackgroundService;
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
        /// 创建包裹
        /// </summary>
        event EventHandler<PackageInstructionEventArgs> CreatePackageEvent;

        /// <summary>
        /// 移除包裹
        /// </summary>
        event EventHandler<PackageInstructionEventArgs> RemovePackageEvent;

        /// <summary>
        /// 解除异常
        /// </summary>
        event EventHandler<string> ClearExceptionEvent;

        /// <summary>
        /// 执行分拣
        /// </summary>

        void ExecuteSorting(SortingParam param, CancellationToken token = default);

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

        /// <summary>
        /// 异常口分拣
        /// </summary>
        void ExceptionSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// 条码分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void BarcodeSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// 重量分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void WeightSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// 体积分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void VolumeSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// 物流分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void LogisticsSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// Ocr分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void OcrSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// Api响应内容分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void ApiResponseSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// 工作流分拣
        /// </summary>
        /// <param name="param"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void CombinedWorkflowSorting(SortingParam param, CancellationToken token = default);
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

    public class PackageInstructionEventArgs : EventArgs {

        /// <summary>
        /// 关键字
        /// </summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>
        /// 指令
        /// </summary>
        public string Instruction { get; set; } = string.Empty;
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

    public class SortingParam {
        public object? Tag { get; set; }

        /// <summary>
        /// 是否由下位机创建包裹
        /// </summary>
        public bool IsCreatedByLowerMachine { get; set; } = false;

        /// <summary>
        /// 创建包裹的时间
        /// </summary>
        public DateTime PackageCreationTime { get; set; }

        /// <summary>
        /// 创建包裹的指令
        /// </summary>
        public string? PackageCreationInstruction { get; set; }

        /// <summary>
        /// 条码关联时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        ///
        /// </summary>
        public long Guid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime? ScanTime { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// Ocr三段码信息
        /// </summary>
        public PackageOcrInfo OcrInfo { get; set; } = new();

        /// <summary>
        /// Api响应内容
        /// </summary>
        public UploadResponse ApiResponse { get; set; } = new();

        /// <summary>
        /// 格口Id
        /// </summary>
        public long ExitId { get; set; }
    }

    /// <summary>
    /// 执行指令回传类
    /// </summary>
    public class InstructionReceived {

        /// <summary>
        /// 条码关联时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime? ScanTime { get; set; }

        /// <summary>
        /// 格口Id
        /// </summary>
        public long ExitId { get; set; }

        /// <summary>
        /// 格口名称
        /// </summary>
        public string ExitName { get; set; } = string.Empty;

        /// <summary>
        /// 物流Id
        /// </summary>
        public long LogisticsId { get; set; }

        /// <summary>
        /// 物流名称
        /// </summary>
        public string LogisticsName { get; set; } = string.Empty;

        /// <summary>
        /// 分拣模式
        /// </summary>
        public SortMode SortingMode { get; set; }

        /// <summary>
        /// 发送的指令
        /// </summary>
        public string SentInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 指令发送时间
        /// </summary>
        public DateTime SendTime { get; set; }

        /// <summary>
        /// 创建包裹时间
        /// </summary>
        public DateTime PackageCreationTime { get; set; }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        public string PackageCreationInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 是否由下位机创建
        /// </summary>
        public bool IsCreatedByLowerMachine { get; set; }

        /// <summary>
        /// 指令目标
        /// </summary>
        public string CommandTarget { get; set; } = string.Empty;

        /// <summary>
        /// 通讯方式
        /// </summary>
        public CommunicationsType CommunicationMethod { get; set; }

        /// <summary>
        /// 效验协议名称
        /// </summary>
        public string ChecksumProtocolName { get; set; } = string.Empty;
    }
}