using JayTom.Dws.Data.Package;

namespace JayTom.Dws.CloudApi.Do {

    public class PackagesDo : BasePageDo {

        /// <summary>
        /// 条码
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// 开始扫码时间
        /// </summary>
        public DateTime? StartScanTime { get; set; }

        /// <summary>
        /// 结束扫码时间
        /// </summary>
        public DateTime? EndScanTime { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string? CameraSerialNumber { get; set; }

        /// <summary>
        /// 最小重量
        /// </summary>
        public double? MinWeight { get; set; }

        /// <summary>
        /// 最大重量
        /// </summary>
        public double? MaxWeight { get; set; }

        /// <summary>
        /// 上传状态
        /// </summary>
        public int? RequestStatus { get; set; }

        /// <summary>
        /// 物理格口
        /// </summary>
        public string? PhysicalExit { get; set; }

        /// <summary>
        /// 发送的指令
        /// </summary>
        public string? SentInstruction { get; set; }

        /// <summary>
        /// 物流名称
        /// </summary>
        public string? LogisticsName { get; set; }

        /// <summary>
        /// 三段码
        /// </summary>
        public string? ThreeSegmentCode { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string? NodeName { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? DeviceName { get; set; }
    }
}