using JayTom.Dws.Domain.Dto;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel {

    public class TriggerPositionResultModel : BindableBase {
        private string _resultName = string.Empty;
        private ResultEnum _resultValue;

        /// <summary>
        /// 名称
        /// </summary>
        public string ResultName {
            get => _resultName;
            set => SetProperty(ref _resultName, value);
        }

        /// <summary>
        /// 实际内容
        /// </summary>
        public ResultEnum ResultValue {
            get => _resultValue;
            set => SetProperty(ref _resultValue, value);
        }
    }
}