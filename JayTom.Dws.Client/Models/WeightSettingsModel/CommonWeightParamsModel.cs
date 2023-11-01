using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.WeightSettingsModel {

    public class CommonWeightParamsModel : BindableBase {
        private float _minWeight;
        private float _maxWeight;

        /// <summary>
        /// 最小重量
        /// </summary>
        public float MinWeight {
            get => _minWeight;
            set => SetProperty(ref _minWeight, value);
        }

        /// <summary>
        /// 最大重量
        /// </summary>
        public float MaxWeight {
            get => _maxWeight;
            set => SetProperty(ref _maxWeight, value);
        }
    }
}