namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class AppTemplateItemInfoModel : ApplicationItemInfoModel {

        /// <summary>
        /// 应用Id
        /// </summary>
        public long LicenseApplicationInfoId { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;
    }
}