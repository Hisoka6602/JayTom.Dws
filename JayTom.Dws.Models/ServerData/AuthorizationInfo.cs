using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.ServerData {

    [Table("Data_AuthorizationInfo", Schema = "dbo")]
    public class AuthorizationInfo : BaseModel {

        /// <summary>
        /// 用户Id
        /// </summary>
        [Required, Column("UserId")]
        public long UserId { get; set; }

        /// <summary>
        /// 验证序列号
        /// </summary>
        [Required, Column("Signature")]
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// 剩余绑定机器数量
        /// </summary>

        [Required, Column("RemainingDevices")]
        public int RemainingDevices { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        [Required, Column("ExpirationDate")]
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 授权公钥
        /// </summary>
        [Required, Column("AuthorizationPublicKey")]
        public string AuthorizationPublicKey { get; set; } = string.Empty;

        /// <summary>
        /// 授权私钥
        /// </summary>
        [Required, Column("AuthorizationPrivateKey")]
        public string AuthorizationPrivateKey { get; set; } = string.Empty;

        public UserInfo? UserInfo { get; set; }
        public ICollection<MachineInfo>? MachineInfos { get; set; }
    }
}
