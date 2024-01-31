using System.ComponentModel;
using JayTom.Dws.CloudApiClient.Plugin.Excel.Attributes;

namespace JayTom.Dws.CloudApiClient.Data.Models.ExcelModels {

    public class ExcelDetailInfoModel {

        [DisplayName("序号"), ExcelInfo(Width = 2000)]
        public int Num { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        [DisplayName("扫码时间"), ExcelInfo(Width = 6000)]
        public DateTime ScanTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 条码
        /// </summary>
        [DisplayName("条码"), ExcelInfo(Width = 6000)]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 重量
        /// </summary>
        [DisplayName("重量"), ExcelInfo(Width = 3000)]
        public float Weight { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        [DisplayName("长度"), ExcelInfo(Width = 3000)]
        public float Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        [DisplayName("宽度"), ExcelInfo(Width = 3000)]
        public float Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        [DisplayName("高度"), ExcelInfo(Width = 3000)]
        public float Height { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        [DisplayName("体积"), ExcelInfo(Width = 3000)]
        public float Volume { get; set; }

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        [DisplayName("上传状态"), ExcelInfo(Width = 4000)]
        public string RequestStatus { get; set; } = "未上传";

        /// <summary>
        /// 理论格口
        /// </summary>
        [DisplayName("理论格口"), ExcelInfo(Width = 4000)]
        public string TheoreticalExit { get; set; } = string.Empty;

        /// <summary>
        /// 物理格口
        /// </summary>
        [DisplayName("物理格口"), ExcelInfo(Width = 4000)]
        public string PhysicalExit { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称
        /// </summary>
        [DisplayName("设备名称"), ExcelInfo(Width = 6000)]
        public string DeviceName { get; set; } = string.Empty;
    }
}