using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalLog {
    /// <summary>
    /// 程序运行日志
    /// </summary>
    [Table("Log_AppLogInfo", Schema = "dbo")]
    public class AppLogInfoModel : BaseLogInfoModel {

    }
}
