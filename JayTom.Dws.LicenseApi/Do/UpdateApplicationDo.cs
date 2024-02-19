using JayTom.Dws.LicenseApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.LicenseApi.Do {

    public class UpdateApplicationDo {

        /// <summary>
        /// 应用程序Id
        /// </summary>
        [Required(ErrorMessage = "应用程序Id不能为空"), LicenseApplicationIdExists(IsExists = true, ErrorMessage = "应用程序不存在")]
        public long ApplicationId { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 调试机器
        /// </summary>
        [Required(ErrorMessage = "机器码不能为空"), RegularExpression("^[A-F0-9]{32}$", ErrorMessage = "机器码格式错误!"),
         DebugMachineCode(ErrorMessage = "该设备无法调用此方法")]
        public string MachineCode { get; set; } = string.Empty;

        /// <summary>
        /// 功能列表
        /// </summary>
        [Required(ErrorMessage = "功能列表不能为空!")]
        public List<FeatureDo>? FeatureInfos { get; set; } = new();
    }
}