using JayTom.Dws.Data.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalData {

    [Table("Conf_ConfigInfo", Schema = "dbo")]
    public class ConfigInfoModel : BaseModel {

        [Column("ConfigName"), Required, UpdateBy]
        public string ConfigName { get; set; } = string.Empty;

        [Column("Value"), Required, InsertOrUpdata]
        public string Value { get; set; } = string.Empty;
    }
}