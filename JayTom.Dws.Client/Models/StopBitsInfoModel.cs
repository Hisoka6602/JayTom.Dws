using Prism.Mvvm;
using System.IO.Ports;

namespace JayTom.Dws.Client.Models
{

    public class StopBitsInfoModel : BindableBase
    {
        private string _name = "None";
        private StopBits _value;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public StopBits Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}