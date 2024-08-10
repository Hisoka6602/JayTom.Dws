using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Plugin.Device.KeyboardDevice;

namespace JayTom.Dws.Domain.Dto {

    public class ContentInputSettingsDto {

        /// <summary>
        /// 是否使用Tcp输入
        /// </summary>
        public bool IsUseTcpInput { get; set; }

        /// <summary>
        /// 是否使用控件输入
        /// </summary>
        public bool IsUseControlInput { get; set; }

        /// <summary>
        /// 是否使用扫码枪输入
        /// </summary>
        public bool IsUseBarcodeScannerInput { get; set; }

        /// <summary>
        /// 是否使用常规过滤
        /// </summary>
        public bool IsUseRegularFilter { get; set; }

        public KeyboardDevice KeyboardDevice { get; set; } = new();

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfo TcpSettingsInfo { get; set; } = new();

        /// <summary>
        /// 控件输入设置
        /// </summary>
        public ControlInputInfo ControlInputInfo { get; set; } = new();

        /// <summary>
        /// 数据模板
        /// </summary>
        public List<ItemTemplateInfo> DataTemplate { get; set; } = new();

        /// <summary>
        /// 分隔符
        /// </summary>
        public string Separator { get; set; } = string.Empty;
    }

    public class ControlInputInfo {

        /// <summary>
        /// 是否接收条码
        /// </summary>
        public bool IsReceiveBarcode { get; set; }

        /// <summary>
        /// 是否接收重量
        /// </summary>
        public bool IsReceiveWeight { get; set; }

        /// <summary>
        /// 是否接收长度
        /// </summary>
        public bool IsReceiveLength { get; set; }

        /// <summary>
        /// 是否接收宽度
        /// </summary>
        public bool IsReceiveWidth { get; set; }

        /// <summary>
        /// 是否接收高度
        /// </summary>
        public bool IsReceiveHeight { get; set; }

        /// <summary>
        /// 是否接收体积
        /// </summary>
        public bool IsReceiveVolume { get; set; }
    }
}