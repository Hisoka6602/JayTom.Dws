using JayTom.Dws.LicenseApi.Attributes;

namespace JayTom.Dws.LicenseApi.Do {

    public class FreezeLicenseCodeDo {

        [LicenseCodeExists(IsExists = true, ErrorMessage = "授权码不存在!")]
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 是否冻结
        /// </summary>
        public bool IsFreeze { get; set; }
    }
}