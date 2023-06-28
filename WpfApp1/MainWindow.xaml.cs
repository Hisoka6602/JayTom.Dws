using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using JayTom.Dws.Device;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Documents;
using JayTom.Dws.Device.Camera;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using JayTom.Dws.Device.Camera._3DCamera;
using JayTom.Dws.Device.Camera.SmartCamera;

namespace WpfApp1 {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private HuaraytechSmartCamera? _smartCamera;
        private Percipio3DCamera? percipio3DCamera;

        public MainWindow() {
            InitializeComponent();
            this.Loaded += OnLoaded;
            this.Closed += OnClosed;
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                // 获取要加载的程序集名称
                var assemblyName = new AssemblyName(args.Name).Name;

                Debug.WriteLine(assemblyName);
                return null;
            };
        }

        private void OnClosed(object? sender, EventArgs e) {
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            /*_smartCamera ??= new HuaraytechSmartCamera();
            _smartCamera.Excepted += delegate (object? o, Exception exception) {
                Application.Current.Dispatcher.Invoke(() => {
                    CodeInfoListView.Items.Add(exception?.Message);
                });
            };
            _smartCamera.Connected += delegate (object? o, IDevice device) {
                Application.Current.Dispatcher.Invoke(() => {
                    CodeInfoListView.Items.Add("设备已连接");
                });
            };
            _smartCamera.Disconnected += delegate (object? o, IDevice device) {
                Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add("设备已断开"); });
            };
            _smartCamera.Initialized += delegate (object? o, IDevice device) {
                Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add("设备已初始化"); });
            };
            _smartCamera.BarcodeHitEvent += delegate (object? o, BarcodeHitEventArgs args) {
                Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add($"扫到条码:{args.Barcode}"); });
                /*if (args?.Image is not null) {
                    CameraImage.Source = Imaging.CreateBitmapSourceFromHBitmap(
                        args.Image.GetHbitmap(),
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(args.Image.Width, args.Image.Height)
                    );
                }#1#
            };*/
            percipio3DCamera = new Percipio3DCamera();
            percipio3DCamera.Connected += delegate (object? o, IDevice device) {
                Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add("设备已连接"); });
            };
            percipio3DCamera.Excepted += delegate (object? o, Exception exception) {
                Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add(exception.Message); });
            };
            percipio3DCamera.DeviceWarning += delegate (object? o, string s) {
                Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add($"警告:{s}"); });
            };
        }

        private async void InitializationButton_OnClick(object sender, RoutedEventArgs e) {
            //var (_, value) = await _smartCamera?.Initialization()!;
            var (_, value) = await percipio3DCamera?.Initialization()!;
            Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add(value); });
        }

        private async void ConnectButton_OnClick(object sender, RoutedEventArgs e) {
            var (key, value) = await percipio3DCamera?.Connect("aa")!;
            Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add(value); });
        }

        private void DisposeButton_OnClick(object sender, RoutedEventArgs e) {
            percipio3DCamera?.Dispose();
        }
    }
}