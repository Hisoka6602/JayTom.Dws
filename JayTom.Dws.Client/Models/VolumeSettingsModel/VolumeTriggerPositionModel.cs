using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.VolumeSettingsModel {

    public class VolumeTriggerPositionModel : BindableBase {
        private string _name = Languages.Language.ResourceManager.GetString("AfterScanning") ?? string.Empty;
        private VolumeTriggerPosition _value = VolumeTriggerPosition.BarcodeDetected;

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
        public VolumeTriggerPosition Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}