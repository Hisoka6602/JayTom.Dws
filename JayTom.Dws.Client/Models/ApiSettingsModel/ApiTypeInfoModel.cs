using Prism.Mvvm;
using JayTom.Dws.Domain.Dto;

namespace JayTom.Dws.Client.Models.ApiSettingsModel {

    public class ApiTypeInfoModel : BindableBase {
        private string _name = string.Empty;
        private string _value = string.Empty;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 实际内容
        /// </summary>
        public string Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}