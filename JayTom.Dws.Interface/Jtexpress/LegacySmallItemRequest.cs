namespace JayTom.Dws.Interface.Jtexpress {

    /// <summary>
    /// 极昼旧版小件回传请求。
    /// </summary>
    internal sealed class LegacySmallItemRequest {
        /// <summary>
        /// 运单号。
        /// </summary>
        public string WaybillNo { get; set; } = string.Empty;

        /// <summary>
        /// 扫描网点编码。
        /// </summary>
        public string NetworkCode { get; set; } = string.Empty;

        /// <summary>
        /// 供包时间。
        /// </summary>
        public string ScanTime { get; set; } = string.Empty;

        /// <summary>
        /// 供包台登录人账号。
        /// </summary>
        public string UserNum { get; set; } = string.Empty;

        /// <summary>
        /// 重量，单位千克。
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// 总长，单位厘米。
        /// </summary>
        public decimal? Length { get; set; }

        /// <summary>
        /// 总宽，单位厘米。
        /// </summary>
        public decimal? Wide { get; set; }

        /// <summary>
        /// 总高，单位厘米。
        /// </summary>
        public decimal? High { get; set; }

        /// <summary>
        /// 供件扫描识别结果。
        /// </summary>
        public int UploadResult { get; set; }

        /// <summary>
        /// 交叉带 MAC 地址。
        /// </summary>
        public string CrossBeltMac { get; set; } = string.Empty;

        /// <summary>
        /// 供件台编码。
        /// </summary>
        public string SupplyDeskCode { get; set; } = string.Empty;

        /// <summary>
        /// 供件台 MAC 地址。
        /// </summary>
        public string SupplyDeskMac { get; set; } = string.Empty;

        /// <summary>
        /// 实际上传时间。
        /// </summary>
        public string UploadTime { get; set; } = string.Empty;

        /// <summary>
        /// 分拣方案编码。
        /// </summary>
        public string SortingPlanCode { get; set; } = string.Empty;

        /// <summary>
        /// 操作模式。
        /// </summary>
        public int OperateType { get; set; }

        /// <summary>
        /// 设备编号。
        /// </summary>
        public string EquipmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 设备层数。
        /// </summary>
        public int EquipmentLayer { get; set; }

        /// <summary>
        /// 实际落格格口号。
        /// </summary>
        public string GridNo { get; set; } = string.Empty;

        /// <summary>
        /// 落格包号。
        /// </summary>
        public string? PackageNo { get; set; }

        /// <summary>
        /// 落格时间。
        /// </summary>
        public string FallTime { get; set; } = string.Empty;

        /// <summary>
        /// 格口下一站或目的地。
        /// </summary>
        public string? NextStation { get; set; }

        /// <summary>
        /// 循环圈数。
        /// </summary>
        public decimal CyclesNum { get; set; }

        /// <summary>
        /// 小车号。
        /// </summary>
        public string CarNum { get; set; } = string.Empty;

        /// <summary>
        /// 格口编码类型。
        /// </summary>
        public string GridCode { get; set; } = string.Empty;

        /// <summary>
        /// RFID 芯片号。
        /// </summary>
        public string? Rfid { get; set; }

        /// <summary>
        /// 三段码。
        /// </summary>
        public string? ThirdCode { get; set; }

        /// <summary>
        /// 建包员编号。
        /// </summary>
        public string? BagUserCode { get; set; }
    }
}
