using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.WeightSettingsModel {

    public class DynamicWeightParamsModel : BindableBase {
        private int _decimalPrecision = 3;

        /// <summary>
        /// 保留位数
        /// </summary>
        public int DecimalPrecision {
            get => _decimalPrecision;
            set => SetProperty(ref _decimalPrecision, value);
        }
    }
}