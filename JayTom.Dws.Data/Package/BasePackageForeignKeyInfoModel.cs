using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    public class BasePackageForeignKeyInfoModel : BaseModel {

        [Column("PackageId"), JsonIgnore]
        public long PackageId { get; set; }

        [ForeignKey("Id")]
        public virtual PackageInfoModel? PackageInfo { get; set; }
    }
}