using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Application.Dto.VideoApi {

    public class ScanNodeDto {

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string ScanNodName { get; set; } = string.Empty;

        /// <summary>
        /// 节点扫描时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}