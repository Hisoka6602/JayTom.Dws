using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Data.LocalData {

    public class BarCodeInfoModel : BaseModel {

        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 重量
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// 扫码时间
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// 上传状态(1成功、2失败、0未上传)
        /// </summary>
        public int RequestStatus { get; set; }

        /// <summary>
        /// 上传内容
        /// </summary>
        public string RequestContent { get; set; } = string.Empty;

        /// <summary>
        /// 接口响应内容
        /// </summary>
        public string ResponseContent { get; set; } = string.Empty;

        /// <summary>
        /// 接口响应时间
        /// </summary>
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 条码图片保存路径
        /// </summary>
        public string? BarcodeImagePath { get; set; }

        /// <summary>
        /// 全景图片保存路径
        /// </summary>
        public string? PanoramaImagePath { get; set; }

        /// <summary>
        /// 下位机指令内容
        /// </summary>
        public string? CommandContent { get; set; }

        /// <summary>
        /// 指令发送时间
        /// </summary>
        public DateTime? CommandSentTime { get; set; }

        /// <summary>
        /// 指令发送目标地址
        /// </summary>
        public string? DestinationAddress { get; set; }

        /// <summary>
        /// 其他项
        /// </summary>
        public string? Other { get; set; }
    }
}