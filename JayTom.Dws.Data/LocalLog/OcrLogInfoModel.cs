using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {

    [Table("Log_OcrLogInfo", Schema = "dbo")]
    public class OcrLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 提交的信息
        /// </summary>
        [Column("SubmitContent")]
        public string SubmitContent { get; set; } = string.Empty;

        /// <summary>
        /// 返回的信息
        /// </summary>
        [Column("ResponseContent")]
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 三段码信息
        /// </summary>
        [Column("OcrCode")]
        public string OcrCode { get; set; } = string.Empty;

        /// <summary>
        /// 接口信息
        /// </summary>
        [Column("InterfaceInfo")]
        public string InterfaceInfo { get; set; } = string.Empty;
    }
}