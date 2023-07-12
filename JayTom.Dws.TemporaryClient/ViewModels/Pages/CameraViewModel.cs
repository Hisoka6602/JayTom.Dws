using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using JayTom.Dws.Device.Camera;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace JayTom.Dws.TemporaryClient.ViewModels.Pages {

    public class CameraViewModel : BindableBase, IDialogAware {
        private readonly IDialogService _dialogService;
        private readonly I3DCamera _camera;
        private ImageSource? _cameraImage = null;
        private string _volumeText;

        public CameraViewModel(IDialogService dialogService, I3DCamera camera) {
            _dialogService = dialogService;
            _camera = camera;
        }

        public ICommand CloseWinCommand {
            get => new DelegateCommand<object>(CloseWinDelegate);
        }

        public string VolumeText {
            get => _volumeText;
            set => SetProperty(ref _volumeText, value);
        }

        public ImageSource? CameraImage {
            get => _cameraImage;
            set => SetProperty(ref _cameraImage, value);
        }

        private void CloseWinDelegate(object obj) {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }

        public bool CanCloseDialog() {
            return true;
        }

        public void OnDialogClosed() {
            _camera.RealtimeImageEvent -= CameraOnRealtimeImageEvent;
            _camera.VolumeCapturedEvent -= CameraOnVolumeCapturedEvent;
        }

        public void OnDialogOpened(IDialogParameters parameters) {
            //加载图像
            _camera.RealtimeImageEvent += CameraOnRealtimeImageEvent;
            _camera.VolumeCapturedEvent += CameraOnVolumeCapturedEvent;
            _camera.ItemOutOfBounds += CameraOnItemOutOfBounds;
            _camera.ItemNotDetected += CameraOnItemNotDetected;
        }

        private async void CameraOnItemNotDetected(object? sender, EventArgs e) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                VolumeText = "Item not found";
            });
        }

        private async void CameraOnItemOutOfBounds(object? sender, ItemOutOfBoundsEventArgs e) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                VolumeText = $"Out of bounds:{e.Direction}";
            });
        }

        private async void CameraOnVolumeCapturedEvent(object? sender, VolumeCapturedEventArgs e) {
            //显示信息
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                VolumeText = $"Length:{e.Length:F2},Width:{e.Width:F2},Height:{e.Height:F2}";
            });
        }

        private async void CameraOnRealtimeImageEvent(object? sender, Bitmap e) {
            //显示图像
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                CameraImage = GetBitMapSourceFromBitmap((Bitmap)e);
            });
        }

        public string Title { get; } = "体积相机";

        public event Action<IDialogResult>? RequestClose;

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