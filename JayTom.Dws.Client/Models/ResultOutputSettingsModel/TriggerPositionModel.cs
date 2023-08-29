using Prism.Mvvm;
using JayTom.Dws.Domain.Dto;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel {

    public class TriggerPositionModel : BindableBase {
        private string _triggerPositionName = string.Empty;
        private TriggerPositionEnum _triggerPositionValue;

        /// <summary>
        /// 名称
        /// </summary>
        public string TriggerPositionName {
            get => _triggerPositionName;
            set => SetProperty(ref _triggerPositionName, value);
        }

        /// <summary>
        /// 实际内容
        /// </summary>
        public TriggerPositionEnum TriggerPositionValue {
            get => _triggerPositionValue;
            set => SetProperty(ref _triggerPositionValue, value);
        }
    }
}