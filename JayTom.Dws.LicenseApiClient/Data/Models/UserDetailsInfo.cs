namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class UserDetailsInfo {

        /// <summary>
        /// 公司名称
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// 公司地址
        /// </summary>
        public string CompanyAddress { get; set; } = string.Empty;

        /// <summary>
        /// 联系邮箱
        /// </summary>
        public string ContactEmail { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 合同文件路径
        /// </summary>
        public string ContractFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 营业执照文件路径
        /// </summary>
        public string BusinessLicenseFilePath { get; set; } = string.Empty;
    }
}