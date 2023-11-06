using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalLog {
    [Table("Log_ApiLogInfo", Schema = "dbo")]
    public class ApiLogInfoModel : BaseLogInfoModel {
        //直接写UploadResponse的Json
    }
}
