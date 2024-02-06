namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class AuthCodeInfoModel {
        public long TemplateInfoId { get; set; }

        /// <summary>
        /// 客户端上限数量
        /// </summary>
        public int MaxClientCount { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// 客户
        /// </summary>
        public string ClientName { get; set; } = string.Empty;
    }
}