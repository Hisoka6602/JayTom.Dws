namespace JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub {

    public interface IMessageHub {
        public List<DataSummary> DataSummaries { get; }

        //发送汇总数据
        void SendTotalCount();

        /// <summary>
        /// 添加一个计数
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        void AddTotalCount(DateTime date);

        /// <summary>
        /// 重置
        /// </summary>
        /// <returns></returns>
        Task<bool> ResetDataSummaries();
    }

    public class DataSummary {
        public DateTime Date { get; set; }
        public int TotalCount { get; set; }
    }
}