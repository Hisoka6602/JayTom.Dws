using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class HomeViewModel : BindableBase {
        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();

        public ObservableCollection<CameraItemInfoModel> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public HomeViewModel() {
            CameraItems = new()
            {
                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.1",
                    Status = CameraStatus.Running,
                    Type = CameraType.IndustrialCamera,
                    ConnectionType = ConnectionType.Bluetooth,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.2",
                    Status = CameraStatus.Running,
                    Type = CameraType.PanoramicCamera,
                    ConnectionType = ConnectionType.Ethernet,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.2",
                    Status = CameraStatus.Running,
                    Type = CameraType.PanoramicCamera,
                    ConnectionType = ConnectionType.Ethernet,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                /*new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.3",
                    Status = CameraStatus.Failure,
                    Type = CameraType.SmartCamera,
                    ConnectionType = ConnectionType.SerialPort,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.1",
                    Status = CameraStatus.Paused,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Tcp,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.2",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Usb,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.3",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Bluetooth,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },

                new CameraItemInfoModel()
                {
                    CameraName = "海康工业相机.3",
                    Status = CameraStatus.Failure,
                    Type = CameraType.SmartCamera,
                    ConnectionType = ConnectionType.SerialPort,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.1",
                    Status = CameraStatus.Paused,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Tcp,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.2",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Usb,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },
                new CameraItemInfoModel()
                {
                    CameraName = "大华3D相机.3",
                    Status = CameraStatus.Disconnected,
                    Type = CameraType.ThreeDCamera,
                    ConnectionType = ConnectionType.Bluetooth,
                    ImageClickCommand = ImageClickCommand,
                    StatusClickCommand = StatusClickCommand,
                },*/
            };
        }

        /// <summary>
        /// 图像点击事件
        /// </summary>
        public ICommand ImageClickCommand {
            get => new DelegateCommand<CameraItemInfoModel>(ImageClickDelegate);
        }

        private void ImageClickDelegate(CameraItemInfoModel obj) {
            //放大图片(用另一个图像框显示、并重新绑定接收图像来源、过渡动画)
            Console.WriteLine(obj);
        }

        /// <summary>
        /// 状态点击事件
        /// </summary>
        public ICommand? StatusClickCommand {
            get => new DelegateCommand<CameraItemInfoModel>(StatusClickDelegate);
        }

        private async void StatusClickDelegate(CameraItemInfoModel obj) {
            //先加载进度条
            if (!obj.IsSwitchingState) {
                try {
                    obj.IsSwitchingState = true;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    obj.Status = obj.Status switch {
                        CameraStatus.Running => CameraStatus.Paused,
                        CameraStatus.Failure or CameraStatus.Paused or CameraStatus.Disconnected =>
                            CameraStatus.Running,
                        _ => obj.Status
                    };
                }
                catch (Exception e) {
                }
                finally {
                    obj.IsSwitchingState = false;
                }
            }

            Console.WriteLine(obj);
        }
    }
}