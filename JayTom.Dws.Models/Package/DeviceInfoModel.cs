using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Models.Package {

    [Table("Data_DeviceInfo", Schema = "dbo")]
    public class DeviceInfoModel : BasePackageForeignKeyInfoModel {

        /// <summary>
        /// 机器码
        /// </summary>
        [Column("MachineCode")]
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        [Column("DeviceName")]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 节点名称
        /// </summary>
        [Column("NodeName")]
        public string NodeName { get; set; } = string.Empty;
    }
}