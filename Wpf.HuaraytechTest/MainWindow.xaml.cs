using System;
using MVSDK_Net;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Wpf.HuaraytechTest {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private int _res = IMVDefine.IMV_OK;
        private MyCamera _camera = new();
        private IMVDefine.IMV_DeviceList deviceList = new();

        public MainWindow() {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            //创建对象、定义事件
            _camera = new MyCamera();
            //枚举设备
            _res = MyCamera.IMV_EnumDevices(ref deviceList,
                (uint)IMVDefine.IMV_EInterfaceType.interfaceTypeAll);
            if (_res != IMVDefine.IMV_OK) {
                MessageBox.Show($"枚举相机失败:{_res}");
            }
        }

        private void OpenDevButton_OnClick(object sender, RoutedEventArgs e) {
            //创建句柄
            _res = _camera.IMV_CreateHandle(IMVDefine.IMV_ECreateHandleMode.modeByIndex, 0);
            if (_res != IMVDefine.IMV_OK) {
                MessageBox.Show($"创建句柄失败:{_res}");
            }
            //打开设备
            _res = _camera.IMV_Open();
            if (_res != IMVDefine.IMV_OK) {
                MessageBox.Show($"打开设备失败:{_res}");
            }
            //注册回调函数
            _camera.IMV_AttachGrabbing(FrameCallBack, IntPtr.Zero);
        }

        private void FrameCallBack(ref IMVDefine.IMV_Frame frame, IntPtr pUser) {
            frame.frameInfo.

            //frame.frameHandle图像句柄
        }
    }
}