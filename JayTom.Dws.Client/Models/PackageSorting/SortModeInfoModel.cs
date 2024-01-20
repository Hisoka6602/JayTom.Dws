using Prism.Mvvm;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Models.PackageSorting
{

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