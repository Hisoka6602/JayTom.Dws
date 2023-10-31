using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Data_SortingInfo", Schema = "dbo")]
    public class SortingInfoModel : BaseModel {
        public long BarcodeId { get; set; }

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
        [Column("SentCommand")]
        public string SentCommand { get; set; } = string.Empty;

        /// <summary>
        /// 接收的指令
        /// </summary>
        [Column("SentCommand")]
        public string ReceivedCommand { get; set; } = string.Empty;

        /// <summary>
        /// 创建包裹时间
        /// </summary>
        [Column("PackageCreationTime")]
        public DateTime PackageCreationTime { get; set; }

        /// <summary>
        /// 创建包裹指令
        /// </summary>
        [Column("PackageCreationCommand")]
        public string PackageCreationCommand { get; set; } = string.Empty;

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
        public string CommunicationMethod { get; set; } = string.Empty;

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
}