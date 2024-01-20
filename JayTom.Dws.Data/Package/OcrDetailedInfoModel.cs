using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    public class OcrDetailedInfoModel : BaseModel {

        [Column("OcrInfoId")]
        public long OcrInfoId { get; set; }

        [ForeignKey("Id")]
        public virtual OcrInfoModel? OcrInfo { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 地址
        /// </summary>
        [Column("Address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 电话
        /// </summary>
        [Column("Phone")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// 信息类型(收件人信息、发件人信息)
        /// </summary>
        [Column("InfoType")]
        public InfoType InformationType { get; set; }
    }

    /// <summary>
    /// 信息类型
    /// </summary>
    public enum InfoType {

        /// <summary>
        /// 收件人信息
        /// </summary>
        RecipientInfo,

        /// <summary>
        /// 发件人信息
        /// </summary>
        SenderInfo
    }
}