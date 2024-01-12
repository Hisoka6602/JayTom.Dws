using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class FeatureDo {

        [Required(ErrorMessage = "功能名称不能为空!"),
         MaxLength(30, ErrorMessage = "应用程序名称不能超过30个字符")]
        public string FeatureName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Guid能为空!"),
         MaxLength(30, ErrorMessage = "Guid能为空不能超过30个字符")]
        public string FeatureGuid { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}