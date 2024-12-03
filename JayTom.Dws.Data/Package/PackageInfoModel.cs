using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.Package {

    [Table("Data_PackageInfo", Schema = "dbo")]
    public class PackageInfoModel : BaseModel {

        /// <summary>
        /// 包裹时间戳Id
        /// </summary>
        [Column("PackageTimestamped"), Required]
        public long PackageTimestamped { get; set; }

        /// <summary>
        /// 包裹创建时间
        /// </summary>
        [Column("PackageCreateTime"), Required]
        public DateTime PackageCreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 上传状态
        /// </summary>
        [Column("RequestStatus")]
        public UploadStatus RequestStatus { get; set; } = UploadStatus.NotUploaded;

        /// <summary>
        /// 条码信息
        /// </summary>
        public virtual BarCodeInfoModel? BarCodeInfo { get; set; }

        /// <summary>
        /// 格口信息
        /// </summary>
        public virtual ExitInfoModel? ExitInfo { get; set; }

        /// <summary>
        /// 分拣信息
        /// </summary>
        public virtual SortingInfoModel? SortingInfo { get; set; }

        /// <summary>
        /// 节点信息
        /// </summary>
        public virtual ICollection<NodeInfoModel>? NodeInfos { get; set; }

        /// <summary>
        /// Api信息
        /// </summary>
        public virtual ICollection<ApiInfoModel>? ApiInfos { get; set; }

        /// <summary>
        /// 视频云信息
        /// </summary>
        public virtual CloudVideoUploadInfoModel? CloudVideoUploadInfo { get; set; }

        /// <summary>
        /// 聚合包裹信息
        /// </summary>
        public virtual AggregatePackagesInfoModel? AggregatePackagesInfo { get; set; }
    }
}