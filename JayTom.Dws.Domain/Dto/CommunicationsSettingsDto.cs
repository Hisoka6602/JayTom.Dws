using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Dto.CommunicationsSettings;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Domain.Dto
{

    public class CommunicationsSettingsDto {

        //Usb通讯参数
        //CAN总线通讯
        //Ethernet通讯
        //SPI通讯
        //I2C通讯
        /// <summary>
        /// Tcp通讯参数
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo { get; set; } = new();

        /// <summary>
        /// 串口通讯参数
        /// </summary>
        public SerialPortSettingsInfo SerialPortSettingsInfo { get; set; } = new();

        /// <summary>
        /// 通讯协议
        /// </summary>
        public CommunicationProtocol Protocol { get; set; } = CommunicationProtocol.None;

        /// <summary>
        /// 通讯类型
        /// </summary>
        public CommunicationsType Type { get; set; } = CommunicationsType.None;

        /// <summary>
        /// 下位机回复
        /// </summary>
        public MachineReplyInfo MachineReplyInfo { get; set; } = new();

        /// <summary>
        /// 心跳包
        /// </summary>
        public HeartbeatInfo HeartbeatInfo { get; set; } = new();

        /// <summary>
        /// 下位机设置
        /// </summary>
        public DeviceControlSettingsInfo DeviceControlSettingsInfo { get; set; } = new();

        /// <summary>
        /// 是否使用包裹过期
        /// </summary>
        public bool IsUsePackageExpiry { get; set; }

        /// <summary>
        /// 包裹过期时间(设置为0则不验证)
        /// </summary>
        public int PackageExpiryTime { get; set; }
    }
}