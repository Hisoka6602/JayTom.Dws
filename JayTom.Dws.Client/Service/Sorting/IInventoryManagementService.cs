using System;
using System.Linq;
using System.Text;
using System.Threading;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Service.Sorting {

    /// <summary>
    /// 指令服务(在这里效验通讯)
    /// </summary>
    public interface IInventoryManagementService {

        /// <summary>
        /// 通讯信息事件
        /// </summary>
        event EventHandler<CommunicationMessageInfo> CommunicationInfoEvent;

        /// <summary>
        /// 通讯异常事件
        /// </summary>
        event EventHandler<Exception> CommunicationExceptionEvent;

        /// <summary>
        /// 发送指令
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        /// <param name="attach"></param>
        void SendInstructions(List<string> instructions, TimeSpan interval, InstructionsAttach attach);

        /// <summary>
        /// 发送指令
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="interval"></param>
        /// <param name="attach"></param>
        void SendInstructions(List<SortingInstructionInfoModel> instructions, TimeSpan interval, InstructionsAttach attach);

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
        public string? Guid { get; set; }
    }

    public class InstructionsAttach {

        /// <summary>
        /// 获取或设置唯一标识符。
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// 获取或设置条码信息。
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 获取或设置重量（以千克为单位）。
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// 获取或设置长度（以厘米为单位）。
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// 获取或设置宽度（以厘米为单位）。
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 获取或设置高度（以厘米为单位）。
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 获取或设置体积（以立方厘米为单位）。
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// 获取或设置其他信息（通用对象类型）。
        /// </summary>
        public object? Other { get; set; }
    }
}