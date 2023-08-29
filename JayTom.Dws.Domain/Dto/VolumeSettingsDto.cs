using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;

namespace JayTom.Dws.Domain.Dto {

    public class VolumeSettingsDto {

        /// <summary>
        /// 数据模板
        /// </summary>
        public List<ItemTemplateInfo> DataTemplate { get; set; } = new();

        /// <summary>
        /// 是否使用Tcp输入
        /// </summary>
        public bool IsUseTcpInput { get; set; }

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo { get; set; } = new();

        /// <summary>
        /// 是否主动触发体积获取
        /// </summary>
        public bool TriggerVolumeRequest { get; set; }
    }

    public class VolumeInformationRequesterInfo {

        /// <summary>
        /// 触发位置
        /// </summary>
        public VolumeTriggerPosition VolumeTriggerPosition { get; set; } = VolumeTriggerPosition.None;

        /// <summary>
        /// 发送内容
        /// </summary>
        public string SendContent { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用Tcp发送
        /// </summary>
        public bool IsUseTcpSend { get; set; }

        /// <summary>
        /// 是否使用串口发送
        /// </summary>
        public bool IsUseSerialPortSend { get; set; }

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
}