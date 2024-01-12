using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class SetTemplatePermissionsDo {

        /// <summary>
        /// 模板Id
        /// </summary>
        [TemplateIdExists(IsExists = true, ErrorMessage = "模板Id不存在!")]
        public long TemplateId { get; set; }

        /// <summary>
        /// 功能列表
        /// </summary>
        [Required(ErrorMessage = "功能列表不能为空!")]
        public List<FeatureDo>? FeatureInfos { get; set; } = new();
    }
}