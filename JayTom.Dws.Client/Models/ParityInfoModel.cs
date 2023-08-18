using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class ParityInfoModel : BindableBase {
        private string _name = "None";
        private Parity _value;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public Parity Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}