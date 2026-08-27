using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalLog {

    [Table("Log_CameraLogInfo", Schema = "dbo")]
    public class CameraLogInfoModel : BaseLogInfoModel {

        /// <summary>
        /// 相机序列号
        /// </summary>
        [Column("CameraSerialNumber")]
        public string CameraSerialNumber { get; set; } = string.Empty;
    }
}