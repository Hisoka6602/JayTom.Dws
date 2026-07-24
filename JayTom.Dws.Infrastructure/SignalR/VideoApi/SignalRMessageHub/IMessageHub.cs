namespace JayTom.Dws.Infrastructure.SignalR.VideoApi.SignalRMessageHub {

    public interface IMessageHub {

        /// <summary>
        /// 数据汇总
        /// </summary>
        Task DataStatistics();

        /// <summary>
        /// 添加或更新一行
        /// </summary>
        Task MessageItem(MessageBarCodeItemInfo info);

        /// <summary>
        /// 更新节点
        /// </summary>
        Task UpDateNodes();
    }

    public class DataStatistics {

        /// <summary>
        /// 今天条码总数
        /// </summary>
        public int TodayBarcodeTotal { get; set; }

        /// <summary>
        /// 昨天条码总数
        /// </summary>
        public int YesterdayBarcodeTotal { get; set; }

        /// <summary>
        /// 本月条码总数
        /// </summary>
        public int ThisMonthBarcodeTotal { get; set; }

        /// <summary>
        /// 上月条码总数
        /// </summary>
        public int LastMonthBarcodeTotal { get; set; }
    }

    public class MessageBarCodeItemInfo {

        /// <summary>
        /// 条码
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string? NodeName { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string? CameraSerialNumber { get; set; }

        /// <summary>
        /// 相机自定义名称
        /// </summary>
        public string? CameraCustomName { get; set; }

        /// <summary>
        /// 扫码图片地址
        /// </summary>
        public string? ScanImageUrl { get; set; }

        /// <summary>
        /// 全景图
        /// </summary>
        public List<string> PanoramaImageItems { get; set; } = new();

        /// <summary>
        /// 视频通道
        /// </summary>
        public List<MessageNvrCameraBindingItemInfo> NvrCameraBindingItem { get; set; } = new();
    }

    public class MessageNvrCameraBindingItemInfo {

        /// <summary>
        /// Ip地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 通道
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// 扫码相机序列号
        /// </summary>
        public string BarcodeScannerSerialNumber { get; set; } = string.Empty;
    }
}
