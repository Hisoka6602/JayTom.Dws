using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CloudApiDto {

    public class PackageStatisticsDto {

        /// <summary>
        /// 包裹总数
        /// </summary>
        public int TotalPackages { get; set; }

        /// <summary>
        /// 正常分拣
        /// </summary>
        public int NormalSortingCount { get; set; }

        /// <summary>
        /// 异常分拣
        /// </summary>
        public int AbnormalSortingCount { get; set; }

        /// <summary>
        /// 异常分拣占比
        /// </summary>
        public double AbnormalSortingRate { get; set; }

        /// <summary>
        /// 平均重量
        /// </summary>
        public double AverageWeight { get; set; }

        /// <summary>
        /// 识别率
        /// </summary>
        public double RecognitionRate { get; set; }

        /// <summary>
        /// 分拣效率（单位：包裹/时）
        /// </summary>
        public double SortingEfficiency { get; set; }

        /// <summary>
        /// 格口统计信息
        /// </summary>
        public List<StatisticsDto>? ExitStatisticsInfo { get; set; }
    }
}