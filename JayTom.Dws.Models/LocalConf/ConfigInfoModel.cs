using JayTom.Dws.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.LocalConf {

    [Table("Conf_ConfigInfo", Schema = "dbo")]
    public class ConfigInfoModel : BaseModel {

        [Column("ConfigName"), Required, UpdateBy]
        public string ConfigName { get; set; } = string.Empty;

        [Column("Value"), Required, InsertOrUpdate]
        public string Value { get; set; } = string.Empty;
    }
}