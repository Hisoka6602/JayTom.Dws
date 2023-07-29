using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Regions;
using Prism.Commands;
using System.Windows;
using System.Drawing;
using System.Threading;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Device.Camera;
using System.Windows.Documents;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Client.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Repository.LocalData;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using CameraStatus = JayTom.Dws.Client.Models.CameraStatus;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class HomeViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly IDeviceService _deviceService;
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
        private static SemaphoreSlim _runningSemaphoreSlim = new(1, 1);

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
            IBarCodeRepository barCodeRepository, IDeviceService deviceService) {
            _dialogService = dialogService;
            _computerInfoReporter = computerInfoReporter;
            _barCodeRepository = barCodeRepository;
            _deviceService = deviceService;
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
            _computerInfoReporter.ComputerInfoReceived += delegate (object? sender, ComputerInfoModel model) {
                /*AddNewRow(new BarCodeItemModel() {
                    Num = BarCodeItems.Count + 1,
                    Barcode = new Random().Next(100000000, 999999999).ToString(),
                    ScanTime = DateTime.Now,
                    BarcodeImagePath = @"C:\Users\77051\Desktop\16.jpg",
                    IsBarcodeImageExists = true,
                    PanoramaImagePath = @"C:\Users\77051\Desktop\16.jpg",
                    IsPanoramaImageExists = true
                });*/
            };
            _deviceService.CameraInitialized += async delegate (object? sender, List<ICamera> list) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    CameraItems.Clear();
                    Task.Delay(100);
                    var infoModels = list.Select(s => new CameraItemInfoModel {
                        ConnectionType = (ConnectionType)s.ConnectionType,
                        CameraName = s.CameraName,
                        Type = (CameraType)s.CameraType,
                        Status = CameraStatus.Running,
                    })?.ToList();
                    CameraItems.AddRange(infoModels);
                });
            };
            _deviceService.BarcodeScanned += async delegate (object? sender, BarcodeHitEventArgs args) {
                //更新图片
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var model = CameraItems.FirstOrDefault(f => f.CameraName.Equals(args.CameraName));
                    if (model is not null) {
                        //图片转换
                        if (args?.Image is not null) {
                            if (args.Timestamp != model.ImageTimestamp) {
                                model.Image = null;
                                model.ImageTimestamp = args.Timestamp;
                                var thumbnailWidth = (int)(args.Image.Width * 0.3);
                                var thumbnailHeight = (int)(args.Image.Height * 0.3);
                                using var thumbnail = args.Image.GetThumbnailImage(thumbnailWidth, thumbnailHeight,
                                    null, IntPtr.Zero);
                                // 将缩略图转换为BitmapSource
                                model.Image = ((Bitmap)thumbnail).ConvertBitmapToBitmapSource();
                                BarCode = args.Barcode;
                                AddNewRow(new BarCodeItemModel() {
                                    Barcode = BarCode,
                                    ScanTime = args.ScanTime,
                                });
                            }
                        }
                        model.FrameRate = args?.FrameRate ?? 0;
                    }
                    //更新右边信息
                });
            };
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeHitEventArgs args) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    var model = CameraItems.FirstOrDefault(f => f.CameraName.Equals(args.CameraName));
                    if (model is not null) {
                        model.Image = null;
                        HomeMessageQueue.Enqueue("未识别到条码!");
                    }
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
            if (!obj.IsSwitchingState) {
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
                catch (Exception e) {
                }
                finally {
                    obj.IsSwitchingState = false;
                }
            }

            Console.WriteLine(obj);
        }

        /// <summary>
        /// 开始按钮点击
        /// </summary>
        public ICommand StartCommand {
            get => new DelegateCommand<CameraItemInfoModel>(StartDelegate);
        }

        private async void StartDelegate(CameraItemInfoModel obj) {
            await Task.Run(async () => {
                await _runningSemaphoreSlim.WaitAsync();
                if (!RunningStatus) {
                    //启动
                    var (key, value) = await _deviceService.Start();
                    //提示
                }
                else {
                    //停止
                    var (key, value) = await _deviceService.Stop();
                    //提示
                }

                await Application.Current.Dispatcher.InvokeAsync(() => {
                    RunningStatus = _deviceService.RunningStatus;
                });
                _runningSemaphoreSlim.Release();
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