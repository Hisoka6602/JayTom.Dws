using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_NvrInfo", Schema = "dbo")]
    public class NvrInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// IP地址
        /// </summary>
        [Column("IpAddress"), Required]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        [Column("Port"), Required]
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Column("Username"), Required]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Column("Password"), Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 通道
        /// </summary>
        [Column("Channel"), Required]
        public int Channel { get; set; }
    }
}