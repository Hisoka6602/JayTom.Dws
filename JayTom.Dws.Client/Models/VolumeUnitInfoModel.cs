using JayTom.Dws.Domain.Converters;
using Prism.Mvvm;

namespace JayTom.Dws.Client.Models
{

    public class VolumeUnitInfoModel : BindableBase
    {
        private string _name = "mm";
        private VolumeUnit _value = VolumeUnit.Millimeter;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public VolumeUnit Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}