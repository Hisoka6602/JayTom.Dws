using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.DownstreamProtocols {

    public interface IDeviceCommunicationProtocol {

        /// <summary>
        /// 数据编码
        /// </summary>
        /// <param name="type"></param>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        string EncodeData(FunctionType type, object tag, string data, object? other);

        /// <summary>
        /// 数据解码
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        DeviceDecodeResult? DecodeData(string data);

        /// <summary>
        /// 转换包裹流水号
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        string ConvertSortingCode(object obj);

        /// <summary>
        /// 协议字节长度
        /// </summary>

        public int DataLen { get; }
    }

    public class DeviceDecodeResult {

        /// <summary>
        /// 功能类型
        /// </summary>
        public FunctionType Type { get; set; } = FunctionType.None;

        /// <summary>
        /// 关键字
        /// </summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 原内容
        /// </summary>
        public string RawContent { get; set; } = string.Empty;

        /// <summary>
        /// 协议名称
        /// </summary>
        public string ProtocolName { get; set; } = string.Empty;

        /// <summary>
        /// 关键字所在字节位置
        /// </summary>
        public int KeywordPosition { get; set; }

        /// <summary>
        /// 是否异常
        /// </summary>
        public bool IsException { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// 指令时间
        /// </summary>
        public DateTime Time { get; set; }
    }

    public enum FunctionType {

        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 创建包裹
        /// </summary>
        CreatePackage,

        /// <summary>
        /// 移除包裹(分拣完成)
        /// </summary>
        RemovePackage,

        /// <summary>
        /// 开始运行
        /// </summary>
        StartRunning,

        /// <summary>
        /// 停止运行
        /// </summary>
        StopRunning,

        /// <summary>
        /// 异常信息
        /// </summary>
        ExceptionMessage,

        /// <summary>
        /// 设备信息
        /// </summary>
        DeviceInfo,

        /// <summary>
        /// 心跳包
        /// </summary>
        Heartbeat,

        /// <summary>
        /// 发送出口
        /// </summary>
        SendExit,

        /// <summary>
        /// 解除异常
        /// </summary>
        ClearException,

        /// <summary>
        /// 锁格
        /// </summary>
        LockExit,
    }

    public class InstructionsAttach {

        /// <summary>
        /// 条码关联时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 获取或设置唯一标识符。
        /// </summary>
        public long Guid { get; set; }

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
        /// 格口名称
        /// </summary>
        public string ExitName { get; set; } = string.Empty;

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime? ScanTime { get; set; }

        /// <summary>
        /// 格口Id
        /// </summary>
        public long ExitId { get; set; }

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

        /// <summary>
        /// 获取或设置其他信息（通用对象类型）。
        /// </summary>
        public object? Other { get; set; }
    }
}