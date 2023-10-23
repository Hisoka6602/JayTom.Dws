using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.DownstreamProtocols {

    public interface IDeviceCommunicationProtocol {

        /// <summary>
        /// 数据编码
        /// </summary>
        /// <param name="type"></param>
        /// <param name="num"></param>
        /// <param name="data"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        string EncodeData(FunctionType type, int num, string data, object? other);

        /// <summary>
        /// 数据解码
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        DeviceDecodeResult? DecodeData(string data);
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
        /// 移除包裹
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
        ClearException
    }
}