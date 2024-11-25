namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class AuthCodeInfoModel {

        /// <summary>
        /// 用户
        /// </summary>
        public string? UserCode { get; set; }

        /// <summary>
        /// 模板Id
        /// </summary>
        public long TemplateInfoId { get; set; }

        /// <summary>
        /// 客户端上限数量
        /// </summary>
        public int MaxClientCount { get; set; } = 1;

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// 客户
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// 是否分组批量生成
        /// </summary>
        public bool IsGroupedBatchGeneration { get; set; }

        /// <summary>
        /// 授权码生成数量
        /// </summary>
        public int AuthCodeGenerationCount { get; set; }

        /// <summary>
        /// 分组名
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 扫码器上限
        /// </summary>
        public int MaxBindingScannerCount { get; set; } = 1;
    }
}