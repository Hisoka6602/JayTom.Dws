using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.WeightSettingsModel
{
    public class AdditionalWeightPropertiesModel : BindableBase
    {
        private bool _isUseActualWeightConversionRate;
        private decimal _weightConversionRate;
        private bool _isUseAppendedWeight;
        private decimal _appendedWeightValue;
        private bool _isUseFixedWeight;
        private decimal _fixedWeightValue;
        private bool _isUseMergedWeightTimeout;
        private int _mergedWeightTimeout = 300;

        /// <summary>
        /// 是否使用实际重量转换率
        /// </summary>
        public bool IsUseActualWeightConversionRate
        {
            get => _isUseActualWeightConversionRate;
            set => SetProperty(ref _isUseActualWeightConversionRate, value);
        }

        /// <summary>
        /// 重量转换率
        /// </summary>
        public decimal WeightConversionRate
        {
            get => _weightConversionRate;
            set => SetProperty(ref _weightConversionRate, value);
        }

        /// <summary>
        /// 是否使用追加重量
        /// </summary>
        public bool IsUseAppendedWeight
        {
            get => _isUseAppendedWeight;
            set => SetProperty(ref _isUseAppendedWeight, value);
        }

        /// <summary>
        /// 追加重量的值
        /// </summary>
        public decimal AppendedWeightValue
        {
            get => _appendedWeightValue;
            set => SetProperty(ref _appendedWeightValue, value);
        }

        /// <summary>
        /// 是否使用固定重量
        /// </summary>
        public bool IsUseFixedWeight
        {
            get => _isUseFixedWeight;
            set => SetProperty(ref _isUseFixedWeight, value);
        }

        /// <summary>
        /// 固定重量的值
        /// </summary>
        public decimal FixedWeightValue
        {
            get => _fixedWeightValue;
            set => SetProperty(ref _fixedWeightValue, value);
        }

        /// <summary>
        /// 是否使用融合重量超时
        /// </summary>
        public bool IsUseMergedWeightTimeout
        {
            get => _isUseMergedWeightTimeout;
            set => SetProperty(ref _isUseMergedWeightTimeout, value);
        }

        /// <summary>
        /// 融合重量超时时间
        /// </summary>
        public int MergedWeightTimeout
        {
            get => _mergedWeightTimeout;
            set => SetProperty(ref _mergedWeightTimeout, value);
        }
    }
}