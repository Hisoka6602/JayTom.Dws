using Prism.Mvvm;
using System.IO.Ports;
using JayTom.Dws.Plugin.Tcp;
using JayTom.Dws.Models.LocalLog;

namespace JayTom.Dws.Client.Models.SettingsCommomModels
{

    public class SerialPortSettingsInfoModel : BindableBase
    {
        private string _portName = string.Empty;
        private int _baudRate;
        private int _dataBits;
        private Parity _parity;
        private StopBits _stopBits;
        private DataFormatType _dataFormat = DataFormatType.Ascii;

        /// <summary>
        /// 串口
        /// </summary>
        public string PortName
        {
            get => _portName;
            set => SetProperty(ref _portName, value);
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        /// <summary>
        /// 效验位
        /// </summary>
        public Parity Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }

        /// <summary>
        /// 数据格式
        /// </summary>
        public DataFormatType DataFormat
        {
            get => _dataFormat;
            set => SetProperty(ref _dataFormat, value);
        }
    }
}