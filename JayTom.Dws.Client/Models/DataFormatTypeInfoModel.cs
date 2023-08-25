using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class DataFormatTypeInfoModel : BindableBase {
        private string _name = "Ascii";
        private DataFormatType _value = DataFormatType.Ascii;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public DataFormatType Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}