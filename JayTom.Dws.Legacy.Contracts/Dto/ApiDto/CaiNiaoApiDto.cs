using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.ApiDto {

    public class CaiNiaoApiDto {

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = "http://10.220.64.463:10002/ucs/api";

        /// <summary>
        /// 超时
        /// </summary>
        public int TimeOut { get; set; } = 1000;

        /// <summary>
        /// SignKey
        /// </summary>
        public string Source { get; set; } = "test";

        /// <summary>
        /// 版本
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 设备代码
        /// </summary>
        public string BcrCode { get; set; } = "BCR02";

        /// <summary>
        /// 设备名称
        /// </summary>
        public string BcrName { get; set; } = "sorter";
    }
}