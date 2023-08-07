using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Camera {

    /// <summary>
    /// 智能相机接口
    /// </summary>
    public interface ISmartCamera : ICamera {

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
        /// 读码触发回调事件
        /// </summary>
        event EventHandler<BarcodeTriggeredEventArgs> BarcodeReadTriggered;

        //软触发/主动触发
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