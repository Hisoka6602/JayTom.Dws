using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Legacy.Contracts.Dto.CloudApiDto {

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
        public decimal AbnormalSortingRate { get; set; }

        /// <summary>
        /// 平均重量
        /// </summary>
        public decimal AverageWeight { get; set; }

        /// <summary>
        /// 识别率
        /// </summary>
        public decimal RecognitionRate { get; set; }

        /// <summary>
        /// 分拣效率（单位：包裹/时）
        /// </summary>
        public int SortingEfficiency { get; set; }

        /// <summary>
        /// 格口统计信息
        /// </summary>
        public List<StatisticsDto>? ExitStatisticsInfo { get; set; }

        /// <summary>
        /// 异常统计
        /// </summary>
        public List<StatisticsDto>? ErrorStatistics { get; set; }

        /// <summary>
        /// 走势数据
        /// </summary>
        public List<TrendDataInfo>? TrendDataItems { get; set; }
    }

    public class TrendDataInfo {

        /// <summary>
        /// 时间
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 分割单位
        /// </summary>
        public string Unit { get; set; } = "小时";
    }
}