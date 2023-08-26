using System;
using DryIoc;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using System.Drawing;
using System.Threading;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using System.Windows.Documents;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Client.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Service.ResultOutput;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using CameraStatus = JayTom.Dws.Client.Models.CameraStatus;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;
using static JayTom.Dws.Client.Service.BackgroundService.ScanProcessBackgroundService;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class HomeViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IResultOutputService _resultOutputService;
        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();
        private ObservableCollection<BarCodeItemModel> _barCodeItems = new();
        private DataGrid? _dataGrid = null;
        private int _totalDataCount;
        private int _uploadedDataCount;
        private int _abnormalDataCount;
        private bool _runningStatus;
        private SnackbarMessageQueue _homeMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _barCode = "包裹条码";
        private float _weight;
        private float _volume;
        private float _length;
        private float _width;
        private float _height;
        private bool _isSwitchingState;
        private static SemaphoreSlim _runningSemaphoreSlim = new(1, 1);
        private static SemaphoreSlim _imageSemaphoreSlim = new(1, 1);

        public SnackbarMessageQueue HomeMessageQueue {
            get => _homeMessageQueue;
            set => SetProperty(ref _homeMessageQueue, value);
        }

        public ObservableCollection<CameraItemInfoModel> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public ObservableCollection<BarCodeItemModel> BarCodeItems {
            get => _barCodeItems;
            set => SetProperty(ref _barCodeItems, value);
        }

        /// <summary>
        /// 总数
        /// </summary>
        public int TotalDataCount {
            get => _totalDataCount;
            set => SetProperty(ref _totalDataCount, value);
        }

        /// <summary>
        /// 上传数量
        /// </summary>
        public int UploadedDataCount {
            get => _uploadedDataCount;
            set => SetProperty(ref _uploadedDataCount, value);
        }

        /// <summary>
        /// 异常数量
        /// </summary>
        public int AbnormalDataCount {
            get => _abnormalDataCount;
            set => SetProperty(ref _abnormalDataCount, value);
        }

        /// <summary>
        /// 设备运行状态
        /// </summary>
        public bool RunningStatus {
            get => _runningStatus;
            set => SetProperty(ref _runningStatus, value);
        }

        /// <summary>
        /// 开关按钮切换状态
        /// </summary>
        public bool IsSwitchingState {
            get => _isSwitchingState;
            set => SetProperty(ref _isSwitchingState, value);
        }

        #region 条码信息

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        public float Weight {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 体积
        /// </summary>
        public float Volume {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        public float Length {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 宽度
        /// </summary>
        public float Width {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 高度
        /// </summary>
        public float Height {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        #endregion 条码信息

        public HomeViewModel(IDialogService dialogService,
            IComputerInfoReporter computerInfoReporter,
            IBarCodeRepository barCodeRepository, IDeviceService deviceService,
            IImageStorageService imageStorageService, IResultOutputService resultOutputService) {
            _dialogService = dialogService;
            _computerInfoReporter = computerInfoReporter;
            _barCodeRepository = barCodeRepository;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _resultOutputService = resultOutputService;
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
            };
            BarCodeItems = new()
            {
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = UploadStatus.Succeeded,
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)868.662,
                },
                new BarCodeItemModel()
                {
                    Barcode = "621055654309412",
                    BarcodeImagePath = "D:\\远程工具",
                    Height = (float)1.6,
                    Length = (float)1.8,
                    Num = 1,
                    RequestTime = DateTime.Now,
                    RequestContent = "上传内容",
                    RequestStatus = UploadStatus.Succeeded,
                    ResponseContent = "接口响应内容",
                    ResponseTime = DateTime.Now,
                    ScanTime = DateTime.Now,
                    Width = (float)1.3,
                    Weight = (float)8.6,
                },
            };
            _deviceService.CameraInitialized += async delegate (object? sender, List<ICamera> list) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    CameraItems.Clear();
                    Task.Delay(100);
                    var infoModels = list.Select(s => new CameraItemInfoModel {
                        ConnectionType = (ConnectionType)(s?.Info?.ConnectionType ?? CameraConnectionType.Ethernet),
                        CameraName = $"{s?.Info?.Brand}:{s?.Info?.SerialNumber}" ?? string.Empty,
                        Type = (CameraType)(s?.Info?.Type ?? JayTom.Dws.Camera.CameraType.IndustrialCamera),
                        Status = CameraStatus.Running,
                        CameraId = (s?.Info?.Id)?.ToString() ?? string.Empty,
                        SerialNumber = s?.Info?.SerialNumber ?? string.Empty,
                        Camera = s,
                        StatusClickCommand = StatusClickCommand
                    })?.ToList();
                    CameraItems.AddRange(infoModels);
                });
            };
            _deviceService.CameraReleased += async delegate (object? sender, string s) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(s));
                    if (model != null) {
                        CameraItems.Remove(model);
                    }
                });
            };
            _deviceService.BarcodeScanned += DeviceServiceOnBarcodeScanned;
            _deviceService.RealTimeImage += DeviceServiceOnRealTimeImage;
            _deviceService.PanoramaCaptured += DeviceServiceOnPanoramaCaptured;
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args) {
                await Application.Current.Dispatcher.InvokeAsync(async () => {
                    var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber));

                    if (model is not null) {
                        //图片转换
                        if (args?.Image is not null) {
                            if (args.Timestamp != model.ImageTimestamp) {
                                model.Image = null;
                                await Task.Delay(10);
                                model.ImageTimestamp = args.Timestamp;
                                // 将缩略图转换为BitmapSource
                                await Application.Current.Dispatcher.InvokeAsync(() => {
                                    //更新图片
                                    model.Image = args.ThumbImage.ConvertBitmapToBitmapSource();
                                    model.FrameRate = args?.FrameRate ?? 0;
                                    //更新右边信息
                                    BarCode = "未识别到条码";
                                });
                            }
                        }
                    }
                });
                AddNewRow(new BarCodeItemModel() {
                    Barcode = args.Barcode!,
                    ScanTime = args.ScanTime!,
                });
            };
            _deviceService.CameraDisconnected += delegate (object? sender, List<ICamera> list) {
                //更新现有列表,例如删除相机成员
            };
            _deviceService.DeviceException += async delegate (object? sender, DeviceExceptionEventArgs args) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    HomeMessageQueue.Enqueue(args?.ExceptionMessage?.Message ?? string.Empty);
                });

                //弹出提示框
            };
            _deviceService.StableWeight += async delegate (object? sender, StableWeightEventArgs args) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    Weight = args.Weight;
                });
            };
            _imageStorageService.ImageSaveFailed += async delegate (object? sender, Exception exception) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    HomeMessageQueue.Enqueue($"图片保存异常:{exception.Message}");
                });
            };
            _resultOutputService.OutputFailed += async delegate (object? sender, Exception exception) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    HomeMessageQueue.Enqueue($"结果输出异常:{exception.Message}");
                });
            };
            EventAggregator.Instance.Subscribe<ScanBarCodeInfo>(async Info => {
                //填充数据到列表
                if (Info is ScanBarCodeInfo model) {
                    AddNewRow(new BarCodeItemModel() {
                        Barcode = model.BarCode,
                        ScanTime = model.ScanTime,
                        Weight = (float)(model.Weight ?? 0),
                        Length = (float)(model.Length ?? 0),
                        Width = (float)(model.Width ?? 0),
                        Height = (float)(model.Height ?? 0)
                    });
                }
            });
        }

        private async void DeviceServiceOnPanoramaCaptured(object? sender, PanoramaCaptureEventArgs args) {
            //全景相机
            await _imageSemaphoreSlim.WaitAsync();
            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) &&
                                                        f.Type is CameraType.PanoramicCamera);
            if (model is not null) {
                //图片转换
                if (args?.ThumbImage is not null) {
                    if (args.Timestamp != model.ImageTimestamp) {
                        model.Image = null;
                        await Task.Delay(5);
                        model.ImageTimestamp = args.Timestamp;
                        await Application.Current.Dispatcher.BeginInvoke(() => {
                            //更新图片
                            model.Image = args.ThumbImage.ConvertBitmapToBitmapSource();
                        });
                    }
                }
            }
            _imageSemaphoreSlim.Release();
        }

        private async void DeviceServiceOnRealTimeImage(object? sender, RealTimeImageEventArgs args) {
            //实时画面
        }

        private async void DeviceServiceOnBarcodeScanned(object? sender, BarcodeReadEventArgs args) {
            //更新图片
            await _imageSemaphoreSlim.WaitAsync();

            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) &&
                                                        f.Type is CameraType.IndustrialCamera or CameraType.SmartCamera);
            if (model is not null) {
                //图片转换
                if (args?.ThumbImage is not null) {
                    if (args.Timestamp != model.ImageTimestamp) {
                        model.Image = null;
                        await Task.Delay(50);
                        model.ImageTimestamp = args.Timestamp;
                        await Application.Current.Dispatcher.BeginInvoke(() => {
                            //更新图片
                            model.Image = args.ThumbImage.ConvertBitmapToBitmapSource();
                            model.FrameRate = args?.FrameRate ?? 0;
                            //更新右边信息
                            BarCode = args?.Barcode ?? "未识别到条码";
                        });
                    }
                }
            }
            _imageSemaphoreSlim.Release();
        }

        /// <summary>
        /// 图像点击事件
        /// </summary>
        public ICommand ImageClickCommand {
            get => new DelegateCommand<CameraItemInfoModel>(ImageClickDelegate);
        }

        public ICommand UploadStatusCommand {
            get => new DelegateCommand<BarCodeItemModel>(UploadStatusDelegate);
        }

        public ICommand LoadedCommand {
            get => new DelegateCommand<Page>(LoadedDelegate);
        }

        private async void LoadedDelegate(Page obj) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                _dataGrid = Utils.GetVisualChild<DataGrid>(obj, b => b.Name.Equals("BarCodeDataGrid"));
            });
        }

        private void UploadStatusDelegate(BarCodeItemModel obj) {
            //判断状态是否已上传再获进行弹窗
            if (obj.RequestStatus != UploadStatus.NotUploaded) {
                _dialogService.ShowDialog("ApiAccessDialog", new DialogParameters { { "BarCodeItem", obj } }, null);
            }
        }

        private async void ImageClickDelegate(CameraItemInfoModel obj) {
            //放大图片(用另一个图像框显示、并重新绑定接收图像来源、过渡动画)
            /*await Application.Current.Dispatcher.InvokeAsync(() => {
                AddNewRow(new BarCodeItemModel() {
                    Barcode = new Random().Next(100000000, 999999999).ToString()
                });
            });*/
        }

        /// <summary>
        /// 状态点击事件
        /// </summary>
        public ICommand? StatusClickCommand {
            get => new DelegateCommand<CameraItemInfoModel>(StatusClickDelegate);
        }

        private async void StatusClickDelegate(CameraItemInfoModel obj) {
            //先加载进度条
            //临时截图
            if (obj.Camera is IIndustrialCamera industrialCamera) {
                await industrialCamera.TakePhotoAsync();
            }

            /*if (!obj.IsSwitchingState) {
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
                finally {
                    obj.IsSwitchingState = false;
                }
            }*/
        }

        /// <summary>
        /// 开始按钮点击
        /// </summary>
        public ICommand StartCommand {
            get => new DelegateCommand<CameraItemInfoModel>(StartDelegate);
        }

        private async void StartDelegate(CameraItemInfoModel obj) {
            await Task.Run(async () => {
                if (!IsSwitchingState) {
                    try {
                        await _runningSemaphoreSlim.WaitAsync();
                        IsSwitchingState = true;
                        if (!RunningStatus) {
                            //启动
                            var (key, value) = await _deviceService.Start();
                            //提示
                        }
                        else {
                            //停止
                            HomeMessageQueue.Clear();
                            var (key, value) = await _deviceService.Stop();
                            //提示
                        }

                        await Application.Current.Dispatcher.InvokeAsync(() => {
                            RunningStatus = _deviceService.RunningStatus;
                        });
                    }
                    finally {
                        _runningSemaphoreSlim.Release();
                        IsSwitchingState = false;
                    }
                }
            });
        }

        /// <summary>
        /// 添加一行
        /// </summary>
        private async void AddNewRow(BarCodeItemModel item) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                _barCodeRepository.InsertAsync(new BarCodeInfoModel() {
                    Barcode = item.Barcode,
                    Weight = item.Weight,
                    ScanTime = item.ScanTime,
                    PanoramaImagePath = item.PanoramaImagePath,
                    BarcodeImagePath = item.BarcodeImagePath,
                });
                item.Num = BarCodeItems.Count + 1;
                BarCodeItems.Insert(0, item);
                item.IsInserting = true;
                TotalDataCount += 1;
                if (item.RequestStatus == UploadStatus.Succeeded) {
                    UploadedDataCount += 1;
                }

                if (item.RequestStatus == UploadStatus.Failed) {
                    AbnormalDataCount += 1;
                }
            });
        }
    }
}