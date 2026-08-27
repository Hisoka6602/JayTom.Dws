using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;

namespace JayTom.Dws.Legacy.Contracts.Dto {

    public class VolumeSettingsDto {

        /// <summary>
        /// 单位
        /// </summary>
        public VolumeUnit Unit { get; set; } = VolumeUnit.Millimeter;

        /// <summary>
        /// 数据模板
        /// </summary>
        public List<ItemTemplateInfo> DataTemplate { get; set; } = new();

        /// <summary>
        /// 分隔符
        /// </summary>
        public string Separator { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用外部体积输入
        /// </summary>
        public bool IsUseExternalVolumeInput { get; set; }

        /// <summary>
        /// 是否主动触发体积获取
        /// </summary>
        public bool IsTriggerVolumeRequest { get; set; }

        /// <summary>
        /// 是否使用融合超时
        /// </summary>
        public bool IsUseFusionTimeout { get; set; }

        /// <summary>
        /// 融合超时时间
        /// </summary>
        public int FusionTimeout { get; set; }

        /// <summary>
        /// 发送参数
        /// </summary>
        public VolumeInformationRequesterInfo VolumeInformationRequesterInfo { get; set; } = new();

        /// <summary>
        /// 触发延迟(毫秒)
        /// </summary>
        public int TriggerDelayMilliseconds { get; set; } = 100;
    }

    public class VolumeInformationRequesterInfo {

        /// <summary>
        /// 触发位置
        /// </summary>
        public VolumeTriggerPosition VolumeTriggerPosition { get; set; } = VolumeTriggerPosition.None;

        /// <summary>
        /// 发送延迟（单位：毫秒）
        /// </summary>
        public int SendDelay { get; set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string SendContent { get; set; } = string.Empty;

        /// <summary>
        /// 发送次数
        /// </summary>
        public int SendCount { get; set; }

        /// <summary>
        /// 发送间隔
        /// </summary>
        public int SendInterval { get; set; }

        /// <summary>
        /// 发送模式
        /// </summary>
        public VolumeRequesterType VolumeRequesterType { get; set; }

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo { get; set; } = new();

        /// <summary>
        /// 串口设置
        /// </summary>
        public SerialPortSettingsInfo SerialPortSettingsInfo { get; set; } = new();
    }

    public enum VolumeTriggerPosition {

        /// <summary>
        /// 扫码后
        /// </summary>
        BarcodeDetected,

        /// <summary>
        /// 称重后
        /// </summary>
        WeightObtained,

        /// <summary>
        /// 无
        /// </summary>
        None
    }

    public enum VolumeRequesterType {
        Tcp,
        SerialPort
    }
}