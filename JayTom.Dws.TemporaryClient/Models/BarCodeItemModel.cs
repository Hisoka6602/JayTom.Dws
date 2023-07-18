using System;
using Prism.Mvvm;
using System.ComponentModel;
using JayTom.Dws.Plugin.Excel.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.TemporaryClient.Models {

    public class BarCodeItemModel : BindableBase {
        private int _num;
        private long _timestampedGuid;
        private string _barcode = string.Empty;
        private float _weight;
        private float _length;
        private float _width;
        private float _height;
        private DateTime _scanTime;
        private string _requestStatus = "NotUploaded";
        private DateTime _requestTime;
        private string _requestContent = string.Empty;
        private DateTime _responseTime;
        private string _responseContent = string.Empty;

        [DisplayName("No."), ExcelInfo(Width = 3000)]
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 时间戳Id
        /// </summary>
        [DisplayName("TimestampedGuid"), ExcelInfo(Width = 5000)]
        public long TimestampedGuid {
            get => _timestampedGuid;
            set => SetProperty(ref _timestampedGuid, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        [DisplayName("Barcode"), ExcelInfo(Width = 4000)]
        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        [DisplayName("Weight"), ExcelInfo(Width = 2000)]
        public float Weight {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        [DisplayName("Length"), ExcelInfo(Width = 2000)]
        public float Length {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 宽度
        /// </summary>
        [DisplayName("Width"), ExcelInfo(Width = 2000)]
        public float Width {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 高度
        /// </summary>
        [DisplayName("Height"), ExcelInfo(Width = 2000)]
        public float Height {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        /// <summary>
        /// 扫码时间
        /// </summary>
        [DisplayName("ScanTime"), ExcelInfo(Width = 4000)]
        public DateTime ScanTime {
            get => _scanTime;
            set => SetProperty(ref _scanTime, value);
        }

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        [DisplayName("RequestStatus"), ExcelInfo(Width = 4000)]
        public string RequestStatus {
            get => _requestStatus;
            set => SetProperty(ref _requestStatus, value);
        }

        /// <summary>
        /// 上传时间
        /// </summary>
        [DisplayName("RequestTime"), ExcelInfo(Width = 4000)]
        public DateTime RequestTime {
            get => _requestTime;
            set => SetProperty(ref _requestTime, value);
        }

        /// <summary>
        /// 上传内容
        /// </summary>
        [DisplayName("RequestContent"), ExcelInfo(Width = 8000)]
        public string RequestContent {
            get => _requestContent;
            set => SetProperty(ref _requestContent, value);
        }

        /// <summary>
        /// 接口响应时间
        /// </summary>
        [DisplayName("ResponseTime"), ExcelInfo(Width = 4000)]
        public DateTime ResponseTime {
            get => _responseTime;
            set => SetProperty(ref _responseTime, value);
        }

        /// <summary>
        /// 接口响应内容
        /// </summary>
        [DisplayName("ResponseContent"), ExcelInfo(Width = 8000)]
        public string ResponseContent {
            get => _responseContent;
            set => SetProperty(ref _responseContent, value);
        }
    }
}