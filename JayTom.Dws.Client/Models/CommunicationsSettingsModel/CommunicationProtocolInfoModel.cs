using Prism.Mvvm;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel
{

    public class CommunicationProtocolInfoModel : BindableBase
    {
        private string _name = "None";
        private CommunicationProtocol _value = CommunicationProtocol.None;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public CommunicationProtocol Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}