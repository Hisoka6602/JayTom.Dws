using JayTom.Dws.LicenseApi.Attributes;

namespace JayTom.Dws.LicenseApi.Do {

    public class UpdateLicenseCodeDo : BaseLicenseCodeDo {

        /// <summary>
        /// 客户端上限数量
        /// </summary>
        [LicenseCountLimit(IsEdit = true, ErrorMessage = "授权数量超过可配置上限")]
        public int MaxClientCount { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>
        [LicenseCodeExists(ErrorMessage = "授权码不存在", IsExists = true)]
        public string LicenseCode { get; set; } = string.Empty;
    }
}