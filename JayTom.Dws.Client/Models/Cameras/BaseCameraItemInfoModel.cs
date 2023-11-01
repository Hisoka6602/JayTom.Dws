using Prism.Mvvm;

namespace JayTom.Dws.Client.Models.Cameras {

    public class BaseCameraItemInfoModel : BindableBase {
        private string _name = string.Empty;
        private string _serialNumber = string.Empty;
        private string _model = string.Empty;
        private string _version = string.Empty;
        private string _ipAddress = string.Empty;
        private CameraType _cameraType;
        private ConnectionType _connectionType = 0;
        private int _num;
        private string _brand = string.Empty;

        /// <summary>
        /// 序号
        /// </summary>
        public int Num {
            get => _num;
            set => SetProperty(ref _num, value);
        }

        /// <summary>
        /// 相机名称
        /// </summary>

        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 相机序列号
        /// </summary>

        public string SerialNumber {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        /// <summary>
        /// 相机型号
        /// </summary>

        public string Model {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// 相机固件版本
        /// </summary>

        public string Version {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>
        /// 相机 IP 地址
        /// </summary>

        public string IpAddress {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// 相机类型
        /// </summary>

        public CameraType CameraType {
            get => _cameraType;
            set => SetProperty(ref _cameraType, value);
        }

        public string Brand {
            get => _brand;
            set => SetProperty(ref _brand, value);
        }

        /// <summary>
        /// 连接方式
        /// </summary>

        public ConnectionType ConnectionType {
            get => _connectionType;
            set => SetProperty(ref _connectionType, value);
        }
    }
}