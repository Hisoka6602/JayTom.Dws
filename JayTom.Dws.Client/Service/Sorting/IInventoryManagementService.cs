using System;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    /// <summary>
    /// 指令服务(在这里效验通讯)
    /// </summary>
    public interface IInventoryManagementService {

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// 通讯信息事件
        /// </summary>
        event EventHandler<CommunicationMessageInfo> CommunicationInfoEvent;

        /// <summary>
        /// 通讯异常事件
        /// </summary>
        event EventHandler<Exception> CommunicationExceptionEvent;

        /// <summary>
        /// 接收的指令
        /// </summary>
        event EventHandler<DeviceDecodeResult> ReceivedInstructionsEvent;

        /// <summary>
        /// 心跳包异常事件
        /// </summary>
        public event EventHandler<Exception> HeartbeatError;

        /// <summary>
        /// 发送异常
        /// </summary>
        event EventHandler<ExceptionEventArgs> SendError;

        /// <summary>
        /// 发送指令(多用于测试)
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        /// <param name="attach"></param>
        void SendInstructions(object tag, List<string> instructions, TimeSpan interval, InstructionsAttach attach);

        /// <summary>
        /// 发送指令
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        /// <param name="attach"></param>
        void SendInstructions(object tag, List<SortingInstructionInfoModel> instructions, TimeSpan interval, InstructionsAttach attach);

        /// <summary>
        /// 连接方法
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Connect(CancellationToken token = default);

        /// <summary>
        /// 断开方法
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, string>> Disconnect(CancellationToken token = default);
    }

    public class CommunicationMessageInfo : CommunicationInfo {

        /// <summary>
        /// 条码关联时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 获取或设置关联的条码。
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 获取或设置关联的出口。
        /// </summary>
        public string? ExitName { get; set; }

        /// <summary>
        /// 获取或设置消息的来源。
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// 获取或设置消息的目的地。
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// 获取或设置分拣的唯一标识符（Guid）。
        /// </summary>
        public long? Guid { get; set; }
    }
}