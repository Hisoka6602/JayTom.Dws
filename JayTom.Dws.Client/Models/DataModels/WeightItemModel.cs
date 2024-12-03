using System;
using Prism.Mvvm;
using JayTom.Dws.Data.Package;

namespace JayTom.Dws.Client.Models.DataModels {

    public class WeightItemModel : BindableBase {
        private string _serialPortName = string.Empty;
        private string _originalText = string.Empty;
        private double _formattedWeight;
        private DateTime? _createTime;

        /// <summary>
        /// 串口名称
        /// </summary>
        public string SerialPortName {
            get => _serialPortName;
            set => SetProperty(ref _serialPortName, value);
        }

        /// <summary>
        /// 源字符
        /// </summary>
        public string OriginalText {
            get => _originalText;
            set => SetProperty(ref _originalText, value);
        }

        /// <summary>
        /// 格式化后重量
        /// </summary>
        public double FormattedWeight {
            get => _formattedWeight;
            set => SetProperty(ref _formattedWeight, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }
    }
}