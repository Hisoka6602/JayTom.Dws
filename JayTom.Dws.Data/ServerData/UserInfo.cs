using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.ServerData {

    [Table("Data_UserInfo", Schema = "dbo")]
    public class UserInfo : BaseModel {

        /// <summary>
        /// Pid
        /// </summary>
        [Required, Column("Pid")]
        public int Pid { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Required, Column("UserName")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 公司名称
        /// </summary>
        [Column("CompanyName"), MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// 公司Emaill
        /// </summary>
        [Column("CompanyEmail"), MaxLength(100)]
        public string CompanyEmail { get; set; } = string.Empty;

        /// <summary>
        /// 公司地址
        /// </summary>
        [MaxLength(500), Column("CompanyAddress")]
        public string CompanyAddress { get; set; } = string.Empty;

        /// <summary>
        /// 联系人
        /// </summary>
        [MaxLength(100), Column("ContactPerson")]
        public string ContactPerson { get; set; } = string.Empty;

        /// <summary>
        /// 联系电话
        /// </summary>
        [MaxLength(20), Column("ContactPhone")]
        public string ContactPhone { get; set; } = string.Empty;

        // 导航属性，建立与授权信息表的一对多关系
        public ICollection<AuthorizationInfo>? AuthorizationInfos { get; set; }
    }
}