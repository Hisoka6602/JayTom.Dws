namespace JayTom.Dws.CloudApiClient.Api {

    public interface ICloudApiRequest {

        /// <summary>
        /// 设置地址
        /// </summary>
        /// <param name="url"></param>
        void SetBaseUrl(string url);

        /// <summary>
        /// 获取统计数据
        /// </summary>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <param name="deviceName"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> GetStatistics(DateTime? startDateTime, DateTime? endDateTime, string? deviceName, CancellationToken token = default);

        /// <summary>
        /// 获取数据列表
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="startScanTime"></param>
        /// <param name="endScanTime"></param>
        /// <param name="cameraSerialNumber"></param>
        /// <param name="minWeight"></param>
        /// <param name="maxWeight"></param>
        /// <param name="requestStatus"></param>
        /// <param name="physicalExit"></param>
        /// <param name="sentInstruction"></param>
        /// <param name="logisticsName"></param>
        /// <param name="threeSegmentCode"></param>
        /// <param name="nodeName"></param>
        /// <param name="deviceName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<KeyValuePair<bool, object>> GetPackages(string? barcode,
            DateTime? startScanTime, DateTime? endScanTime, string? cameraSerialNumber,
            double? minWeight, double? maxWeight, int? requestStatus,
            string? physicalExit, string? sentInstruction, string? logisticsName,
            string? threeSegmentCode, string? nodeName, string? deviceName,
             int pageIndex = 0, int pageSize = 1000,
            CancellationToken token = default);
    }

    public class ApiResult {
        public bool Result { get; set; }
        public object? Data { get; set; }
        public string Msg { get; set; } = string.Empty;
        public int Total { get; set; }
    }
}