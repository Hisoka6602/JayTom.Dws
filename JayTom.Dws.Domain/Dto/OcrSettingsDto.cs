using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto {
    public class OcrSettingsDto {

        /// <summary>
        /// 是否使用 OCR 识别
        /// </summary>
        public bool IsUseOcr { get; set; }

        /// <summary>
        /// 是否识别三段码
        /// </summary>
        public bool IsThreeSegmentCode { get; set; }

        /// <summary>
        /// 是否显示识别耗时
        /// </summary>
        public bool IsShowRecognitionTime { get; set; }

        /// <summary>
        /// 是否显示收件人信息
        /// </summary>
        public bool IsShowReceiverInfo { get; set; }

        /// <summary>
        /// 是否显示发件人信息
        /// </summary>
        public bool IsShowSenderInfo { get; set; }

        /// <summary>
        /// 识别超时时间
        /// </summary>
        public int RecognitionTimeout { get; set; }

        /// <summary>
        /// 算法选择
        /// </summary>
        public string ModelFilePath { get; set; } = string.Empty;

        /// <summary>
        /// 置信度
        /// </summary>
        public decimal ConfidenceThreshold { get; set; }

        /// <summary>
        /// 截图扩充倍数
        /// </summary>
        public decimal RectangleScale { get; set; }

        /// <summary>
        /// 截图存图路径
        /// </summary>
        public string CropImagePath { get; set; } = string.Empty;

        /// <summary>
        /// 是否开启截图保存
        /// </summary>
        public bool IsSaveCropImage { get; set; }
        /// <summary>
        /// 是否开启条码二次确认
        /// </summary>
        public bool IsSecondConfirmationEnabled { get; set; }
    }
}