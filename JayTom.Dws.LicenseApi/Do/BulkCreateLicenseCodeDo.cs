using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class BulkCreateLicenseCodeDo {

        [TemplateIdExists(IsExists = true, ErrorMessage = "模板Id不存在!")]
        public long TemplateInfoId { get; set; }

        /// <summary>
        /// 生成的授权码数量
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "生成的授权码数量必须大于0")]
        [LicenseCountLimit(ErrorMessage = "授权数量超过可配置上限")]
        public int LicenseCodeCount { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 客户
        /// </summary>
        [Required(ErrorMessage = "客户不能为空"),
         RegularExpression(@"^(?!\s*$).+", ErrorMessage = "客户不能只包含空格")]
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// 租户
        /// </summary>
        [UserCodeExists(IsExists = true, ErrorMessage = "租户代码不存在")]
        public string? UserCode { get; set; }
    }
}