using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using Newtonsoft.Json;
using System.Diagnostics;
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
using Image = System.Drawing.Image;
using System.Runtime.InteropServices;

namespace WpfApp2 {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            var loader = new TyPmLoader() {
                IsShowBorder = true
            };
            //实时图像回调
            loader.RealTimeImageEvent += async delegate (object? o, Image image) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    Image1.Source = GetBitMapSourceFromBitmap((Bitmap)image);
                });
            };
            //RGB色彩图像回调
            loader.RealTimeRgbImageEvent += async delegate (object? o, Image image) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    Image2.Source = GetBitMapSourceFromBitmap((Bitmap)image);
                });
            };
            //体积数据回调
            loader.VolumeDataCaptureEvent += delegate (object? o, Dimensions dimensions) {
                Debug.WriteLine(JsonConvert.SerializeObject(dimensions));
            };
            loader.Captured3DImage += delegate (object? o, byte[] bytes) {
                Debug.WriteLine("捕抓到3D图");
            };
            loader.ExceptionLogged += delegate (object? o, Exception exception) {
                Debug.WriteLine(exception.Message);
            };
            loader.ItemNotDetected += delegate (object? o, EventArgs args) {
                Debug.WriteLine("未检测到物品");
            };
            loader.ItemOutOfBounds += delegate (object? o, ItemOutOfBoundsEventArgs args) {
                Debug.WriteLine($"物品超出检测边缘:{args.Direction}");
            };
            //初始化
            loader.InitializeApp();
            //连接
            loader.Connect();
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Bitmap->BitmapSource
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapSource GetBitMapSourceFromBitmap(Bitmap bitmap) {
            var intPtrl = bitmap.GetHbitmap();
            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(intPtrl,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(intPtrl);
            return bitmapSource;
        }
    }
}