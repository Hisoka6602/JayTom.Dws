using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.CloudApiData {

    [Table("Conf_ExceptionTypeInfo", Schema = "dbo")]
    public class ExceptionTypeInfoModel : BaseModel {

        /// <summary>
        /// 异常名称
        /// </summary>
        [Column("ExceptionName"), Required, UpdateBy]
        public string ExceptionName { get; set; } = string.Empty;

        /// <summary>
        /// 异常颜色
        /// </summary>
        [Column("ExceptionColor"), Required]
        public string ExceptionColor { get; set; } = string.Empty;

        /// <summary>
        /// 匹配信息
        /// </summary>
        public virtual ICollection<ExceptionMatchInfoModel>? ExceptionMatchInfos { get; set; }
    }
}