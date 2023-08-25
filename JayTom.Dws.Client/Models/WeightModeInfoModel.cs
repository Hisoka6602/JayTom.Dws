using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.IO.Ports;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class WeightModeInfoModel : BindableBase {
        private string _name = "None";
        private WeightMode _value = WeightMode.None;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public WeightMode Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}