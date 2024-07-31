using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CloudApiDto {

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
        public double Percentage { get; set; }

        /// <summary>
        /// 整体Item占比
        /// </summary>
        public double OverallPercentage { get; set; }
    }
}