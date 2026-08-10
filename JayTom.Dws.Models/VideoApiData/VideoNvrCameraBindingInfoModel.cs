using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.VideoApiData {

    [Table("Data_VideoNvrCameraBindingInfo", Schema = "dbo")]
    public class VideoNvrCameraBindingInfoModel : NvrCameraBindingInfoModel {

        [Column("ScanNodeId"), Required]
        public long ScanNodeId { get; set; }

        [ForeignKey("Id")]
        public virtual VideoScanNodeInfoModel? ScanNodeInfo { get; set; }
    }
}