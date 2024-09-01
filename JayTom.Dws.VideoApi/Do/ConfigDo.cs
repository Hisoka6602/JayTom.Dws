using JayTom.Dws.VideoApi.Attributes;
using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.VideoApi.Do {

    public class ConfigDo {

        [Required(ErrorMessage = "配置名称不能为空")]
        public string SettingsName { get; set; } = string.Empty;

        [Required(ErrorMessage = "配置内容不能为空"),
         JsonValidation(ErrorMessage = "配置内容不是有效的 JSON 格式")]
        public string ConfigJson { get; set; } = string.Empty;
    }
}