using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JayTom.Dws.Client.Models {

    public class CameraItemInfoModel : BindableBase {
        private ImageSource? _image;
        private string _cameraName = string.Empty;
        private CameraType _type;
        private CameraStatus _status = CameraStatus.Disconnected;
        private double _frameRate;
        private int _cameraId;
        private bool _isSwitchingState;

        public int CameraId {
            get => _cameraId;
            set => SetProperty(ref _cameraId, value);
        }

        /// <summary>
        /// 图片
        /// </summary>
        public ImageSource? Image {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName {
            get => _cameraName;
            set => SetProperty(ref _cameraName, value);
        }

        /// <summary>
        /// 相机类型
        /// </summary>
        public CameraType Type {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 相机状态
        /// </summary>
        public CameraStatus Status {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 帧率
        /// </summary>
        public double FrameRate {
            get => _frameRate;
            set => SetProperty(ref _frameRate, value);
        }

        /// <summary>
        /// 是否切换状态中
        /// </summary>
        public bool IsSwitchingState {
            get => _isSwitchingState;
            set => SetProperty(ref _isSwitchingState, value);
        }

        /// <summary>
        /// 相机连接类型
        /// </summary>
        public ConnectionType ConnectionType { get; set; }

        /// <summary>
        /// 图像点击事件
        /// </summary>
        public ICommand? ImageClickCommand { get; set; }

        /// <summary>
        /// 状态点击事件
        /// </summary>
        public ICommand? StatusClickCommand { get; set; }
    }

    public enum CameraStatus {

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 故障
        /// </summary>
        Failure,

        /// <summary>
        /// 暂停中
        /// </summary>
        Paused
    }

    public enum CameraType {

        /// <summary>
        /// 工业相机
        /// </summary>
        IndustrialCamera,

        /// <summary>
        /// 全景相机
        /// </summary>
        PanoramicCamera,

        /// <summary>
        /// 3D相机
        /// </summary>
        ThreeDCamera,

        /// <summary>
        /// 智能相机
        /// </summary>
        SmartCamera
    }

    public enum ConnectionType {

        /// <summary>
        /// USB连接
        /// </summary>
        Usb,

        /// <summary>
        /// 网口连接
        /// </summary>
        Ethernet,

        /// <summary>
        /// 串口连接
        /// </summary>
        SerialPort,

        /// <summary>
        /// 蓝牙连接
        /// </summary>
        Bluetooth,

        /// <summary>
        /// Tcp连接
        /// </summary>
        Tcp
    }
}