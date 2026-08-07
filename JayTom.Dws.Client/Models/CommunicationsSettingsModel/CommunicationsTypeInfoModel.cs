using Prism.Mvvm;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel
{

    public class CommunicationsTypeInfoModel : BindableBase
    {
        private string _name = "None";
        private CommunicationsType _value = CommunicationsType.None;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public CommunicationsType Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}