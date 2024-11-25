using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class CreateLicenseCodeDo : BaseLicenseCodeDo {

        /// <summary>
        /// 客户端上限数量
        /// </summary>
        [LicenseCountLimit(ErrorMessage = "授权数量超过可配置上限")]
        public int MaxClientCount { get; set; }

        /// <summary>
        /// 授权码
        /// </summary>

        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 扫码器上限
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "数值不在可设置范围内")]
        public int MaxBindingScannerCount { get; set; }
    }
}