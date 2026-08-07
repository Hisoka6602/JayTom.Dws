using JayTom.Dws.Domain.Dto;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models
{

    public class WeightAccessInfoMode : BindableBase
    {
        private string _name = "Readonly";
        private WeightAccessMode _value = WeightAccessMode.Readonly;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public WeightAccessMode Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}