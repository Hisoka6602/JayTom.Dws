using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Domain.Dto.CameraConfiguration {
    public class UsbBarcodeReaderDto {
        /*/// <summary>
        /// 是否使用OrCode码
        /// </summary>
        public bool IsUseOrCode { get; set; }

        /// <summary>
        /// 是否使用MicroQR
        /// </summary>
        public bool IsUseMicroQr { get; set; }

        /// <summary>
        /// 是否使用Code39
        /// </summary>
        public bool IsUseCode39 { get; set; }

        /// <summary>
        /// 是否使用Code93
        /// </summary>
        public bool IsUseCode93 { get; set; }

        /// <summary>
        /// 是否使用Code128
        /// </summary>
        public bool IsUseCode128 { get; set; }

        /// <summary>
        /// 是否使用CodeBar
        /// </summary>
        public bool IsUseCodeBar { get; set; }

        /// <summary>
        /// 是否使用ITF
        /// </summary>
        public bool IsUseItf { get; set; }

        /// <summary>
        /// 是否使用Ean13
        /// </summary>
        public bool IsUseEan13 { get; set; }

        /// <summary>
        /// 是否使用
        /// </summary>
        public bool IsUseEan8 { get; set; }*/

        /// <summary>
        /// 本地化模式
        /// </summary>
        public int LocalizationMode { get; set; }

        /// <summary>
        /// 去模糊级别
        /// </summary>
        public int DeblurLevel { get; set; } = 3;

        /// <summary>
        /// 期望的条形码数量
        /// </summary>
        public int ExpectedBarcodesCount { get; set; } = 1;

        /// <summary>
        /// 缩放阈值
        /// </summary>
        public int ScaleDownThreshold { get; set; } = 2300;

        /// <summary>
        /// 是否使用文本过滤模式
        /// </summary>
        public bool IsUseTextFilterMode { get; set; } = true;

        /// <summary>
        /// 是否使用区域预检测模式
        /// </summary>
        public bool IsUseRegionPredetectionMode { get; set; } = true;

        /// <summary>
        /// 灰度转换模式
        /// </summary>
        public int GrayscaleTransformationMode { get; set; }

        /// <summary>
        /// 图像预处理模式
        /// </summary>
        public int ImagePreprocessingMode { get; set; }

        /// <summary>
        /// 最小结果置信度
        /// </summary>
        public int MinResultConfidence { get; set; } = 3;

        /// <summary>
        /// 纹理检测敏感度
        /// </summary>
        public int TextureDetectionSensitivity { get; set; } = 9;

        /// <summary>
        /// 二值化块大小
        /// </summary>
        public int BinarizationBlockSize { get; set; }

        /// <summary>
        /// 识别模式
        /// </summary>
        public int RecognitionMode { get; set; }

        /// <summary>
        /// 识别跳过帧
        /// </summary>
        public int RecognitionSkipFrames { get; set; }

        /// <summary>
        /// 图片缩放百分比
        /// </summary>
        public int ScalePercentage { get; set; } = 5;

        /// <summary>
        /// 条码类型
        /// </summary>
        public BarcodeType BarcodeType { get; set; } = BarcodeType.Code39 | BarcodeType.Code128 | BarcodeType.CodeBar | BarcodeType.QRCode;
    }

    [Flags]
    public enum BarcodeType {

        /// <summary>
        /// 未知类型的条码。
        /// </summary>
        None = 0,

        /// <summary>
        /// QR Code 条码。
        /// </summary>
        QRCode = 1 << 0,

        /// <summary>
        /// 微型 QR Code 条码。
        /// </summary>
        MicroQR = 1 << 1,

        /// <summary>
        /// Code 39 条码。
        /// </summary>
        Code39 = 1 << 2,

        /// <summary>
        /// Code 93 条码。
        /// </summary>
        Code93 = 1 << 3,

        /// <summary>
        /// Code 128 条码。
        /// </summary>
        Code128 = 1 << 4,

        /// <summary>
        /// CodeBar 条码。
        /// </summary>
        CodeBar = 1 << 5,

        /// <summary>
        /// Interleaved 2 of 5 (ITF) 条码。
        /// </summary>
        ITF = 1 << 6,

        /// <summary>
        /// EAN-13 条码。
        /// </summary>
        EAN13 = 1 << 7,

        /// <summary>
        /// EAN-8 条码。
        /// </summary>
        EAN8 = 1 << 8
    }
}