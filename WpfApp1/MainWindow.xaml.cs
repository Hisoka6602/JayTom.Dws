using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Device;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Windows.Controls;
using System.Windows.Documents;
using JayTom.Dws.Device.Camera;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Image = System.Drawing.Image;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using JayTom.Dws.Device.Camera._3DCamera;
using JayTom.Dws.Device.Camera.SmartCamera;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace WpfApp1 {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private HuaraytechSmartCamera? _smartCamera;
        private Percipio3DCamera? percipio3DCamera;
        private WayzimSmartCamera? _wayzimSmartCamera;
        private ICamera _camera;
        private WriteableBitmap cameraImageBitmap;
        private BitmapSource cameraBitmapSource;
        private static SemaphoreSlim semaphoreSlim = new(1, 1);
        private long _imageTimestamp = 0;

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public MainWindow() {
            InitializeComponent();
            this.Loaded += OnLoaded;
            this.Closed += OnClosed;
        }

        private void OnClosed(object? sender, EventArgs e) {
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            var startNew = new TaskFactory().StartNew(() => {
                while (true) {
                    GC.Collect();
                    Thread.Sleep(1000);
                }
            }, TaskCreationOptions.LongRunning);
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
            //_camera = new WayzimSmartCamera();

            _camera = new HuaraytechSmartCamera();
            _camera.Connected += async delegate (object? o, IDevice device) {
                await Application.Current.Dispatcher.InvokeAsync(() => { CodeInfoListView.Items.Add("设备已连接"); });
            };
            _camera.Excepted += async delegate (object? o, Exception exception) {
                await Application.Current.Dispatcher.InvokeAsync(() => { CodeInfoListView.Items.Add(exception.Message); });
            };
            //_camera.BarcodeHitEvent += CameraOnBarcodeHitEvent;
            _camera.RealtimeImageEvent += CameraOnRealtimeImageEvent;
            /*_camera.DeviceWarning += delegate (object? o, string s) {
                Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add($"警告:{s}"); });
            };*/
        }

        private async void CameraOnRealtimeImageEvent(object? sender, RealtimeImageEventArgs args) {
            if (args?.Bitmap is not null) {
                try {
                    await Task.Delay(10);
                    await semaphoreSlim.WaitAsync();
                    int thumbnailWidth = (int)(args.Bitmap.Width * 0.3);
                    int thumbnailHeight = (int)(args.Bitmap.Height * 0.3);
                    using var thumbnail = args.Bitmap.GetThumbnailImage(thumbnailWidth, thumbnailHeight,
                        null, IntPtr.Zero);
                    // 将缩略图转换为BitmapSource
                    var bitmapSource = ConvertBitmapToBitmapSource((Bitmap)thumbnail);
                    // 使用缩略图更新CameraImage.Source
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        // 使用缩略图更新CameraImage.Source
                        CameraImage.Source = bitmapSource;
                    }, DispatcherPriority.Background);
                }
                finally {
                    semaphoreSlim.Release();
                }
            }
        }

        private async void CameraOnBarcodeHitEvent(object? sender, BarcodeHitEventArgs args) {
            /*if (args?.Image is not null) {
                args.Image = null;
                //args.Image.Dispose();
            }*/
            if (args?.Image is not null) {
                if (_imageTimestamp != args.Timestamp) {
                    _imageTimestamp = args.Timestamp;
                    try {
                        await Task.Delay(50);
                        await semaphoreSlim.WaitAsync();
                        int thumbnailWidth = (int)(args.Image.Width * 0.3);
                        int thumbnailHeight = (int)(args.Image.Height * 0.3);
                        using var thumbnail = args.Image.GetThumbnailImage(thumbnailWidth, thumbnailHeight,
                            null, IntPtr.Zero);
                        // 将缩略图转换为BitmapSource
                        var bitmapSource = ConvertBitmapToBitmapSource((Bitmap)thumbnail);
                        // 使用缩略图更新CameraImage.Source
                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            // 使用缩略图更新CameraImage.Source
                            CameraImage.Source = bitmapSource;
                        }, DispatcherPriority.Background);
                    }
                    finally {
                        semaphoreSlim.Release();
                    }
                }
            }
            await Application.Current.Dispatcher.InvokeAsync(() => {
                CodeInfoListView.Items.Add($"扫到条码:{args?.Barcode},相机:{args?.CameraId}");
            });
        }

        private async void InitializationButton_OnClick(object sender, RoutedEventArgs e) {
            //var (_, value) = await _smartCamera?.Initialization()!;
            var (_, value) = await _camera?.Initialization()!;
            Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add(value == string.Empty ? "初始化成功" : value); });
        }

        private async void ConnectButton_OnClick(object sender, RoutedEventArgs e) {
            var (key, value) = await _camera?.Connect("aa")!;
            Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add(value); });
        }

        private void DisposeButton_OnClick(object sender, RoutedEventArgs e) {
            _camera.BarcodeHitEvent -= CameraOnBarcodeHitEvent;

            GC.Collect();
            /*_camera?.Dispose();
            Application.Current.Dispatcher.Invoke(() => { CodeInfoListView.Items.Add("断开"); });*/
        }

        private BitmapSource ConvertBitmapToBitmapSource(Image bitmap) {
            BitmapSource bitmapSource;
            using var memory = new System.IO.MemoryStream();
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;
            var bitmapDecoder = BitmapDecoder.Create(memory, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            bitmapSource = bitmapDecoder.Frames[0];
            return bitmapSource;
        }
    }

    public class BarcodeInfo {
        public string Barcode { get; set; }
        public BitmapSource BitmapSource { get; set; }
        public string CameraName { get; set; }
    }
}