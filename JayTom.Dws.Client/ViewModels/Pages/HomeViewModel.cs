using System;
using DryIoc;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using JayTom.Dws.Ocr;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.License;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using System.Windows.Controls;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using JayTom.Dws.Client.Service;
using JayTom.Dws.Domain.Manager;
using JayTom.Dws.Interface.Cloud;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using JayTom.Dws.Domain.Dto.AppDto;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Domain.Interface.Cloud;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Domain.Interface.License;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Interface.Attributes;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Client.Models.OcrSettingsModel;
using LogType = JayTom.Dws.Data.LocalLog.LogType;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels;
using ExceptionEventArgs = JayTom.Dws.Client.Service.Sorting.ExceptionEventArgs;

namespace JayTom.Dws.Client.ViewModels.Pages {

    public class HomeViewModel : BindableBase {
        private readonly IDialogService _dialogService;
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IExternalDataService _externalDataService;
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly IClientLicenseApi _clientLicenseApi;
        private readonly IContainer _container;

        private ObservableCollection<PackageItemModel> _packageItems = new();

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

        private static SemaphoreSlim _updateSlim = new(1, 1);
        private OcrSettingsInfoModel _ocrSettingsInfo = new();

        private OcrInfoItemModel _ocrItemInfo = new();
        private bool _isLoaded;
        private CancellationTokenSource _cancellationTokenSource = new();
        private ConcurrentQueue<PackageExitUpdateEvent> _packageExitUpdateItems = new();

        private ConcurrentQueue<CloudVideoUploadMessage> _cloudVideoUploadItems = new();
        private BindableBase? _currentViewModel;

        public SnackbarMessageQueue HomeMessageQueue {
            get => _homeMessageQueue;
            set => SetProperty(ref _homeMessageQueue, value);
        }

        public ObservableCollection<PackageItemModel> PackageItems {
            get => _packageItems;
            set => SetProperty(ref _packageItems, value);
        }

        /// <summary>
        /// Ocr设置
        /// </summary>
        public OcrSettingsInfoModel OcrSettingsInfo {
            get => _ocrSettingsInfo;
            set => SetProperty(ref _ocrSettingsInfo, value);
        }

        /// <summary>
        /// Ocr显示信息
        /// </summary>
        public OcrInfoItemModel OcrItemInfo {
            get => _ocrItemInfo;
            set => SetProperty(ref _ocrItemInfo, value);
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

        /// <summary>
        /// 左边的实时视图
        /// </summary>
        public BindableBase? CurrentViewModel {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
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
            IDeviceService deviceService,
            IImageStorageService imageStorageService,
            IResultOutputService resultOutputService,
            IExternalDataService externalDataService,
            IConfigRepository configRepository,
            ISortingService sortingService,
            IClientLicenseApi clientLicenseApi,
            IContainer container) {
            _dialogService = dialogService;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _resultOutputService = resultOutputService;
            _externalDataService = externalDataService;
            _configRepository = configRepository;
            _sortingService = sortingService;
            _clientLicenseApi = clientLicenseApi;
            _container = container;
            _deviceService.BarcodeScanned += DeviceServiceOnBarcodeScanned;
            _deviceService.NotBarcodeHitEvent += async delegate (object? sender, BarcodeReadEventArgs args) {
                await Application.Current.Dispatcher.BeginInvoke(async () => {
                    BarCode = args?.Barcode ?? "未识别到条码";
                });
            };
            _deviceService.CameraDisconnected += delegate (object? sender, List<ICamera> list) {
                //更新现有列表,例如删除相机成员
            };
            _deviceService.VolumeCaptured += DeviceServiceOnVolumeCaptured;
            _deviceService.DeviceException += async delegate (object? sender, DeviceExceptionEventArgs args) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    HomeMessageQueue.Enqueue(args?.ExceptionMessage?.Message ?? string.Empty);
                });

                //弹出提示框
            };
            _deviceService.StableWeight += async delegate (object? sender, StableWeightEventArgs args) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    Weight = args.Weight;
                });
            };
            _deviceService.OcrContentRecognized += DeviceServiceOnOcrContentRecognized;
            _deviceService.BarCodeKeyReceived += async (sender, s) => {
                await Application.Current.Dispatcher.BeginInvoke(async () => {
                    BarCode = s.Barcode;
                });
            };

            _imageStorageService.ImageSaveFailed += async delegate (object? sender, Exception exception) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("图片保存异常") ?? string.Empty}:{exception.Message}");
                });
            };
            _resultOutputService.OutputFailed += async delegate (object? sender, Exception exception) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("结果输出异常") ?? string.Empty}:{exception.Message}");
                });
            };
            //外部数据
            _externalDataService.ExternalDataException += async delegate (object? sender, Exception exception) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("外部输入异常") ?? string.Empty}:{exception.Message}");
                });
            };
            //外部全量数据
            _externalDataService.ContentInputReceived += async (sender, args) => {
                await Application.Current.Dispatcher.BeginInvoke(async () => {
                    BarCode = args?.Barcode ?? "未解析到条码";
                    Weight = args?.Weight ?? 0;
                    Length = args?.Length ?? 0;
                    Width = args?.Width ?? 0;
                    Height = args?.Height ?? 0;
                    Volume = args?.Volume ?? 0;
                });
            };
            _externalDataService.VolumeReceived += async delegate (object? sender, ExternalVolumeInputEventArgs args) {
                await Application.Current.Dispatcher.BeginInvoke(() => {
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
            EventAggregator.Instance.Subscribe<PackageInfo>(info => {
                //填充数据到列表

                if (info is { } model) {
                    AddNewRow(new PackageItemModel() {
                        Barcode = model.BarCodeInfo?.Barcode ?? string.Empty,
                        ScanTime = model.BarCodeInfo?.ScanTime ?? DateTime.Now,
                        Weight = (float)(model.WeightInfo?.FormattedWeight ?? 0),
                        Length = (float)(model.VolumeInfo?.FormattedLength ?? 0),
                        Width = (float)(model.VolumeInfo?.FormattedWidth ?? 0),
                        Height = (float)(model.VolumeInfo?.FormattedHeight ?? 0),
                        Volume = (float)(model.VolumeInfo?.FormattedVolume ?? 0),
                        TimestampedGuid = model.Timestamp
                    });
                }
            });
            EventAggregator.Instance.Subscribe<BarcodeTypeProviderEvent>(info => {
                if (info is { } args) {
                    Application.Current.Dispatcher.InvokeAsync(() => {
                        //更新右边信息
                        BarCode = args?.Barcode ?? "未识别到条码";
                    });
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async info => {
                try {
                    if (info is { } model) {
                        if (model.SettingsName.Equals("VolumeSettings")) {
                            await Application.Current.Dispatcher.BeginInvoke(async () => {
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
                            await Application.Current.Dispatcher.BeginInvoke(async () => {
                                //临时写在这里加载配置，后续修改通过事件通知
                                var configInfoModel = await _configRepository.FirstOrDefault(s => s.ConfigName.Equals("OcrSettings"));
                                if (configInfoModel is not null) {
                                    var ocrSettingsDto = JsonConvert.DeserializeObject<OcrSettingsDto>(configInfoModel.Value);
                                    if (ocrSettingsDto is not null) {
                                        OcrSettingsInfo = new OcrSettingsInfoModel() {
                                            IsShowSenderInfo = ocrSettingsDto.IsShowSenderInfo,
                                            IsUseOcr = ocrSettingsDto.IsUseOcr,
                                            IsShowReceiverInfo = ocrSettingsDto.IsShowReceiverInfo,
                                            IsShowRecognitionTime = ocrSettingsDto.IsShowRecognitionTime,
                                            IsThreeSegmentCode = ocrSettingsDto.IsThreeSegmentCode,
                                            RecognitionTimeout = ocrSettingsDto.RecognitionTimeout
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
            SubmitApiInfoManager.ApiResponseEvent += async (sender, info) => {
                if (info is {
                    UploadResponse.ExecutionType: ExecutionType.UploadInformation, PackageInfo: not null
                }) {
                    await Task.Delay(100);
                    try {
                        await _updateSlim.WaitAsync();
                        var barCodeItemModel =
                            PackageItems.FirstOrDefault(f => f.TimestampedGuid.Equals(info.PackageInfo.Timestamp));
                        if (barCodeItemModel is not null) {
                            Application.Current.Dispatcher.InvokeAsync(() => {
                                //更新数据
                                barCodeItemModel.RequestStatus = info.UploadResponse?.IsSuccess == true
                                    ? UploadStatus.Succeeded
                                    : UploadStatus.Failed;
                                barCodeItemModel.UploadInfo = new UploadItemModel() {
                                    DurationInSeconds = info.UploadResponse?.Duration ?? 0,
                                    ExceptionMessage = info.UploadResponse?.ExceptionMsg ?? string.Empty,
                                    InterfaceParameters = info.UploadResponse?.ApiParameters ?? string.Empty,
                                    IsSuccess = info.UploadResponse?.IsSuccess ?? false,
                                    RequestContent = info.UploadResponse?.RequestContent ?? string.Empty,
                                    RequestTime = info.UploadResponse?.RequestTime,
                                    RequestUrl = info.UploadResponse?.RequestUrl ?? string.Empty,
                                    ResponseContent = info.UploadResponse?.ResponseContent ?? string.Empty,
                                    ResponseTime = info.UploadResponse?.ResponseTime
                                };
                                if (barCodeItemModel.RequestStatus == UploadStatus.Succeeded) {
                                    UploadedDataCount += 1;
                                }

                                if (barCodeItemModel.RequestStatus == UploadStatus.Failed) {
                                    AbnormalDataCount += 1;
                                }
                            }, DispatcherPriority.Render);
                        }
                    }
                    finally {
                        _updateSlim.Release();
                    }
                }
            };

            //更新云视频上传状态
            EventAggregator.Instance.Subscribe<CloudVideoUploadMessage>(async item => {
                if (item is { } model) {
                    _cloudVideoUploadItems.Enqueue(model);
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is { Type: WindowsActionType.Close }) {
                    _cancellationTokenSource.Cancel();
                }
            });
            //程序启停
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item => {
                if (item is { } info) {
                    EventAggregator.Instance.Publish(new AppLogInfoModel {
                        CreateTime = DateTime.Now,
                        Message = $"程序{(info.Status == ApplicationStatus.Start ? "启动" : "停止")}",
                        Type = LogType.Information
                    });
                }
            });
            //更新格口
            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(item => {
                if (item is { } model) {
                    _packageExitUpdateItems.Enqueue(model);
                }
            });
            if (!_isLoaded) {
                _isLoaded = true;

                new TaskFactory().StartNew(async () => {
                    while (!_cancellationTokenSource.IsCancellationRequested) {
                        await Task.Delay(50).ContinueWith(async a => {
                            var dequeue = _packageExitUpdateItems.TryDequeue(out var exitInfo);
                            if (dequeue && exitInfo is not null) {
                                try {
                                    await _updateSlim.WaitAsync();

                                    var packageItemModel = PackageItems.FirstOrDefault(f => f.TimestampedGuid.Equals(exitInfo.Timestamp));

                                    if (packageItemModel is not null) {
                                        Application.Current.Dispatcher.InvokeAsync(() => {
                                            //更新数据
                                            if (packageItemModel.PackageExitStatus is PackageExitStatus.None or PackageExitStatus.Normal) {
                                                packageItemModel.ExitName = exitInfo.ExitName;
                                                packageItemModel.PackageExitStatus =
                                                    exitInfo.InstructionType switch {
                                                        InstructionType.SignalCallback => PackageExitStatus.Normal,
                                                        InstructionType.PackageException => PackageExitStatus.Abnormal,
                                                        InstructionType.PackageExceptionEx => PackageExitStatus.Abnormal,
                                                        _ => PackageExitStatus.None
                                                    };
                                            }
                                        }, DispatcherPriority.Render);
                                    }
                                    else {
                                        if (DateTime.Now.Subtract(exitInfo.CreateTime).TotalSeconds < 20) {
                                            _packageExitUpdateItems.Enqueue(exitInfo);
                                        }
                                    }
                                }
                                finally {
                                    _updateSlim.Release();
                                }
                            }
                            var b = _cloudVideoUploadItems.TryDequeue(out var cloudVideoUpload);
                            if (b && cloudVideoUpload is not null) {
                                try {
                                    await _updateSlim.WaitAsync();
                                    System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                                        var barCodeItemModel = PackageItems.FirstOrDefault(f => f.Barcode.Equals(cloudVideoUpload.Barcode) &&
                                            f.ScanTime.Equals(cloudVideoUpload.ScanTime));
                                        if (barCodeItemModel is not null) {
                                            barCodeItemModel.IsUploadedToCloudVideo = cloudVideoUpload.IsSuccessful;
                                        }
                                        else {
                                            if (DateTime.Now.Subtract(cloudVideoUpload.ScanTime).TotalSeconds < 10) {
                                                _cloudVideoUploadItems.Enqueue(cloudVideoUpload);
                                            }
                                        }
                                    }, DispatcherPriority.Render);
                                }
                                finally {
                                    _updateSlim.Release();
                                }
                            }
                        });
                    }
                }, TaskCreationOptions.LongRunning);
            }
            //远程指令
            EventAggregator.Instance.Subscribe<RemoteAction>(async item => {
                if (item is { } remoteAction) {
                    switch (remoteAction.Command) {
                        case RemoteCommand.Start:
                        case RemoteCommand.Stop:
                            StartDelegate(remoteAction.Command);
                            break;
                    }
                }
            });

            //加载左边模板
            CurrentViewModel = _container.Resolve<NvrPreviewHomeViewModel>();
        }

        /// <summary>
        /// Ocr识别到内容触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void DeviceServiceOnOcrContentRecognized(object? sender, OcrResult args) {
            await Application.Current.Dispatcher.BeginInvoke(() => {
                //更新右边信息
                BarCode = args?.BarCode ?? "未识别到条码";
                OcrItemInfo.ElapsedTime = args?.ElapsedTime ?? 0;
                OcrItemInfo.RecipientAddress = args?.RecipientAddress ?? string.Empty;
                OcrItemInfo.RecipientName = args?.RecipientName ?? string.Empty;
                OcrItemInfo.RecipientPhone = args?.RecipientPhone ?? string.Empty;
                OcrItemInfo.SenderName = args?.SenderName ?? string.Empty;
                OcrItemInfo.SenderPhone = args?.SenderPhone ?? string.Empty;
                OcrItemInfo.SenderAddress = args?.SenderAddress ?? string.Empty;
                OcrItemInfo.ThreeSegmentCode = args?.ThreeSegmentCode ?? string.Empty;
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

        private async void DeviceServiceOnBarcodeScanned(object? sender, BarcodeReadEventArgs args) {
            await Application.Current.Dispatcher.BeginInvoke(() => {
                BarCode = args?.Barcode ?? "未识别到条码";
            });
        }

        public ICommand UploadStatusCommand => new DelegateCommand<PackageItemModel>(UploadStatusDelegate);

        public ICommand LoadedCommand => new DelegateCommand<Page>(LoadedDelegate);

        private async void LoadedDelegate(Page obj) {
            await Application.Current.Dispatcher.BeginInvoke(async () => {
                //临时写在这里加载配置，后续修改通过事件通知
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = "VolumeSettings"
                });
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = "OcrSettings"
                });

                var configInfoModel = await _configRepository.FirstOrDefault(f =>
                    f.ConfigName.Equals("OtherSettings"));
                //自动启动
                if (configInfoModel is not null) {
                    try {
                        var settingsDto = JsonConvert.DeserializeObject<OtherSettingsDto>(configInfoModel.Value);
                        if (settingsDto is not null && settingsDto.IsAutoStart) {
                            StartDelegate(null);
                        }
                    }
                    catch (Exception e) {
                        EventAggregator.Instance.Publish(new AppLogInfoModel {
                            CreateTime = DateTime.Now,
                            Message = e.Message,
                            Type = LogType.Exception
                        });
                    }
                }
            });
        }

        private void UploadStatusDelegate(PackageItemModel obj) {
            //判断状态是否已上传再获进行弹窗
            if (obj.RequestStatus != UploadStatus.NotUploaded) {
                _dialogService.Show("ApiAccessDialog", new DialogParameters { { "PackageItem", obj } }, null);
            }
        }

        /// <summary>
        /// 开始按钮点击
        /// </summary>
        public ICommand StartCommand => new DelegateCommand<object>(StartDelegate);

        private async void StartDelegate(object obj) {
            await Task.Run(async () => {
                var command = RemoteCommand.None;
                if (obj is RemoteCommand remoteCommand) {
                    command = remoteCommand;
                }
                if (!IsSwitchingState) {
                    try {
                        await _runningSemaphoreSlim.WaitAsync();
                        IsSwitchingState = true;
                        if (!RunningStatus && (obj is null || command == RemoteCommand.Start)) {
                            //效验
                            /*
                            var machineCode = await _computer.GenerateMachineCode();
                            /#1#/判断机器码
                            if (!machineCode.Equals("1E371E8FB7F89C94D93B274DDE14AC46")) {
                                return;
                            }#1#
                            //判断时间
                            var validateTime = await _certificateValidationService.ValidateTime();
                            if (!validateTime) {
                                return;
                            }
                            */
#if !DEBUG

                            var licenseDirectory = Path.Combine(AppContext.BaseDirectory, "License");
                            if (!Directory.Exists(licenseDirectory)) {
                                Directory.CreateDirectory(licenseDirectory);
                            }
                            var firstOrDefault = Directory.GetFiles(licenseDirectory, "*.key").FirstOrDefault();
                            if (firstOrDefault is not null) {
                                //解密授权
                                var (b, s) = LicenseManager.DecryptAuthorizationFile(firstOrDefault, out var data);

                                if (data is not null) {
                                    //重新下载
                                    Task.Run(async () => {
                                        var (key1, o) = await _clientLicenseApi.CreateAuthorization(data.LicenseCode, data.MachineCode, data.Remarks);
                                        if (o is ApiResult result &&
                                            !string.IsNullOrEmpty(result.Data?.ToString() ?? string.Empty)) {
                                            if (key1) {
                                                var licenseDirectory = Path.Combine(AppContext.BaseDirectory, "License");
                                                var files = Directory.GetFiles(licenseDirectory, "*.key");
                                                Parallel.ForEach(files, File.Delete);

                                                await _clientLicenseApi.DownloadFileAsync(result.Data?.ToString() ?? string.Empty,
                                                    $"{licenseDirectory}\\License.key");
                                            }
                                        }
                                    });
                                }
                                if (!b) {
                                    EventAggregator.Instance.Publish(new AppLogInfoModel {
                                        CreateTime = DateTime.Now,
                                        Message = s,
                                        Type = LogType.Exception
                                    });
                                    HomeMessageQueue.Enqueue(s);
                                    return;
                                }
                                else {
                                    //提交激活
                                    if (data is not null) {
                                        Task.Run(async () => {
                                            await _clientLicenseApi.ActivateAuthorization(data.LicenseCode, data.MachineCode, data.Remarks);
                                        });
                                    }
                                }
                            }
                            else {
                                EventAggregator.Instance.Publish(new AppLogInfoModel {
                                    CreateTime = DateTime.Now,
                                    Message = "未检测到授权文件",
                                    Type = LogType.Exception
                                });
                                HomeMessageQueue.Enqueue("未检测到授权文件");
                                return;
                            }
#endif

                            //启动
                            await _externalDataService.Start();
                            var (key, value) = await _deviceService.Start();
                            await _sortingService.Start();
                            //提示
                            //ApplicationStatusChanged
                            EventAggregator.Instance.Publish(new ApplicationStatusChanged {
                                Status = ApplicationStatus.Start
                            });
                            AppContext.SetData("IsRunning", true);
                        }
                        else {
                            //停止
                            HomeMessageQueue.Clear();
                            await _externalDataService.Stop();
                            var (key, value) = await _deviceService.Stop();
                            await _sortingService.Stop();
                            //提示
                            EventAggregator.Instance.Publish(new ApplicationStatusChanged {
                                Status = ApplicationStatus.Stop
                            });
                            AppContext.SetData("IsRunning", false);
                        }

                        await Application.Current.Dispatcher.BeginInvoke(() => {
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
        private async void AddNewRow(PackageItemModel item) {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                item.Num = TotalDataCount += 1;

                PackageItems.Insert(0, item);
                if (PackageItems.Count > 50) {
                    PackageItems.RemoveAt(PackageItems.Count - 1);
                }
                //item.IsInserting = true;
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 清空计数
        /// </summary>
        public ICommand ClearCountCommand => new DelegateCommand<object>(ClearCountDelegate);

        private async void ClearCountDelegate(object obj) {
            await Application.Current.Dispatcher.BeginInvoke(async () => {
                if (_deviceService.RunningStatus) {
                    HomeMessageQueue.Enqueue("请先停止运行再清空");
                    return;
                }
                PackageItems.Clear();
                TotalDataCount =
                    UploadedDataCount =
                        AbnormalDataCount = 0;
                _packageExitUpdateItems.Clear();
                _cloudVideoUploadItems.Clear();
            }, DispatcherPriority.Background);
        }
    }
}