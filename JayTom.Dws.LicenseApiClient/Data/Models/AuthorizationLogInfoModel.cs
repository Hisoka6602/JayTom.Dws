namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class AuthorizationLogInfoModel {
        public int Num { get; set; }
        public string? UserCode { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public OperationType OperationType { get; set; }

        /// <summary>
        /// 消耗的授权码数量
        /// </summary>
        public int ConsumedLicenseCount { get; set; }

        /// <summary>
        /// 操作IP (Operation IP)
        /// </summary>
        public string OperationIp { get; set; } = string.Empty;

        /// <summary>
        /// 操作用户 (Operation User)
        /// </summary>
        public string OperationUser { get; set; } = string.Empty;

        /// <summary>
        /// 扫码器上限
        /// </summary>
        public int MaxBindingScannerCount { get; set; }

        /// <summary>
        /// 客户 (Customer)
        /// </summary>
        public string Customer { get; set; } = string.Empty;
    }

    public enum OperationType {

        /// <summary>
        /// 创建
        /// </summary>
        Created,

        /// <summary>
        /// 修改
        /// </summary>
        Modified
    }
}