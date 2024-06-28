using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Domain.Entities.PackageEntities {

    public class PackageInfoEntities {

        /// <summary>
        /// 数据集合
        /// </summary>
        public List<PackageInfoModel> Infos { get; set; } = new();

        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}