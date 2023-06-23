using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf {

    [Table("Conf_RequestParametersInfo", Schema = "dbo")]
    public class RequestParametersInfoModel : BaseModel {

        /// <summary>
        /// 接口名称
        /// </summary>
        [Column("InterfaceName"), Required]
        public string InterfaceName { get; set; } = string.Empty;

        /// <summary>
        /// 参数Json
        /// </summary>
        [Column("ParametersJson"), Required]
        public string ParametersJson { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreateTime"), Required]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("UpdateTime")]
        public DateTime? UpdateTime { get; set; }
    }
}