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
        event EventHandler<BarcodeReadEventArgs> CodeReadTriggered;
    }
}