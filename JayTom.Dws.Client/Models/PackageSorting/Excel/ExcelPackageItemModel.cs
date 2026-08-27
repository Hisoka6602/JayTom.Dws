using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Excel.Attributes;

namespace JayTom.Dws.Client.Models.PackageSorting.Excel
{

    public class ExcelPackageItemModel : BindableBase
    {
        private int _num;
        private long _timestampMilliseconds;
        private string _barcode = string.Empty;
        private decimal _weight;
        private decimal _length;
        private decimal _width;
        private decimal _height;
        private decimal _volume;
        private DateTime _scanTime;
        private UploadStatus _requestStatus;
        private string? _barcodeImagePath;
        private string _theoreticalExit = string.Empty;
        private string _sortingCode = string.Empty;
        private string _physicalExit = string.Empty;
        private DateTime? _signalCallbackInstructionGeneratedTime;

        public ExcelPackageItemModel()
        {
        }

        [DisplayName("No."), ExcelInfo(Width = 3000)]
        public int Num
        {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        ///包裹Id
        /// </summary>
        [DisplayName("包裹Id"), ExcelInfo(Width = 5000)]
        [Newtonsoft.Json.JsonProperty("TimestampedGuid")]
        public long TimestampMilliseconds
        {
            get => _timestampMilliseconds;
            set => SetProperty(ref _timestampMilliseconds, value);
        }

        /// <summary>
        /// 面单条码
        /// </summary>
        [DisplayName("面单条码"), ExcelInfo(Width = 4000)]
        public string Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        [DisplayName("重量"), ExcelInfo(Width = 2000)]
        public decimal Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        [DisplayName("长度"), ExcelInfo(Width = 2000)]
        public decimal Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 宽度
        /// </summary>
        [DisplayName("宽度"), ExcelInfo(Width = 2000)]
        public decimal Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 高度
        /// </summary>
        [DisplayName("高度"), ExcelInfo(Width = 2000)]
        public decimal Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        /// <summary>
        /// 体积
        /// </summary>
        [DisplayName("体积"), ExcelInfo(Width = 2000)]
        public decimal Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        /// <summary>
        /// 扫码时间
        /// </summary>
        [DisplayName("扫码时间"), ExcelInfo(Width = 4000)]
        public DateTime ScanTime
        {
            get => _scanTime;
            set => SetProperty(ref _scanTime, value);
        }

        /// <summary>
        /// 上传状态
        /// </summary>
        [DisplayName("上传状态"), ExcelInfo(Width = 4000)]
        public UploadStatus RequestStatus
        {
            get => _requestStatus;
            set => SetProperty(ref _requestStatus, value);
        }

        /// <summary>
        /// 扫码图路径
        /// </summary>
        [DisplayName("扫码图路径"), ExcelInfo(Width = 8000)]
        public string? BarcodeImagePath
        {
            get => _barcodeImagePath;
            set => SetProperty(ref _barcodeImagePath, value);
        }

        /// <summary>
        /// 理论格口
        /// </summary>
        [DisplayName("理论格口"), ExcelInfo(Width = 3000)]
        public string TheoreticalExit
        {
            get => _theoreticalExit;
            set => SetProperty(ref _theoreticalExit, value);
        }

        /// <summary>
        /// 物理格口
        /// </summary>
        [DisplayName("物理格口"), ExcelInfo(Width = 3000)]
        public string PhysicalExit
        {
            get => _physicalExit;
            set => SetProperty(ref _physicalExit, value);
        }

        /// <summary>
        /// 流水号
        /// </summary>
        [DisplayName("流水号"), ExcelInfo(Width = 3000)]
        public string SortingCode
        {
            get => _sortingCode;
            set => SetProperty(ref _sortingCode, value);
        }

        /// <summary>
        /// 落格信号回调时间
        /// </summary>
        [DisplayName("落格信号回调时间"), ExcelInfo(Width = 5000)]
        public DateTime? SignalCallbackInstructionGeneratedTime
        {
            get => _signalCallbackInstructionGeneratedTime;
            set => SetProperty(ref _signalCallbackInstructionGeneratedTime, value);
        }
    }
}
