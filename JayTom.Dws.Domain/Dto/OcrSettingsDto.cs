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
        /// 是否显示物流公司
        /// </summary>
        public bool IsShowLogisticsCompany { get; set; }

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
        /// 是否显示格口号
        /// </summary>
        public bool IsShowCompartmentNumber { get; set; }
    }
}