using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

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