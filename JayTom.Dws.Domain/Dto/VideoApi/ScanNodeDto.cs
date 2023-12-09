namespace JayTom.Dws.Domain.Dto.VideoApi {

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