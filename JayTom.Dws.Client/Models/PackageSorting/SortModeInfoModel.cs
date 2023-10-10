using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;

namespace JayTom.Dws.Client.Models.PackageSorting {

    public class SortModeInfoModel : BindableBase {
        private SortMode _value = SortMode.None;
        private string _name = string.Empty;

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public SortMode Value {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}