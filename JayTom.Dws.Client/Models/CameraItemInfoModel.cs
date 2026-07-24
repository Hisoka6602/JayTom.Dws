using System;
using Prism.Mvvm;
using System.Drawing;
using System.Windows;
using System.Threading;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using Image = System.Windows.Controls.Image;
using JayTom.Dws.Data.LocalConf.CameraConfig;

namespace JayTom.Dws.Client.Models {

    public class CameraItemInfoModel : BindableBase, IDisposable {
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
        /// <summary>
        /// 图像处理取消源。
        /// </summary>
        private readonly CancellationTokenSource _tokenSource = new();
        /// <summary>
        /// 有界图像队列。
        /// </summary>
        private readonly ConcurrentQueue<Bitmap> _bitmapQueue = new();
        /// <summary>
        /// 图像队列同步锁。
        /// </summary>
        private readonly object _bitmapQueueLock = new();
        /// <summary>
        /// 新图像到达信号。
        /// </summary>
        private readonly SemaphoreSlim _bitmapSignal = new(0, 1);
        /// <summary>
        /// 图像处理任务。
        /// </summary>
        private readonly Task _imageWorker;
        private CameraDisplayStatus _cameraDisplayStatus;

        public CameraItemInfoModel() {
            _imageWorker = Task.Run(async () => {
                while (!_tokenSource.IsCancellationRequested) {
                    try {
                        await _bitmapSignal.WaitAsync(_tokenSource.Token).ConfigureAwait(false);
                        Bitmap? bitmap = null;
                        lock (_bitmapQueueLock) {
                            while (_bitmapQueue.TryDequeue(out var queuedBitmap)) {
                                bitmap?.Dispose();
                                bitmap = queuedBitmap;
                            }
                        }
                        var image = Image;
                        if (bitmap is not null && image is not null) {
                            using (bitmap) {
                                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                                var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly,
                                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                await image.Dispatcher.InvokeAsync(() => {
                                    try {
                                        image.WritePixels(
                                            new Int32Rect(0, 0, bitmap.Width, bitmap.Height),
                                            bitmapData.Scan0,
                                            bitmapData.Stride * bitmapData.Height,
                                            bitmapData.Stride
                                        );
                                    }
                                    finally {
                                        bitmap.UnlockBits(bitmapData);
                                    }
                                }, DispatcherPriority.Background).Task.ConfigureAwait(false);
                            }
                        }
                        else {
                            bitmap?.Dispose();
                        }
                    }
                    catch (OperationCanceledException) when (_tokenSource.IsCancellationRequested) {
                        break;
                    }
                    catch (Exception ex) {
                        NLog.LogManager.GetCurrentClassLogger().Error(ex, "Error processing image");
                    }
                }
                ClearImages();
            });
        }

        /// <summary>
        /// 将图像加入有界显示队列，只保留最新两帧。
        /// </summary>
        /// <param name="bitmap">待显示图像。</param>
        public void EnqueueImage(Bitmap bitmap) {
            lock (_bitmapQueueLock) {
                while (_bitmapQueue.Count >= 2 && _bitmapQueue.TryDequeue(out var staleBitmap)) {
                    staleBitmap.Dispose();
                }
                _bitmapQueue.Enqueue(bitmap);
                if (_bitmapSignal.CurrentCount == 0) {
                    _bitmapSignal.Release();
                }
            }
        }

        /// <summary>
        /// 仅在时间戳更新时将图像加入显示队列，避免并发事件重复刷新。
        /// </summary>
        /// <param name="bitmap">待显示图像。</param>
        /// <param name="timestamp">图像时间戳。</param>
        /// <returns>成功接收新图像时返回 <see langword="true"/>。</returns>
        public bool TryEnqueueImage(Bitmap bitmap, long timestamp) {
            while (true) {
                var currentTimestamp = Volatile.Read(ref _imageTimestamp);
                if (timestamp <= currentTimestamp) {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _imageTimestamp, timestamp, currentTimestamp) ==
                    currentTimestamp) {
                    EnqueueImage(bitmap);
                    return true;
                }
            }
        }

        /// <summary>
        /// 清空并释放待显示图像。
        /// </summary>
        public void ClearImages() {
            lock (_bitmapQueueLock) {
                while (_bitmapQueue.TryDequeue(out var bitmap)) {
                    bitmap.Dispose();
                }
            }
        }

        /// <summary>
        /// 停止图像处理并释放排队图像。
        /// </summary>
        public void Dispose() {
            if (_tokenSource.IsCancellationRequested) {
                return;
            }
            _tokenSource.Cancel();
            lock (_bitmapQueueLock) {
                if (_bitmapSignal.CurrentCount == 0) {
                    _bitmapSignal.Release();
                }
            }
            ClearImages();
        }

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
            get => Volatile.Read(ref _imageTimestamp);
            set {
                if (Interlocked.Exchange(ref _imageTimestamp, value) != value) {
                    RaisePropertyChanged();
                }
            }
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
        public CameraConnectionType ConnectionType { get; set; }

        /// <summary>
        /// 相机绑定类型
        /// </summary>

        public CameraBindingType BindingType { get; set; }

        /// <summary>
        /// 主页显示状态
        /// </summary>
        public CameraDisplayStatus CameraDisplayStatus {
            get => _cameraDisplayStatus;
            set => SetProperty(ref _cameraDisplayStatus, value);
        }

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

        /// <summary>
        /// 点击隐藏事件
        /// </summary>
        public ICommand? HideCommand { get; set; }

        /// <summary>
        /// 点击显示事件
        /// </summary>
        public ICommand? ShowCommand { get; set; }
    }
}
