namespace JayTom.Dws.CloudApiClient.Data.Models {

    public class StatisticsInfoModel {

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
        public int SortingEfficiency { get; set; }

        /// <summary>
        /// 格口统计信息
        /// </summary>
        public List<StatisticsInfoItemInfo>? ExitStatisticsInfo { get; set; }

        /// <summary>
        /// 异常统计
        /// </summary>
        public List<StatisticsInfoItemInfo>? ErrorStatistics { get; set; }
    }

    public class StatisticsInfoItemInfo {

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
    }
}