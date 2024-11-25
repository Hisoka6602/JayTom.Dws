namespace JayTom.Dws.LicenseApi.Dto {

    public class LicenseInfoDto {
        public long Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

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
        /// 客户
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// 用户代码
        /// </summary>
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 分组名称
        /// </summary>
        public string GroupName { get; set; } = string.Empty;
        /// <summary>
        /// 扫码器上限数量
        /// </summary>
        public int MaxBindingScannerCount { get; set; }
        public List<LicenseClientBindingDto> MachineCodeItem { get; set; } = new();
    }

    public class LicenseClientBindingDto {

        /// <summary>
        /// 机器码
        /// </summary>
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 首次激活时间
        /// </summary>
        public DateTime FirstActivatedDate { get; set; }

        /// <summary>
        /// 最后效验时间
        /// </summary>

        public DateTime LastVerifiedDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}