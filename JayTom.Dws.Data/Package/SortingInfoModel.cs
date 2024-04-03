using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_SortingInfo", Schema = "dbo")]
    public class SortingInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 是否使用分拣
        /// </summary>
        [Column("IsSortingUsed")]
        public bool IsSortingUsed { get; set; }

        /// <summary>
        /// 分拣编码/分拣流水号
        /// </summary>
        [Column("SortingCode")]
        public string SortingCode { get; set; } = string.Empty;

        /// <summary>
        /// 分拣模式
        /// </summary>
        [Column("SortingMode")]
        public SortMode SortingMode { get; set; }

        /// <summary>
        /// 是否有下位机创建
        /// </summary>
        [Column("IsCreatedByLowerMachine")]
        public bool IsCreatedByLowerMachine { get; set; }

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

        /// <summary>
        /// 连接名称
        /// </summary>
        [Column("ConnectionName")]
        public string ConnectionName { get; set; } = string.Empty;

        /// <summary>
        /// 是否异常分拣
        /// </summary>
        [Column("IsAbnormalSorting")]
        public bool IsAbnormalSorting { get; set; }

        /// <summary>
        /// 异常分拣类型
        /// </summary>
        [Column("AbnormalSortingType")]
        public AbnormalSortingType AbnormalSortingType { get; set; }

        public virtual ICollection<InstructionInfoModel>? InstructionInfos { get; set; }
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

        /// <summary>
        /// 菜鸟分拣协议
        /// </summary>
        CaiNiao,
    }

    public enum AbnormalSortingType {

        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 网络超时
        /// </summary>
        NetworkTimeout,

        /// <summary>
        /// Api异常访问
        /// </summary>
        ApiAccessError,

        /// <summary>
        /// 无条码
        /// </summary>
        NoRead,

        /// <summary>
        /// 多条码识别
        /// </summary>
        MultipleBarCode,

        /// <summary>
        /// 无分拣指令
        /// </summary>
        NoSortingInstruction,

        /// <summary>
        /// 无物理格口(无适应规则)
        /// </summary>
        NoPhysicalMailbox,

        /// <summary>
        /// 锁格
        /// </summary>
        LockExit,

        /// <summary>
        /// 叠包
        /// </summary>
        StackedPackage,

        /// <summary>
        /// 非本机构条码
        /// </summary>
        PostNonLocalBarcode,

        /// <summary>
        /// 查不到段道
        /// </summary>
        PostSegmentNotFound,

        /// <summary>
        /// 未命中规则
        /// </summary>
        UnmatchedRule,
    }
}