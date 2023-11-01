using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel {

    public class LocationOutputSettingsInfoModel : BindableBase {
        private System.Drawing.Point _barcodeOutputPosition;
        private System.Drawing.Point _weightOutputPosition;
        private string? _barcodeOutputKey;
        private string? _weightOutputKey;
        private int _operationDelay;
        private bool _isOutputWeightFirst;
        private bool _isOutputBarcode;
        private bool _isOutputWeight;

        /// <summary>
        /// 条码输出位置
        /// </summary>
        public System.Drawing.Point BarcodeOutputPosition {
            get => _barcodeOutputPosition;
            set => SetProperty(ref _barcodeOutputPosition, value);
        }

        /// <summary>
        /// 重量输出位置
        /// </summary>
        public System.Drawing.Point WeightOutputPosition {
            get => _weightOutputPosition;
            set => SetProperty(ref _weightOutputPosition, value);
        }

        /// <summary>
        /// 条码输出后按键
        /// </summary>
        public string? BarcodeOutputKey {
            get => _barcodeOutputKey;
            set => SetProperty(ref _barcodeOutputKey, value);
        }

        /// <summary>
        /// 重量输出后按键
        /// </summary>
        public string? WeightOutputKey {
            get => _weightOutputKey;
            set => SetProperty(ref _weightOutputKey, value);
        }

        /// <summary>
        /// 操作延迟
        /// </summary>
        public int OperationDelay {
            get => _operationDelay;
            set => SetProperty(ref _operationDelay, value);
        }

        /// <summary>
        /// 是否先输出重量
        /// </summary>
        public bool IsOutputWeightFirst {
            get => _isOutputWeightFirst;
            set => SetProperty(ref _isOutputWeightFirst, value);
        }

        /// <summary>
        /// 是否输出条码
        /// </summary>
        public bool IsOutputBarcode {
            get => _isOutputBarcode;
            set => SetProperty(ref _isOutputBarcode, value);
        }

        /// <summary>
        /// 是否输出重量
        /// </summary>
        public bool IsOutputWeight {
            get => _isOutputWeight;
            set => SetProperty(ref _isOutputWeight, value);
        }
    }
}