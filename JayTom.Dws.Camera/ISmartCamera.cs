using System;
using System.Linq;
using System.Text;
using JayTom.Dws.Ocr;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JayTom.Dws.Camera {

    /// <summary>
    /// 智能相机接口
    /// </summary>
    public interface ISmartCamera : ICamera {

        /// <summary>
        /// Ocr对象
        /// </summary>
        public IOcr? Ocr { get; set; }

        /// <summary>
        /// 条码边框大小
        /// </summary>
        public int BarcodeBorderSize { get; set; }

        /// <summary>
        /// 边框颜色
        /// </summary>
        public System.Drawing.Color BarcodeBorderColor { get; set; }

        /// <summary>
        /// 是否显示边框
        /// </summary>
        public bool IsShowBarcodeBorder { get; set; }

        /// <summary>
        /// 是否使用触发模式
        /// </summary>
        public bool IsUseTriggerMode { get; set; }

        /// <summary>
        /// 触发模式
        /// </summary>
        public TriggerMode TriggerMode { get; set; }

        /// <summary>
        /// 数据来源(管脚、针对海康)
        /// </summary>
        public int SourceLine { get; set; }

        /// <summary>
        /// 软触发一次
        /// </summary>
        void SoftwareTriggerOnce();

        /// <summary>
        /// 读码触发回调事件
        /// </summary>
        event EventHandler<BarcodeTriggeredEventArgs> BarcodeReadTriggered;

        /// <summary>
        /// 包裹触发但未识别到条码
        /// </summary>

        event EventHandler<BarcodeReadEventArgs> NotBarcodeHitEvent;

        /// <summary>
        /// 当OCR识别到内容时触发的事件
        /// </summary>
        event EventHandler<OcrResult> OcrContentRecognized;

        /// <summary>
        /// 设置扫码过滤参数
        /// </summary>
        /// <param name="params"></param>
        /// <returns></returns>
        void SetScanCodeFilterParams([NotNull] ScanCodeFilterParams @params);
    }

    public enum TriggerMode {

        /// <summary>
        /// 软件触发模式
        /// </summary>
        Software,

        /// <summary>
        /// 硬件触发模式
        /// </summary>
        Hardware
    }

    public class BarcodeTriggeredEventArgs : BarcodeReadEventArgs {

        /// <summary>
        /// 条码Id
        /// </summary>
        public string CodeId { get; set; } = string.Empty;

        /// <summary>
        /// 处理总耗时
        /// </summary>
        public int TotalProcCost { get; set; }

        /// <summary>
        /// 算法耗时
        /// </summary>
        public ushort AlgoCost { get; set; }

        /// <summary>
        /// PPM(10倍)
        /// </summary>
        public ushort Ppm { get; set; }

        /// <summary>
        /// 字符长度
        /// </summary>
        public int Len { get; set; }

        /// <summary>
        /// 条码类型
        /// </summary>
        public string BarType { get; set; } = string.Empty;

        /// <summary>
        /// 条码被识别的次数
        /// </summary>
        public int AppearCount { get; set; }

        /// <summary>
        /// 图像清晰度(10倍)
        /// </summary>
        public ushort Sharpness { get; set; }

        /// <summary>
        /// 条码角度(10倍)（0~3600）
        /// </summary>
        public int Angle { get; set; }
    }
}