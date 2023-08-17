using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class StopBitsInfoModel : BindableBase {
        private string _name = "None";
        private int _value;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}