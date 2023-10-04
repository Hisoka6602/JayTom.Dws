using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Models.PackageSorting {
    public class ExitTypeInfoModel : BindableBase {
        private string _name = string.Empty;
        private ExitType _value = ExitType.PackageExit;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ExitType Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}
