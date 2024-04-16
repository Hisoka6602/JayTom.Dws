using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;
using JayTom.Dws.Domain.DownstreamProtocols;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;

namespace JayTom.Dws.Client.Service.Sorting {

    public interface ISortingConnectionService {

        /// <summary>
        /// 连接事件
        /// </summary>
        event EventHandler<ConnectionInfo> Connected;

        /// <summary>
        /// 配置初始化
        /// </summary>
        /// <returns></returns>
        Task ConfigurationInitializer();

        /// <summary>
        /// 添加连接
        /// </summary>
        Task<KeyValuePair<bool, string>> AddConnection(CommunicationsType type, CommunicationProtocol communicationProtocol, string connectionName, object? connectionParam);

        /// <summary>
        /// 释放连接
        /// </summary>
        Task<KeyValuePair<bool, string>> ReleaseConnection(string connectionName);

        /// <summary>
        /// 断开全部
        /// </summary>
        Task<KeyValuePair<bool, string>> DisconnectAll();

        /// <summary>
        /// 通讯信息事件
        /// </summary>
        event EventHandler<ConnectionCommunicationMessageInfo> CommunicationInfoEvent;

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
        /// 断开事件
        /// </summary>
        event EventHandler<ConnectionInfo> Disconnected;

        /// <summary>
        /// 发送指令(多用于测试)
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="exitId"></param>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        /// <param name="attach"></param>
        void SendInstructions(object tag, long exitId, List<string> instructions, TimeSpan interval, InstructionsAttach attach);

        /// <summary>
        /// 发送指令
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="exitId"></param>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        /// <param name="attach"></param>
        void SendInstructions(object tag, long exitId, List<SortingInstructionInfoModel> instructions, TimeSpan interval, InstructionsAttach attach);

        /// <summary>
        /// 发送前置信号
        /// </summary>
        void SendPreSignal(int num, InstructionsAttach attach, CancellationToken token = default);

        /// <summary>
        /// 发送信息组合完成信号
        /// </summary>
        void SendPackageInfoCompletedSignal(int num, InstructionsAttach attach, CancellationToken token = default);
    }

    public class ConnectionCommunicationMessageInfo : CommunicationInfo {

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;

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

    public class ConnectionInfo {

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// 通讯类型
        /// </summary>
        public CommunicationsType Type { get; set; }

        /// <summary>
        /// 协议对象
        /// </summary>
        public IDeviceCommunicationProtocol? DeviceCommunicationProtocol { get; set; }

        /// <summary>
        /// 串口对象
        /// </summary>
        public ISortingSerialPort? SortingSerialPort { get; set; }

        /// <summary>
        /// Tcp对象
        /// </summary>
        public ISortingTcp? SortingTcp { get; set; }
    }
}