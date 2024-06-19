using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
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

        /// <summary>
        /// 异常类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public SortingExceptionReturnType SortingExceptionReturnTypeConvert(string obj);

        /// <summary>
        /// 异常类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public SortingExceptionReturnType SortingExceptionReturnTypeConvert(byte obj);

        /// <summary>
        /// 转换指令信息
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public CommandParsing? CommandParsingConvert(object obj);

        /// <summary>
        /// 格口匹配内容转换
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string? ExitContentConvert(object data);
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

        /// <summary>
        /// 包裹分拣异常返回类型
        /// </summary>
        public SortingExceptionReturnType SortingExceptionReturnType { get; set; }

        /// <summary>
        /// 指令解析
        /// </summary>
        public CommandParsing CommandParsing { get; set; } = new();

        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; } = string.Empty;
    }

    public enum FunctionType {

        /// <summary>
        /// 无
        /// </summary>
        [Description("无")]
        None,

        /// <summary>
        /// 创建包裹
        /// </summary>
        [Description("创建包裹")]
        CreatePackage,

        /// <summary>
        /// 移除包裹(分拣完成)
        /// </summary>
        [Description("落格完成")]
        RemovePackage,

        /// <summary>
        /// 包裹异常
        /// </summary>
        [Description("包裹异常")]
        PackageException,

        /// <summary>
        /// 包裹异常(需要判断操作)
        /// </summary>
        [Description("包裹异常")]
        PackageExceptionEx,

        /// <summary>
        /// 开始运行
        /// </summary>
        [Description("开始运行")]
        StartRunning,

        /// <summary>
        /// 停止运行
        /// </summary>
        [Description("停止运行")]
        StopRunning,

        /// <summary>
        /// 异常信息
        /// </summary>
        [Description("异常信息")]
        ExceptionMessage,

        /// <summary>
        /// 设备信息
        /// </summary>
        [Description("设备信息")]
        DeviceInfo,

        /// <summary>
        /// 心跳包
        /// </summary>
        [Description("心跳包")]
        Heartbeat,

        /// <summary>
        /// 发送出口
        /// </summary>
        [Description("发送出口")]
        SendExit,

        /// <summary>
        /// 解除异常
        /// </summary>
        [Description("解除异常")]
        ClearException,

        /// <summary>
        /// 锁格
        /// </summary>
        [Description("锁格")]
        LockExit,

        /// <summary>
        /// 发送前置动作
        /// </summary>
        [Description("发送前置动作")]
        SendPreSignal,

        /// <summary>
        /// 接收前置动作回复
        /// </summary>
        [Description("接收前置动作回复")]
        ReceivePreSignalReply,

        /// <summary>
        /// 包裹信息赋值完成
        /// </summary>
        [Description("包裹信息赋值完成")]
        PackageInfoCompletedSignal,

        /// <summary>
        /// 序号绑定回复
        /// </summary>
        [Description("序号绑定回复")]
        SequenceBindingReply,

        /// <summary>
        /// 复位按钮触发
        /// </summary>
        [Description("复位按钮触发")]
        ResetButtonTrigger,

        /// <summary>
        /// 包裹居中
        /// </summary>
        [Description("包裹居中")]
        PackageCenter,
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

        /// <summary>
        /// 包裹位置信息(灰度仪居中使用)
        /// </summary>
        public PackagePositionInfo? PackagePositionInfo { get; set; }

        /// <summary>
        /// 联动车辆
        /// </summary>
        public int LinkedCarCount { get; set; } = 0;
    }

    public enum SortingExceptionReturnType {

        /// <summary>
        /// 无
        /// </summary>
        [Description("分拣成功")]
        None,

        /// <summary>
        /// 距离过近
        /// </summary>
        [Description("距离过近")]
        DistanceTooClose,

        /// <summary>
        /// 锁格
        /// </summary>
        [Description("锁格")]
        LockExit,

        /// <summary>
        /// 车号不匹配
        /// </summary>
        [Description("车号不匹配")]
        VehicleNumberMismatch,

        /// <summary>
        /// 线速度未稳定放包
        /// </summary>
        [Description("线速度未稳定放包")]
        UnstableLineSpeed
    }

    public class CommandParsing {

        /// <summary>
        /// 功能码
        /// </summary>
        public byte FunctionCode { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// 格口号
        /// </summary>
        public uint CompartmentNumber { get; set; }

        /// <summary>
        /// 异常码
        /// </summary>
        public byte ExceptionCode { get; set; }
    }

    public class PackagePositionInfo {

        /// <summary>
        /// 中心点X
        /// </summary>
        public int CenterX { get; set; }

        /// <summary>
        /// 中心点Y
        /// </summary>
        public int CenterY { get; set; }

        /// <summary>
        /// 偏移方向
        /// </summary>
        public OffsetDirection OffsetDirection { get; set; }

        /// <summary>
        /// 偏移量
        /// </summary>
        public int OffsetDistance { get; set; }
    }

    /// <summary>
    /// 偏移方向
    /// </summary>
    public enum OffsetDirection {

        /// <summary>
        /// 左
        /// </summary>
        [Description("偏左")]
        Left,

        /// <summary>
        /// 右
        /// </summary>
        [Description("偏右")]
        Right
    }
}