using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JayTom.Dws.Data.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Data.LocalConf.CameraConfig {

    [Table("Conf_ScanNodeConfigInfo", Schema = "dbo")]
    public class ScanNodeConfigInfoModel : BaseModel {

        /// <summary>
        /// 地址
        /// </summary>
        [Column("IpAddress"), Required]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        [Column("Port"), Required]
        public int Port { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        [Column("NodeName"), Required]
        public string NodeName { get; set; } = string.Empty;

        /// <summary>
        /// 节点序号
        /// </summary>
        [Column("NodeNum"), Required]
        public int NodeNum { get; set; }

        /// <summary>
        /// 等待赋值超时时间
        /// </summary>
        [Column("Timeout"), Required]
        public int Timeout { get; set; }

        /// <summary>
        /// 存图路径
        /// </summary>
        [Column("ImagePath"), Required]
        public string ImagePath { get; set; } = string.Empty;
    }
}