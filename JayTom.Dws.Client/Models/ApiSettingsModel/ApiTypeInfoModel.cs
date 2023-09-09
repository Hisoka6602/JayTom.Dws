using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ApiSettingsModel {

    public class ApiTypeInfoModel : BindableBase {
        private string _name = string.Empty;
        private ApiType _value = ApiType.None;

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
        public ApiType Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}