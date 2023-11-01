using Prism.Mvvm;
using System.IO.Ports;

namespace JayTom.Dws.Client.Models {
    public class ParityInfoModel : BindableBase {
        private string _name = "None";
        private Parity _value = Parity.Even;

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