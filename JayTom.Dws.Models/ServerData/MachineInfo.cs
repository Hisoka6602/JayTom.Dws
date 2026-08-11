using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.ServerData {

    [Table("Data_MachineInfo", Schema = "dbo")]
    public class MachineInfo : BaseModel {

        /// <summary>
        /// 机器码
        /// </summary>
        [Required, Column("MachineCode")]
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 授权序列号
        /// </summary>
        [Required, Column("AuthorizationInfoUserId")]
        public long AuthorizationInfoUserId { get; set; }

        /// <summary>
        /// 初次效验时间
        /// </summary>
        [Required, Column("FirstVerificationTime")]
        public DateTime FirstVerificationTime { get; set; }

        /// <summary>
        /// 最后效验时间
        /// </summary>
        [Required, Column("LastVerificationTime")]
        public DateTime LastVerificationTime { get; set; }

        public AuthorizationInfo? AuthorizationInfo { get; set; }
    }
}
