using JayTom.Dws.Camera;
using Prism.Mvvm;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Image = System.Windows.Controls.Image;

namespace JayTom.Dws.Client.Models {

    public class CameraItemInfoModel : BindableBase {
        private string _cameraName = string.Empty;
        private CameraType _type;
        private CameraStatus _status = CameraStatus.Disconnected;
        private double _frameRate;
        private string _cameraId = string.Empty;
        private bool _isSwitchingState;
        private long _imageTimestamp;
        private string _serialNumber = string.Empty;
        private ICamera? _camera;
        private bool _isRealtimeImageEnabled;
        private Image? _imageControl;
        private CancellationTokenSource tokenSource = new();

        public CameraItemInfoModel() {
            if (this is INotifyCollectionChanged notifyCollectionChanged) {
                notifyCollectionChanged.CollectionChanged +=
                    delegate (object? sender, NotifyCollectionChangedEventArgs args) {
                        if (args.Action == NotifyCollectionChangedAction.Remove && args.OldItems?.Contains(this) == true) {
                            //移除
                            BitmapQueue.Clear();
                            tokenSource.Cancel();
                        }
                    };
            }
            Task.Factory.StartNew(async () => {
                while (!tokenSource.IsCancellationRequested) {
                    var tryDequeue = BitmapQueue.TryDequeue(out var bitmap);
                    if (tryDequeue && bitmap is not null && this.Image is not null) {
                        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                        var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        await this.Image.Dispatcher.InvokeAsync(() => {
                            this.Image.WritePixels(new Int32Rect(0, 0, bitmap.Width, bitmap.Height), bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                            bitmap.UnlockBits(bitmapData);
                        }, DispatcherPriority.Render);
                    }
                    await Task.Delay(20);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// 图片队列
        /// </summary>
        public ConcurrentQueue<Bitmap> BitmapQueue { get; init; } = new();

        public string CameraId {
            get => _cameraId;
            set => SetProperty(ref _cameraId, value);
        }

        /// <summary>
        /// 图片
        /// </summary>
        public WriteableBitmap? Image { get; init; } = new(800, 600, 96, 96, PixelFormats.Bgr24, null);

        /// <summary>
        /// 图片时间戳
        /// </summary>
        public long ImageTimestamp {
            get => _imageTimestamp;
            set => SetProperty(ref _imageTimestamp, value);
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
        /// 序列号
        /// </summary>
        public string SerialNumber {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        /// <summary>
        /// 是否切换状态中
        /// </summary>
        public bool IsSwitchingState {
            get => _isSwitchingState;
            set => SetProperty(ref _isSwitchingState, value);
        }

        /// <summary>
        /// 相机
        /// </summary>
        public ICamera? Camera {
            get => _camera;
            set => SetProperty(ref _camera, value);
        }

        /// <summary>
        /// 是否开启实时图像
        /// </summary>
        public bool IsRealtimeImageEnabled {
            get => _isRealtimeImageEnabled;
            set => SetProperty(ref _isRealtimeImageEnabled, value);
        }

        /// <summary>
        /// 图像控件
        /// </summary>
        public Image? ImageControl {
            get => _imageControl;
            set => SetProperty(ref _imageControl, value);
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

        /// <summary>
        /// 开关实时图像
        /// </summary>
        public ICommand? SwitchRealtimeImageCommand { get; set; }

        /// <summary>
        /// 拍照
        /// </summary>
        public ICommand? TakePhotoCommand { get; set; }
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
        IndustrialCamera = 0,

        /// <summary>
        /// 全景相机
        /// </summary>
        PanoramicCamera = 1,

        /// <summary>
        /// 3D相机
        /// </summary>
        ThreeDCamera = 2,

        /// <summary>
        /// 智能相机
        /// </summary>
        SmartCamera = 3,

        /// <summary>
        /// 录像相机
        /// </summary>
        VideoCamera = 4,
    }

    public enum ConnectionType {

        /// <summary>
        /// USB连接
        /// </summary>
        Usb = 0,

        /// <summary>
        /// 网口连接
        /// </summary>
        Ethernet = 1,

        /// <summary>
        /// 串口连接
        /// </summary>
        SerialPort = 2,

        /// <summary>
        /// 蓝牙连接
        /// </summary>
        Bluetooth = 3,

        /// <summary>
        /// Tcp连接
        /// </summary>
        Tcp = 4
    }
}