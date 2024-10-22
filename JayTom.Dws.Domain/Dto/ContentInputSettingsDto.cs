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
        public bool IsUseBarcodeScannerInput { get; set; } = true;

        /// <summary>
        /// 是否使用常规过滤
        /// </summary>
        public bool IsUseRegularFilter { get; set; }

        /// <summary>
        /// 绑定的扫码枪
        /// </summary>
        public KeyboardDevice KeyboardDevice { get; set; } = new();

        /// <summary>
        /// 控件输入设置
        /// </summary>
        public ControlInputInfo ControlInputInfo { get; set; } = new();

        /// <summary>
        /// Tcp绑定
        /// </summary>
        public List<TcpInputBindingInfo> TcpInputBindingInfos { get; set; } = new();
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

    public class TcpInputBindingInfo {

        /// <summary>
        /// Ip
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; } = 2000;

        /// <summary>
        /// 是否已绑定
        /// </summary>
        public bool IsBound { get; set; }
    }
}