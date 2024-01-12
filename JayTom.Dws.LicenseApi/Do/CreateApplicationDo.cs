using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class CreateApplicationDo {

        /// <summary>
        /// 应用程序名称
        /// </summary>
        [Required(ErrorMessage = "应用程序名称不能为空!"),
         MaxLength(30, ErrorMessage = "应用程序名称不能超过30个字符")]
        public string ApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 功能列表
        /// </summary>
        [Required(ErrorMessage = "功能列表不能为空!")]
        public List<FeatureDo>? FeatureInfos { get; set; } = new();
    }
}