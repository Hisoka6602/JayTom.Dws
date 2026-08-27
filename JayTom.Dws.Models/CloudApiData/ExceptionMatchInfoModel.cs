using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Collections.Generic;
using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.CloudApiData {

    [Table("Conf_ExceptionMatchInfo", Schema = "dbo")]
    public class ExceptionMatchInfoModel : BaseModel {

        /// <summary>
        /// 包含关键字
        /// </summary>
        [Column("Keywords")]
        public string Keywords { get; set; } = string.Empty;

        /// <summary>
        /// 自定义正则表达式
        /// </summary>
        [Column("CustomRegex")]
        public string CustomRegex { get; set; } = string.Empty;

        /// <summary>
        /// 数据源
        /// </summary>
        [Column("DataSource")]
        public int DataSource { get; set; }

        /// <summary>
        /// 异常类型唯一标识符
        /// </summary>
        [Column("ExceptionTypeId")]
        public long ExceptionTypeId { get; set; }

        [ForeignKey("Id")]
        public virtual ExceptionTypeInfoModel? ExceptionInfo { get; set; }

        /// <summary>
        /// 判断优先级
        /// </summary>
        [Column("Priority")]
        public int Priority { get; set; }
    }
}