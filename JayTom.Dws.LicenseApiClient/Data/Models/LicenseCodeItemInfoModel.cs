namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class LicenseCodeItemInfoModel : BaseItemInfoModel {

        /// <summary>
        /// 授权码
        /// </summary>
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 客户端上限数量
        /// </summary>
        public int MaxClientCount { get; set; } = 0;

        /// <summary>
        /// 已激活数量
        /// </summary>
        public int ActivatedClientCount { get; set; } = 0;

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 客户名称/客户信息
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// 机器码
        /// </summary>
        public List<MachineCodeItemInfoModel> MachineCodeItem { get; set; } = new();
    }
}