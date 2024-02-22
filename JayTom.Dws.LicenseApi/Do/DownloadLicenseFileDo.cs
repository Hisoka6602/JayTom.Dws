using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class DownloadLicenseFileDo {

        /// <summary>
        /// 授权码
        /// </summary>
        [Required(ErrorMessage = "授权码不能为空"), LicenseCodeExists(IsExists = true, ErrorMessage = "授权码不存在!")]
        public string LicenseCode { get; set; } = string.Empty;

        /// <summary>
        /// 机器码
        /// </summary>
        [Required(ErrorMessage = "机器码不能为空"), RegularExpression("^[A-F0-9]{32}$", ErrorMessage = "机器码格式错误!")]
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(300, ErrorMessage = "最大备注长度为300字符")]
        public string Remarks { get; set; } = string.Empty;
    }
}