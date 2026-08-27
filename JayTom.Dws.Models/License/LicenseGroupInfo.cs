using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.License {

    [Table("Code_LicenseGroupInfo", Schema = "dbo")]
    public class LicenseGroupInfo : BaseLicenseModel {

        [Column("GroupName")]
        public string GroupName { get; set; } = string.Empty;

        public ICollection<LicenseCodeInfo>? LicenseCodeInfos { get; set; }
    }
}