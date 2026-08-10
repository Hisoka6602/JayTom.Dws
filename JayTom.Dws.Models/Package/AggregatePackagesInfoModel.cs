using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    /// <summary>
    /// 聚合包裹信息(大包裹)
    /// </summary>
    public class AggregatePackagesInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 聚合包裹码
        /// </summary>
        [Column("AggregatePackageCode")]
        public string AggregatePackageCode { get; set; } = string.Empty;

        /// <summary>
        /// 打包时间
        /// </summary>
        [Column("PackagingTime")]
        public DateTime PackagingTime { get; set; }
    }
}