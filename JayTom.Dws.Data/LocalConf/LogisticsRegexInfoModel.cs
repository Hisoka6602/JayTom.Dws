using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf {

    [Table("Conf_LogisticsRegexInfo", Schema = "dbo")]
    public class LogisticsRegexInfoModel : BaseModel {

        [Column("LogisticsCode"), Required]
        public string LogisticsCode { get; set; } = string.Empty;

        [Column("RegexPattern"), Required]
        public string RegexPattern { get; set; } = string.Empty;

        [Column("Replacement"), Required]
        public string Replacement { get; set; } = string.Empty;
    }
}