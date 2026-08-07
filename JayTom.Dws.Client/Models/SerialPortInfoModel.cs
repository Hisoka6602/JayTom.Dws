using Prism.Mvvm;

namespace JayTom.Dws.Client.Models
{

    public class SerialPortInfoModel : BindableBase
    {
        private string _name = string.Empty;
        private SerialPortType _type = SerialPortType.Other;
        private SerialPortStatus _status;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public SerialPortType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public SerialPortStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }

    public enum SerialPortType
    {

        /// <summary>
        /// 磅秤
        /// </summary>
        Scale,

        /// <summary>
        /// 下位机
        /// </summary>
        Controller,

        /// <summary>
        /// 相机
        /// </summary>
        Camera,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    public enum SerialPortStatus
    {

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 未连接
        /// </summary>
        NotConnected,
    }
}