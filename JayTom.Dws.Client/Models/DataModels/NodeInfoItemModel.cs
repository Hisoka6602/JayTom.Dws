using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.DataModels {

    public class NodeInfoItemModel : BindableBase {
        private string _nodeName = string.Empty;
        private string _barcode = string.Empty;
        private DateTime _scanTime = DateTime.MinValue;
        private string _imagePath = string.Empty;
        private string _originalText = string.Empty;
        private string _serialNumber = string.Empty;
        private int _nodeNum;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }

        public int NodeNum {
            get => _nodeNum;
            set => SetProperty(ref _nodeNum, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime {
            get => _scanTime;
            set => SetProperty(ref _scanTime, value);
        }

        /// <summary>
        /// 图片路径
        /// </summary>
        public string ImagePath {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        /// <summary>
        /// 源字符
        /// </summary>
        public string OriginalText {
            get => _originalText;
            set => SetProperty(ref _originalText, value);
        }

        /// <summary>
        /// 输入序列(来源设备唯一标识)
        /// </summary>
        public string SerialNumber {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }
    }
}