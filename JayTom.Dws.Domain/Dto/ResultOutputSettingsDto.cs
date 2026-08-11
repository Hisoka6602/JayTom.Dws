using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Abstractions.Devices;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

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
        /// 是否使用串口输出
        /// </summary>
        public bool IsUseSerialOutput { get; set; }

        /// <summary>
        /// 串口输出配置
        /// </summary>
        public SerialPortSettingsInfo SerialPortSettingsInfo { get; set; } = new();

        /// <summary>
        /// 串口输出内容
        /// </summary>
        public SerialPortResultOutputInfo SerialPortResultOutputInfo { get; set; } = new();

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
    /// 串口输出内容
    /// </summary>
    public class SerialPortResultOutputInfo {

        /// <summary>
        /// 是否使用数据模板输出
        /// </summary>
        public bool IsUseDataTemplateOutput { get; set; }

        /// <summary>
        /// 是否使用自定义内容输出
        /// </summary>
        public bool IsUseCustomContentOutput { get; set; }

        /// <summary>
        /// 自定义内容
        /// </summary>
        public string CustomOutputContent { get; set; } = string.Empty;
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
    /// 音频输出
    /// </summary>
    public class AudioOutputSettingsInfo {

        /// <summary>
        /// 成功音频
        /// </summary>
        public string? SuccessAudio { get; set; }

        /// <summary>
        /// 失败音频
        /// </summary>
        public string? FailureAudio { get; set; }

        /// <summary>
        /// 触发位置
        /// </summary>
        public TriggerPositionEnum TriggerPosition { get; set; }

        /// <summary>
        /// 结果判断
        /// </summary>
        public ResultEnum Result { get; set; }
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
        PackageTrigger,

        /// <summary>
        /// 包裹信息赋值完成后
        /// </summary>
        PackageInfoAssigned,

        /// <summary>
        /// 包裹扫描后
        /// </summary>
        PackageScan,

        /// <summary>
        /// 创建包裹后
        /// </summary>
        CreateTimePackageAfter,

        /// <summary>
        /// 发送前置信号
        /// </summary>
        SendingPreSignalBefore,

        /// <summary>
        /// 移除包裹后
        /// </summary>
        RemovePackageAfter,

        /// <summary>
        /// 条码赋值
        /// </summary>
        BarCodeSetValueAfter,

        /// <summary>
        /// 重量赋值
        /// </summary>
        WeightSetValueAfter,

        /// <summary>
        /// 体积赋值
        /// </summary>
        VolumeSetValueAfter,

        /// <summary>
        /// 外部数据输入后
        /// </summary>
        ExternalDataInputAfter,

        /// <summary>
        /// 扫码枪返回
        /// </summary>
        BarcodeScannerReturn
    }

    /// <summary>
    /// 位置输出
    /// </summary>
    public class LocationOutputSettingsInfo {

        /// <summary>
        /// 条码输出位置
        /// </summary>
        public Point2D BarcodeOutputPosition { get; set; }

        /// <summary>
        /// 重量输出位置
        /// </summary>
        public Point2D WeightOutputPosition { get; set; }

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

        /// <summary>
        /// 是否输出条码
        /// </summary>
        public bool IsOutputBarcode { get; set; }

        /// <summary>
        /// 是否输出重量
        /// </summary>
        public bool IsOutputWeight { get; set; }
    }

    public enum ResultEnum {

        /// <summary>
        /// Api响应
        /// </summary>
        ApiResponse,

        /// <summary>
        /// http输出
        /// </summary>
        HttpOutputResponse,

        /// <summary>
        /// 包裹识别
        /// </summary>
        PackageRecognition,

        /// <summary>
        /// 无
        /// </summary>
        NotSet
    }
}
