using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class StopBitsInfoModel : BindableBase {
        private string _name = "None";
        private StopBits _value;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public StopBits Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}