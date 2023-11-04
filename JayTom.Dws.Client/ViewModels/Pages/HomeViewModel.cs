using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Client.Service;
using JayTom.Dws.Data.LocalData;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Models.OcrSettingsModel;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Service.ExternalDataService;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using CameraStatus = JayTom.Dws.Client.Models.CameraStatus;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;
using ExceptionEventArgs = JayTom.Dws.Client.Service.Sorting.ExceptionEventArgs;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class HomeViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IBarCodeRepository _barCodeRepository;
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IExternalDataService _externalDataService;
        private readonly IConfigRepository _configRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private readonly ISortingService _sortingService;
        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();

        private ObservableCollection<BarCodeItemModel> _barCodeItems = new();

        private DataGrid? _dataGrid = null;
        private int _totalDataCount;
        private int _uploadedDataCount;
        private int _abnormalDataCount;
        private bool _runningStatus;
        private SnackbarMessageQueue _homeMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _barCode = Languages.Language.ResourceManager.GetString("BarCode") ?? string.Empty;
        private float _weight;
        private float _volume;
        private float _length;
        private float _width;
        private float _height;
        private bool _isSwitchingState;
        private VolumeUnit _volumeUnit;
        private static SemaphoreSlim _runningSemaphoreSlim = new(1, 1);
        private static SemaphoreSlim _imageSemaphoreSlim = new(1, 1);
        private static SemaphoreSlim _updateSlim = new(1, 1);
        private OcrSettingsInfoModel _ocrSettingsInfo = new();

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
        /// Ocr
        /// </summary>
        public OcrSettingsInfoModel OcrSettingsInfo {
            get => _ocrSettingsInfo;
            set => SetProperty(ref _ocrSettingsInfo, value);
        }

        /// <summary>
        /// 体积单位
        /// </summary>
        public VolumeUnit VolumeUnit {
            get => _volumeUnit;
            set => SetProperty(ref _volumeUnit, value);
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
            IImageStorageService imageStorageService,
            IResultOutputService resultOutputService,
            IExternalDataService externalDataService,
            IConfigRepository configRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository,
            ISortingService sortingService) {
            _dialogService = dialogService;
            _computerInfoReporter = computerInfoReporter;
            _barCodeRepository = barCodeRepository;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _resultOutputService = resultOutputService;
            _externalDataService = externalDataService;
            _configRepository = configRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _sortingService = sortingService;
            CameraItems = new() {
                /*new CameraItemInfoModel()
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
                },*/
            };
            BarCodeItems = new();
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
                        StatusClickCommand = StatusClickCommand,
                        TakePhotoCommand = TakePhotoCommand,
                        SwitchRealtimeImageCommand = SwitchRealtimeImageCommand,
                        IsRealtimeImageEnabled = s?.IsRealtimeImageEnabled ?? false
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

                    if (model?.Image != null) {
                        //图片转换
                        if (args?.ThumbImage is not null) {
                            if (args.Timestamp != model.ImageTimestamp) {
                                model.ImageTimestamp = args.Timestamp;
                                model.BitmapQueue.Enqueue(args.ThumbImage);
                            }
                        }
                    }

                    if (model is not null) {
                        //更新右边信息
                        model.FrameRate = args?.FrameRate ?? 0;
                        BarCode = args?.Barcode ?? "未识别到条码";
                    }
                });
            };
            _deviceService.CameraDisconnected += delegate (object? sender, List<ICamera> list) {
                //更新现有列表,例如删除相机成员
            };
            _deviceService.VolumeCaptured += DeviceServiceOnVolumeCaptured;
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
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("图片保存异常") ?? string.Empty}:{exception.Message}");
                });
            };
            _resultOutputService.OutputFailed += async delegate (object? sender, Exception exception) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("结果输出异常") ?? string.Empty}:{exception.Message}");
                });
            };
            //外部数据
            _externalDataService.ExternalDataException += async delegate (object? sender, Exception exception) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("外部输入异常") ?? string.Empty}:{exception.Message}");
                });
            };
            _externalDataService.VolumeReceived += async delegate (object? sender, ExternalVolumeInputEventArgs args) {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    Length = (float)args.Length;
                    Width = (float)args.Width;
                    Height = (float)args.Height;
                    Volume = (float)args.Volume;
                });
            };
            //分拣
            _sortingService.HeartbeatError += delegate (object? sender, Exception exception) {
                HomeMessageQueue.Enqueue($"{exception.Message}");
            };
            _sortingService.ExceptionOccurred += delegate (object? sender, ExceptionEventArgs args) {
                HomeMessageQueue.Enqueue($"{args.ExceptionMessage}");
            };

            EventAggregator.Instance.Subscribe<PackageInfo>(async info => {
                //填充数据到列表
                if (info is PackageInfo model) {
                    AddNewRow(new BarCodeItemModel() {
                        Barcode = model.BarCode,
                        ScanTime = model.ScanTime,
                        Weight = (float)(model.Weight ?? 0),
                        Length = (float)(model.Length ?? 0),
                        Width = (float)(model.Width ?? 0),
                        Height = (float)(model.Height ?? 0),
                        Volume = (float)(model.Volume ?? 0)
                    });
                }
            });
            EventAggregator.Instance.Subscribe<BarcodeTypeProviderEvent>(async info => {
                if (info is BarcodeTypeProviderEvent args) {
                    await Application.Current.Dispatcher.BeginInvoke(() => {
                        //更新右边信息
                        BarCode = args?.Barcode ?? "未识别到条码";
                    });
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async info => {
                try {
                    if (info is SettingsChangedEvent model) {
                        if (model.SettingsName.Equals("VolumeSettings")) {
                            await Application.Current.Dispatcher.InvokeAsync(async () => {
                                //临时写在这里加载配置，后续修改通过事件通知
                                var configInfoModel = await _configRepository.FirstOrDefault(s => s.ConfigName.Equals("VolumeSettings"));
                                if (configInfoModel is not null) {
                                    var volumeSettingsDto = JsonConvert.DeserializeObject<VolumeSettingsDto>(configInfoModel.Value);
                                    if (volumeSettingsDto is not null) {
                                        VolumeUnit = volumeSettingsDto.Unit;
                                    }
                                }
                            });
                        }
                        else if (model.SettingsName.Equals("OcrSettings")) {
                            await Application.Current.Dispatcher.InvokeAsync(async () => {
                                //临时写在这里加载配置，后续修改通过事件通知
                                var configInfoModel = await _configRepository.FirstOrDefault(s => s.ConfigName.Equals("OcrSettings"));
                                if (configInfoModel is not null) {
                                    var ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(configInfoModel.Value);
                                    if (ocrSettingsDto is not null) {
                                        OcrSettingsInfo = new OcrSettingsInfoModel() {
                                            IsShowSenderInfo = ocrSettingsDto.IsShowSenderInfo,
                                            IsUseOcr = ocrSettingsDto.IsUseOcr,
                                            IsShowCompartmentNumber = ocrSettingsDto.IsShowCompartmentNumber,
                                            IsShowLogisticsCompany = ocrSettingsDto.IsShowLogisticsCompany,
                                            IsShowReceiverInfo = ocrSettingsDto.IsShowReceiverInfo,
                                            IsShowRecognitionTime = ocrSettingsDto.IsShowRecognitionTime
                                        };
                                    }
                                }
                            });
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            });
            //更新上传状态
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(async item => {
                if (item is ApiResponseReceived model) {
                    try {
                        await _updateSlim.WaitAsync();
                        var barCodeItemModel = BarCodeItems.FirstOrDefault(f => f.Barcode.Equals(model.Barcode) &&
                            f.ScanTime.Equals(model.ScanTime));
                        if (barCodeItemModel is not null) {
                            await Application.Current.Dispatcher.BeginInvoke(() => {
                                //更新数据
                                barCodeItemModel.RequestStatus = model.UploadResponse?.IsSuccess == true ? UploadStatus.Succeeded : UploadStatus.Failed;
                                barCodeItemModel.UploadInfo = new UploadItemModel() {
                                    DurationInSeconds = model.UploadResponse?.Duration ?? 0,
                                    ExceptionMessage = model.UploadResponse?.ExceptionMsg ?? string.Empty,
                                    InterfaceParameters = model.UploadResponse?.ApiParameters ?? string.Empty,
                                    IsSuccess = model.UploadResponse?.IsSuccess ?? false,
                                    RequestContent = model.UploadResponse?.RequestContent ?? string.Empty,
                                    RequestTime = model.UploadResponse?.RequestTime,
                                    RequestUrl = model.UploadResponse?.RequestUrl ?? string.Empty,
                                    ResponseContent = model.UploadResponse?.ResponseContent ?? string.Empty,
                                    ResponseTime = model.UploadResponse?.ResponseTime
                                };
                                if (barCodeItemModel.RequestStatus == UploadStatus.Succeeded) {
                                    UploadedDataCount += 1;
                                }
                                if (barCodeItemModel.RequestStatus == UploadStatus.Failed) {
                                    AbnormalDataCount += 1;
                                }
                            }, DispatcherPriority.Background);
                        }
                    }
                    finally {
                        _updateSlim.Release();
                    }
                }
            });
            //更新分拣状态
            EventAggregator.Instance.Subscribe<InstructionReceived>(async item => {
                if (item is InstructionReceived model) {
                    try {
                        //设置分拣状态参数
                        await Task.Delay(500);
                        await _updateSlim.WaitAsync();
                        var barCodeItemModel = BarCodeItems.FirstOrDefault(f => f.Barcode.Equals(model.BarCode) &&
                            f.ScanTime.Equals(model.ScanTime));
                        if (barCodeItemModel is not null) {
                            await Application.Current.Dispatcher.BeginInvoke(() => {
                                //更新数据
                                barCodeItemModel.ExitName = model.ExitName;
                                barCodeItemModel.SortingInfo = new SortingItemModel() {
                                    IsSortingUsed = true,
                                    ExitId = model.ExitId,
                                    LogisticsId = model.LogisticsId,
                                    SortingMode = model.SortingMode,
                                    SentInstruction = model.SentInstruction,
                                    PackageCreationTime = model.PackageCreationTime,
                                    PackageCreationInstruction = model.PackageCreationInstruction,
                                    IsCreatedByLowerMachine = model.IsCreatedByLowerMachine,
                                    CommandTarget = model.CommandTarget,
                                    CommunicationMethod = model.CommunicationMethod,
                                    ChecksumProtocolName = model.ChecksumProtocolName,
                                };
                            }, DispatcherPriority.Background);
                        }
                    }
                    finally {
                        _updateSlim.Release();
                    }
                }
            });
        }

        private async void DeviceServiceOnVolumeCaptured(object? sender, VolumeCapturedEventArgs args) {
            await Application.Current.Dispatcher.BeginInvoke(() => {
                Length = (float)args.Length;
                Width = (float)args.Width;
                Height = (float)args.Height;
                Volume = (float)args.Volume;
            });
        }

        private async void DeviceServiceOnPanoramaCaptured(object? sender, PanoramaCaptureEventArgs args) {
            //全景相机
            await Task.Yield();
            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) && f.Type is CameraType.PanoramicCamera);
            if (model is not null &&
                model.Image is not null) {
                //图片转换
                if (args?.ThumbImage is not null) {
                    if (args.Timestamp != model.ImageTimestamp) {
                        //model.Image = null;

                        model.ImageTimestamp = args.Timestamp;
                        if (!model.IsRealtimeImageEnabled) {
                            model.BitmapQueue.Enqueue(args.ThumbImage);
                        }
                    }
                }
            }
        }

        private async void DeviceServiceOnRealTimeImage(object? sender, RealTimeImageEventArgs args) {
            //实时画面
            await Task.Yield();
            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.Camera?.Info?.SerialNumber));
            if (model is not null && args.Image is not null &&
                model.Image is not null) {
                if (model.IsRealtimeImageEnabled) {
                    model.BitmapQueue.Enqueue(args.Image);
                }
            }
        }

        private async void DeviceServiceOnBarcodeScanned(object? sender, BarcodeReadEventArgs args) {
            //更新图片

            var model = CameraItems.FirstOrDefault(f => f.SerialNumber.Equals(args.CameraSerialNumber) &&
                                                        f.Type is CameraType.IndustrialCamera or CameraType.SmartCamera);
            if (model is not null) {
                //图片转换
                if (args?.ThumbImage is not null &&
                    model.Image is not null) {
                    if (args.Timestamp != model.ImageTimestamp) {
                        model.ImageTimestamp = args.Timestamp;
                        if (!model.IsRealtimeImageEnabled) {
                            model.BitmapQueue.Enqueue(args.ThumbImage);
                        }
                        await Application.Current.Dispatcher.BeginInvoke(() => {
                            model.FrameRate = args?.FrameRate ?? 0;
                            //更新右边信息
                            BarCode = args?.Barcode ?? "未识别到条码";
                        });
                    }
                }
            }
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
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                _dataGrid = PluginInterface.Utils.Utils.GetVisualChild<DataGrid>(obj, b => b.Name.Equals("BarCodeDataGrid"));
                //临时写在这里加载配置，后续修改通过事件通知
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = "VolumeSettings"
                });
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = "OcrSettings"
                });
            });
        }

        private void UploadStatusDelegate(BarCodeItemModel obj) {
            //判断状态是否已上传再获进行弹窗
            if (obj.RequestStatus != UploadStatus.NotUploaded) {
                _dialogService.Show("ApiAccessDialog", new DialogParameters { { "BarCodeItem", obj } }, null);
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
        /// 开关实时图像
        /// </summary>
        public ICommand? SwitchRealtimeImageCommand {
            get => new DelegateCommand<CameraItemInfoModel>(SwitchRealtimeImageDelegate);
        }

        private async void SwitchRealtimeImageDelegate(CameraItemInfoModel obj) {
            if (obj.Camera is { } camera) {
                if (camera.IsRealtimeImageEnabled) {
                    camera.StopRealTimeImage();
                }
                else {
                    camera.StartRealTimeImage();
                }
                obj.IsRealtimeImageEnabled = camera.IsRealtimeImageEnabled;

                //保存到数据库
                if (camera.BindingType == CameraBindingType.ScannerCamera) {
                    var configInfoModel = await _barcodeScannerCameraConfigRepository.FirstOrDefault(f =>
                        camera.Info != null && f.SerialNumber.Equals(camera.Info.SerialNumber));
                    if (configInfoModel != null) {
                        configInfoModel.IsShowRealTimeImage = camera.IsRealtimeImageEnabled;
                        await _barcodeScannerCameraConfigRepository.InsertOrUpdate(configInfoModel);
                    }
                }
            }
        }

        /// <summary>
        /// 拍照
        /// </summary>
        public ICommand? TakePhotoCommand {
            get => new DelegateCommand<CameraItemInfoModel>(TakePhotoDelegate);
        }

        private async void TakePhotoDelegate(CameraItemInfoModel obj) {
            if (obj.Camera is { } camera) {
                camera.StopRealTimeImage();
                obj.IsRealtimeImageEnabled = camera.IsRealtimeImageEnabled;
                await camera.TakePhotoAsync(string.Empty, 0);
            }
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
                await industrialCamera.TakePhotoAsync(string.Empty, 0);
            }
            else if (obj.Camera is ISecurityCamera securityCamera) {
                await securityCamera.TakePhotoAsync(string.Empty, 0);
            }
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
                            await _externalDataService.Start();
                            var (key, value) = await _deviceService.Start();
                            await _sortingService.Start();
                            //提示
                        }
                        else {
                            //停止
                            HomeMessageQueue.Clear();
                            await _externalDataService.Stop();
                            var (key, value) = await _deviceService.Stop();
                            await _sortingService.Stop();
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
            await Application.Current.Dispatcher.InvokeAsync(async () => {
                item.Num = TotalDataCount += 1;
                try {
                    await Task.Delay(5);
                    await _updateSlim.WaitAsync();
                    BarCodeItems.Insert(0, item);
                    if (BarCodeItems.Count > 200) {
                        Application.Current.Dispatcher.InvokeAsync(() => {
                            BarCodeItems.RemoveAt(BarCodeItems.Count - 1);
                        }, DispatcherPriority.Render);
                    }
                }
                finally {
                    _updateSlim.Release();
                }

                item.IsInserting = true;
            }, DispatcherPriority.Render);
        }
    }
}