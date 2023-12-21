using System.Text;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Media.Imaging;
using System.Windows.Forms.Integration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;

namespace BaseDaHuatech.Wpf {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            var pictureBox = new PictureBox() {
                Width = 600,
                Height = 600,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.None,
            };
            winFormsHost.Child = pictureBox;
        }
    }
}