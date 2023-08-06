using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using Newtonsoft.Json;
using JayTom.Dws.Camera;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using JayTom.Dws.Camera.Cameras.IndustrialCamera.Hikvision;

namespace Wpf.HikvisionIndustrialCameraTest {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private HikvisionIndustrialCamera camera;

        public MainWindow() {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            camera = new HikvisionIndustrialCamera() {
                IsRealtimeImageEnabled = false,
                BarcodeBorderSize = 4
            };
            camera.BarcodeRead += async delegate (object? sender, BarcodeReadEventArgs eventArgs) {
                await Task.Yield();
                await Task.Delay(20);
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    if (eventArgs?.ThumbImage != null) {
                        DevImage.Source = ConvertBitmapToBitmapSource(eventArgs.ThumbImage);
                    }

                    BarCodeListBox.Items.Add($"获取到条码:{eventArgs.Barcode}");
                });
            };
            camera.CameraExceptionOccurred += async delegate (object? sender, CameraExceptionEventArgs eventArgs) {
                await Application.Current.Dispatcher.InvokeAsync(() => { BarCodeListBox.Items.Add(eventArgs?.Exception?.Message); });
            };
            camera.RealtimeImage += async delegate (object? sender, RealtimeImageEventArgs args) {
                await Task.Yield();
                await Task.Delay(20);
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    if (args?.ThumbImage != null) {
                        DevImage.Source = ConvertBitmapToBitmapSource(args.ThumbImage);
                    }
                });
            };
            camera.CameraInitialized += async delegate (object? sender, CameraInitializedEventArgs args) {
                await Application.Current.Dispatcher.InvokeAsync(() => { BarCodeListBox.Items.Add($"初始化完成:{JsonConvert.SerializeObject(args.CameraInfo)}"); });
            };
            camera.CameraStarted += async delegate (object? sender, CameraStartedEventArgs args) {
                await Application.Current.Dispatcher.InvokeAsync(() => { BarCodeListBox.Items.Add($"启动成功:{JsonConvert.SerializeObject(args.CameraInfo)}"); });
            };
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
        }

        private async void OpenDevButton_OnClick(object sender, RoutedEventArgs e) {
            var infos = camera.EnumerateCameras();

            await camera.Initialize(infos[1]);
            await camera.Start(string.Empty);
        }

        /*private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap) {
            BitmapSource bitmapSource;
            using var memory = new System.IO.MemoryStream();
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;
            var bitmapDecoder = BitmapDecoder.Create(memory, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            bitmapSource = bitmapDecoder.Frames[0];
            return bitmapSource;
        }*/

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap) {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try {
                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();  // 提前冻结位图以避免后续的不必要操作
                return bitmapSource;
            }
            finally {
                // 释放 GDI 对象
                DeleteObject(hBitmap);
            }
        }
    }
}