using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Plugin.Excel.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Client.Models {

    public class BarCodeItemModel : BindableBase {
        private float _volume;
        private bool _isInserting;
        private bool _isRemoving;

        [DisplayName("No."), ExcelInfo(Width = 3000)]
        public int Num { get; set; }

        /// <summary>
        /// 时间戳Id
        /// </summary>
        [DisplayName("TimestampedGuid"), ExcelInfo(Width = 5000)]
        public long TimestampedGuid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        [DisplayName("Barcode"), ExcelInfo(Width = 4000)]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 重量
        /// </summary>
        [DisplayName("Weight"), ExcelInfo(Width = 2000)]
        public float Weight { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        [DisplayName("Length"), ExcelInfo(Width = 2000)]
        public float Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        [DisplayName("Width"), ExcelInfo(Width = 2000)]
        public float Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        [DisplayName("Height"), ExcelInfo(Width = 2000)]
        public float Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        [DisplayName("Volume"), ExcelInfo(Width = 2000)]
        public float Volume {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        /// <summary>
        /// 扫码时间
        /// </summary>
        [DisplayName("ScanTime"), ExcelInfo(Width = 4000)]
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        [DisplayName("RequestStatus"), ExcelInfo(Width = 4000)]
        public string RequestStatus { get; set; } = "NotUploaded";

        /// <summary>
        /// 上传时间
        /// </summary>
        [DisplayName("RequestTime"), ExcelInfo(Width = 4000)]
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 上传内容
        /// </summary>
        [DisplayName("RequestContent"), ExcelInfo(Width = 8000)]
        public string RequestContent { get; set; } = string.Empty;

        /// <summary>
        /// 接口响应时间
        /// </summary>
        [DisplayName("ResponseTime"), ExcelInfo(Width = 4000)]
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 接口响应内容
        /// </summary>
        [DisplayName("ResponseContent"), ExcelInfo(Width = 8000)]
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 条码图片保存路径
        /// </summary>
        public string? BarcodeImagePath { get; set; }

        /// <summary>
        /// 是否插入
        /// </summary>
        public bool IsInserting {
            get => _isInserting;
            set => SetProperty(ref _isInserting, value);
        }

        /// <summary>
        /// 是否移除
        /// </summary>
        public bool IsRemoving {
            get => _isRemoving;
            set => SetProperty(ref _isRemoving, value);
        }
    }
}