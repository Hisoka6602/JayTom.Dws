using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;

namespace JayTom.Dws.Client.Models {

    public class VolumeUnitInfoModel : BindableBase {
        private string _name = "mm";
        private VolumeUnit _value = VolumeUnit.Millimeter;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public VolumeUnit Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}