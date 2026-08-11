using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.WeightSettingsModel
{

    public class CommonWeightParamsModel : BindableBase
    {
        private decimal _minWeight;
        private decimal _maxWeight;

        /// <summary>
        /// 最小重量
        /// </summary>
        public decimal MinWeight
        {
            get => _minWeight;
            set => SetProperty(ref _minWeight, value);
        }

        /// <summary>
        /// 最大重量
        /// </summary>
        public decimal MaxWeight
        {
            get => _maxWeight;
            set => SetProperty(ref _maxWeight, value);
        }
    }
}