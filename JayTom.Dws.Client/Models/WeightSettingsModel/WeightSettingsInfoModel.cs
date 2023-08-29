using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Client.Models.SettingsCommomModels;

namespace JayTom.Dws.Client.Models.WeightSettingsModel {

    public class WeightSettingsInfoModel : BindableBase {
        private WeightMode _mode = WeightMode.None;
        private SerialPortSettingsInfoModel _connection = new();
        private CommonWeightParamsModel _commonWeight = new();
        private StaticWeightParamsModel _staticWeight = new();
        private DynamicWeightParamsModel _dynamicWeight = new();
        private AdditionalWeightPropertiesModel _additionalWeight = new();

        /// <summary>
        /// 称重模式
        /// </summary>
        public WeightMode Mode {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        /// <summary>
        /// 连接参数
        /// </summary>
        public SerialPortSettingsInfoModel Connection {
            get => _connection;
            set => SetProperty(ref _connection, value);
        }

        /// <summary>
        /// 公共参数
        /// </summary>
        public CommonWeightParamsModel CommonWeight {
            get => _commonWeight;
            set => SetProperty(ref _commonWeight, value);
        }

        /// <summary>
        /// 静态称参数
        /// </summary>
        public StaticWeightParamsModel StaticWeight {
            get => _staticWeight;
            set => SetProperty(ref _staticWeight, value);
        }

        /// <summary>
        /// 动态称参数
        /// </summary>
        public DynamicWeightParamsModel DynamicWeight {
            get => _dynamicWeight;
            set => SetProperty(ref _dynamicWeight, value);
        }

        /// <summary>
        /// 重量附加属性
        /// </summary>
        public AdditionalWeightPropertiesModel AdditionalWeight {
            get => _additionalWeight;
            set => SetProperty(ref _additionalWeight, value);
        }
    }
}