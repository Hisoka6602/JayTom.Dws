using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Dto.CommunicationsSettings;

namespace JayTom.Dws.Domain.Dto {

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

    public enum CommunicationsType {

        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 串口通信类型。
        /// </summary>
        SerialPort,

        /// <summary>
        /// TCP通信类型。
        /// </summary>
        TCP,

        /// <summary>
        /// USB通信类型。
        /// </summary>
        USB,

        /// <summary>
        /// Ethernet通信类型。
        /// </summary>
        Ethernet,

        /// <summary>
        /// CAN总线通信类型。
        /// </summary>
        CAN,

        /// <summary>
        /// SPI通信类型。
        /// </summary>
        SPI,

        /// <summary>
        /// I2C通信类型。
        /// </summary>
        I2C
    }

    public enum CommunicationProtocol {

        /// <summary>
        /// 无通信类型。
        /// </summary>
        None,

        /// <summary>
        /// ModBus 通信类型。
        /// </summary>
        ModBus,

        /// <summary>
        /// CC-Link 通信类型。
        /// </summary>
        CCLink,

        /// <summary>
        /// ProfiBus 通信类型。
        /// </summary>
        ProfiBus,

        /// <summary>
        /// Profinet 通信类型。
        /// </summary>
        Profinet,

        /// <summary>
        /// EtherNet 通信类型。
        /// </summary>
        EtherNet,

        /// <summary>
        /// DeviceNet 通信类型。
        /// </summary>
        DeviceNet,

        /// <summary>
        /// CANopen 通信类型。
        /// </summary>
        CANopen,

        /// <summary>
        /// OPC 通信类型。
        /// </summary>
        OPC,

        /// <summary>
        /// 无限创科协议
        /// </summary>
        Wxkc,
    }
}