using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Interface;
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
        /// 创建包裹
        /// </summary>
        event EventHandler<string> CreatePackageEvent;

        /// <summary>
        /// 移除包裹
        /// </summary>
        event EventHandler<string> RemovePackageEvent;

        /// <summary>
        /// 解除异常
        /// </summary>
        event EventHandler<string> ClearExceptionEvent;

        /// <summary>
        /// 执行分拣
        /// </summary>

        void ExecuteSorting(SortingParam param, CancellationToken token = default);

        /// <summary>
        /// 发送一组指令
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        void SendInstructions(long grid, List<string> instructions, TimeSpan interval);

        /// <summary>
        /// 发送一组指(包含应答)
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="sortingInstructionInfoModels"></param>
        /// <param name="interval"></param>
        void SendInstructions(long grid, List<SortingInstructionInfoModel> sortingInstructionInfoModels, TimeSpan interval);

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
        void ExceptionSorting(long guid = 0, CancellationToken token = default);

        /// <summary>
        /// 条码分拣
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void BarcodeSorting(string barcode, long guid, CancellationToken token = default);

        /// <summary>
        /// 重量分拣
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void WeightSorting(float weight, long guid, CancellationToken token = default);

        /// <summary>
        /// 体积分拣
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        void VolumeSorting(double length, double width, double height, double volume, long guid, CancellationToken token = default);

        /// <summary>
        /// 物流分拣
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void LogisticsSorting(string barcode, long guid, CancellationToken token = default);

        /// <summary>
        /// Ocr分拣
        /// </summary>
        /// <param name="ocrContent"></param>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void OcrSorting(string ocrContent, long guid, CancellationToken token = default);

        /// <summary>
        /// Api响应内容分拣
        /// </summary>
        /// <param name="apiResponse"></param>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void ApiResponseSorting(UploadResponse apiResponse, long guid, CancellationToken token = default);

        /// <summary>
        /// 工作流分拣
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void CombinedWorkflowSorting(long guid = 0, CancellationToken token = default);
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

    public class SortingParam {
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
        /// Ocr三段码
        /// </summary>
        public string OcrCode { get; set; } = string.Empty;

        /// <summary>
        /// Api响应内容
        /// </summary>
        public UploadResponse ApiResponse { get; set; } = new();
    }
}