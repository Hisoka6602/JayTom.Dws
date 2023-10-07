using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.IO.Ports;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.CommunicationsSettingsModel {

    public class CommunicationProtocolInfoModel : BindableBase {
        private string _name = "None";
        private CommunicationProtocol _value = CommunicationProtocol.None;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public CommunicationProtocol Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}