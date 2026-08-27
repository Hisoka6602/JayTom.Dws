using JayTom.Dws.Legacy.Contracts.Dto;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models
{

    public class WeightModeInfoModel : BindableBase
    {
        private string _name = "None";
        private WeightMode _value = WeightMode.None;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public WeightMode Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}