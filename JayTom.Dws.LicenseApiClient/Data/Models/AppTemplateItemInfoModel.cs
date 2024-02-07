namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class AppTemplateItemInfoModel {
        public long Id { get; set; }

        /// <summary>
        /// 应用Id
        /// </summary>
        public long LicenseApplicationInfoId { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 应用程序
        /// </summary>
        public ApplicationItemInfoModel LicenseApplicationInfo { get; set; } = new();

        /// <summary>
        /// 授权码信息
        /// </summary>
        public LicenseCodeItemInfoModel LicenseCodeInfos { get; set; } = new();

        /// <summary>
        /// 图标
        /// </summary>
        public Stream? Image { get; set; }
    }
}