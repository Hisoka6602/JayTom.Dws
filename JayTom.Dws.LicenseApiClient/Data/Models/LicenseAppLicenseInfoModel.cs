namespace JayTom.Dws.LicenseApiClient.Data.Models {

    public class LicenseAppLicenseInfoModel : BaseItemInfoModel {

        /// <summary>
        /// 模板Id
        /// </summary>
        public long? LicensePermissionTemplateInfoId { get; set; }

        /// <summary>
        /// 授权码上限
        /// </summary>
        public int MaxLicenseCodeCount { get; set; }
    }
}