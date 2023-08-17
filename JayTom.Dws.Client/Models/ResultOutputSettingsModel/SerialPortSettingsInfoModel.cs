using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models.ResultOutputSettingsModel {

    public class SerialPortSettingsInfoModel : BindableBase {
        private string _portName = string.Empty;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;

        /// <summary>
        /// 串口名称
        /// </summary>
        public string PortName {
            get => _portName;
            set => SetProperty(ref _portName, value);
        } // 串口名称

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        /// <summary>
        /// 效验位
        /// </summary>
        public Parity Parity {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }
    }
}