using Prism.Mvvm;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Models.PackageSorting
{

    public class SortModeInfoModel : BindableBase
    {
        public string Name
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public SortMode Value
        {
            get;
            set => SetProperty(ref field, value);
        } = SortMode.None;
    }
}
