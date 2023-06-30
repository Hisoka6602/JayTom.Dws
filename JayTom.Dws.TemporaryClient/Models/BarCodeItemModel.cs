using System;
using Prism.Mvvm;
using System.ComponentModel;
using JayTom.Dws.Plugin.Excel.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.TemporaryClient.Models {

    public class BarCodeItemModel : BindableBase {

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
    }
}