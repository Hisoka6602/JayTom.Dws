using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.WeightSettingsModel
{

    public class DynamicWeightParamsModel : BindableBase
    {
        /// <summary>
        /// 保留位数
        /// </summary>
        public int DecimalPrecision
        {
            get;
            set => SetProperty(ref field, value);
        } = 3;
    }
}
