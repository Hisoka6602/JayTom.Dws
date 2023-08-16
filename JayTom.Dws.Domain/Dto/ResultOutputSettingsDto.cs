using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {

    public class ResultOutputSettingsDto {

        /// <summary>
        /// 数据模板
        /// </summary>
        public List<ItemTemplateInfo> DataTemplate { get; set; } = new();

        /// <summary>
        /// 上传设置
        /// </summary>
        public UploadSettingsInfo UploadSettingsInfo { get; set; } = new();

        /// <summary>
        /// 是否使用Tcp输出
        /// </summary>
        public bool IsUseTcpOutput { get; set; }

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo { get; set; } = new();

        /// <summary>
        /// 是否使用Http输出
        /// </summary>
        public bool IsUseHttpOutput { get; set; }

        /// <summary>
        /// Http输出配置
        /// </summary>
        public HttpUploadSettingsInfo HttpUploadSettingsInfo { get; set; } = new();

        /// <summary>
        /// 是否使用串口输出
        /// </summary>
        public bool IsUseSerialOutput { get; set; }

        /// <summary>
        /// 串口输出配置
        /// </summary>
        public SerialPortSettingsInfo SerialPortSettingsInfo { get; set; } = new();

        /// <summary>
        /// 是否使用音频输出
        /// </summary>
        public bool IsUseAudioOutput { get; set; }

        /// <summary>
        /// 音频输出配置
        /// </summary>
        public AudioOutputSettingsInfo AudioOutputSettingsInfo { get; set; } = new();

        /// <summary>
        /// 是否使用位置输出
        /// </summary>
        public bool IsUseLocationOutput { get; set; }

        /// <summary>
        /// 位置输出位置
        /// </summary>
        public LocationOutputSettingsInfo LocationOutputSettingsInfo { get; set; } = new();
    }

    /// <summary>
    /// 上传设置
    /// </summary>
    public class UploadSettingsInfo {

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 发送延迟
        /// </summary>
        public int SendDelay { get; set; }

        /// <summary>
        /// 是否程序重启后自动上传未成功数据
        /// </summary>
        public bool IsAutoUploadOnRestart { get; set; }
    }

    /// <summary>
    /// Tcp设置
    /// </summary>
    public class TcpSettingsInfo {

        /// <summary>
        /// 连接模式(客户端、服务端)
        /// </summary>
        public TcpConnectionMode ConnectionMode { get; set; }

        /// <summary>
        /// 客户端配置
        /// </summary>
        public TcpInfo ClientConfig { get; set; } = new();

        /// <summary>
        /// 服务端配置
        /// </summary>
        public TcpInfo ServerConfig { get; set; } = new();
    }

    public enum TcpConnectionMode {

        /// <summary>
        /// 客户端
        /// </summary>
        Client,

        /// <summary>
        /// 服务端
        /// </summary>
        Server
    }

    public class TcpInfo {

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// 串口设置
    /// </summary>
    public class SerialPortSettingsInfo {

        /// <summary>
        /// 串口名称
        /// </summary>
        public string PortName { get; set; } = string.Empty;     // 串口名称

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// 效验位
        /// </summary>
        public Parity Parity { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; }
    }

    /// <summary>
    /// http上传设置
    /// </summary>
    public class HttpUploadSettingsInfo {

        /// <summary>
        /// Url地址
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 成功回调内容
        /// </summary>
        public string SuccessResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 请求超时时间
        /// </summary>
        public int Timeout { get; set; } = 2000;
    }

    /// <summary>
    /// 音频输出
    /// </summary>
    public class AudioOutputSettingsInfo {

        /// <summary>
        /// 成功音频
        /// </summary>
        public byte[]? SuccessAudio { get; set; }

        /// <summary>
        /// 失败音频
        /// </summary>
        public byte[]? FailureAudio { get; set; }

        /// <summary>
        /// 触发位置
        /// </summary>
        public TriggerPositionEnum TriggerPosition { get; set; }
    }

    public enum TriggerPositionEnum {

        /// <summary>
        /// Http输出后
        /// </summary>
        HttpOutput,

        /// <summary>
        /// Tcp输出后
        /// </summary>
        TcpOutput,

        /// <summary>
        /// 串口输出后
        /// </summary>
        SerialPortOutput,

        /// <summary>
        /// 位置输出后
        /// </summary>
        LocationOutput,

        /// <summary>
        /// 包裹触发后
        /// </summary>
        PackageTrigger
    }

    /// <summary>
    /// 位置输出
    /// </summary>
    public class LocationOutputSettingsInfo {

        /// <summary>
        /// 条码输出位置
        /// </summary>
        public Point BarcodeOutputPosition { get; set; }

        /// <summary>
        /// 重量输出位置
        /// </summary>
        public Point WeightOutputPosition { get; set; }

        /// <summary>
        /// 条码输出后按键
        /// </summary>
        public string? BarcodeOutputKey { get; set; }

        /// <summary>
        /// 重量输出后按键
        /// </summary>
        public string? WeightOutputKey { get; set; }

        /// <summary>
        /// 操作延迟
        /// </summary>
        public int OperationDelay { get; set; }

        /// <summary>
        /// 是否先输出重量
        /// </summary>
        public bool IsOutputWeightFirst { get; set; }
    }
}