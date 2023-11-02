using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_SortingInfo", Schema = "dbo")]
    public class SortingInfoModel : BaseBarCodeForeignKeyInfo {

        /// <summary>
        /// 是否使用分拣
        /// </summary>
        [Column("IsSortingUsed")]
        public bool IsSortingUsed { get; set; }

        /// <summary>
        /// 格口Id
        /// </summary>
        [Column("ExitId")]
        public long ExitId { get; set; }

        /// <summary>
        /// 物流Id
        /// </summary>
        [Column("LogisticsId")]
        public long LogisticsId { get; set; }

        /// <summary>
        /// 分拣模式
        /// </summary>
        [Column("SortingMode")]
        public SortMode SortingMode { get; set; }

        /// <summary>
        /// 发送的指令
        /// </summary>
        [Column("SentInstruction")]
        public string SentInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 接收的指令
        /// </summary>
        [Column("SentInstruction")]
        public string ReceivedInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 创建包裹时间
        /// </summary>
        [Column("PackageCreationTime")]
        public DateTime PackageCreationTime { get; set; }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        [Column("PackageCreationInstruction")]
        public string PackageCreationInstruction { get; set; } = string.Empty;

        /// <summary>
        /// 是否有下位机创建
        /// </summary>
        [Column("IsCreatedByLowerMachine")]
        public bool IsCreatedByLowerMachine { get; set; }

        /// <summary>
        /// 指令目标
        /// </summary>
        [Column("CommandTarget")]
        public string CommandTarget { get; set; } = string.Empty;

        /// <summary>
        /// 通讯方式
        /// </summary>
        [Column("CommunicationMethod")]
        public CommunicationsType CommunicationMethod { get; set; } = CommunicationsType.None;

        /// <summary>
        /// 效验协议名称
        /// </summary>
        [Column("ChecksumProtocolName")]
        public string ChecksumProtocolName { get; set; } = string.Empty;
    }

    public enum SortMode {

        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 条码分拣
        /// </summary>
        BarcodeSorting,

        /// <summary>
        /// 重量分拣
        /// </summary>
        WeightSorting,

        /// <summary>
        /// 体积分拣
        /// </summary>
        VolumeSorting,

        /// <summary>
        /// 物流分拣
        /// </summary>
        LogisticsSorting,

        /// <summary>
        /// Ocr分拣
        /// </summary>
        OcrSorting,

        /// <summary>
        /// Api分拣
        /// </summary>
        ApiResponseSorting,

        /// <summary>
        /// 组合工作流分拣
        /// </summary>
        CombinedWorkflowSorting
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

        /// <summary>
        /// 江腾窄带
        /// </summary>
        JT_ST,
    }
}