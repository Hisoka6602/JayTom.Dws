using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.Package {

    public class BasePackageForeignKeyInfoModel : BaseModel {

        [Column("PackageId"), JsonIgnore]
        public long PackageId { get; set; }

        [ForeignKey(nameof(PackageId))]
        public virtual PackageInfoModel? PackageInfo { get; set; }
    }
}
