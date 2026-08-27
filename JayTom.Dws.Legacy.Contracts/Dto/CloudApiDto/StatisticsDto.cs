using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.CloudApiDto {

    public class StatisticsDto {

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 占比
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// 整体Item占比
        /// </summary>
        public decimal OverallPercentage { get; set; }
    }
}