using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using JayTom.Dws.Ocr;
using Prism.Commands;
using System.Windows;
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
using JayTom.Dws.Interface.License;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.Models.DataModels;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Client.Models.OcrSettingsModel;
using LogType = JayTom.Dws.Data.LocalLog.LogType;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using InstructionType = JayTom.Dws.Data.Package.InstructionType;
using RemoteAction = JayTom.Dws.Domain.EventMediators.RemoteAction;
using RemoteCommand = JayTom.Dws.Domain.EventMediators.RemoteCommand;
using WindowsAction = JayTom.Dws.Domain.EventMediators.WindowsAction;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.SubHomeViewModels;
using ApplicationStatus = JayTom.Dws.Domain.EventMediators.ApplicationStatus;
using WindowsActionType = JayTom.Dws.Domain.EventMediators.WindowsActionType;
using ExceptionEventArgs = JayTom.Dws.Client.Service.Sorting.ExceptionEventArgs;
using SettingsChangedEvent = JayTom.Dws.Domain.EventMediators.SettingsChangedEvent;
using static JayTom.Dws.Client.Service.BackgroundService.SubmitApiBackgroundService;
using PackageExitUpdateEvent = JayTom.Dws.Domain.EventMediators.PackageExitUpdateEvent;
using ApplicationStatusChanged = JayTom.Dws.Domain.EventMediators.ApplicationStatusChanged;
using BarcodeTypeProviderEvent = JayTom.Dws.Domain.EventMediators.BarcodeTypeProviderEvent;

namespace JayTom.Dws.Client.ViewModels.Pages
{

    public class HomeViewModel : BindableBase
    {
        private readonly IDialogService _dialogService;
        private readonly IDeviceService _deviceService;
        private readonly IImageStorageService _imageStorageService;
        private readonly IResultOutputService _resultOutputService;
        private readonly IExternalDataService _externalDataService;
        private readonly ISettingsReader _settingsReader;
        private readonly ISortingService _sortingService;
        private readonly IClientLicenseApi _clientLicenseApi;

        private ObservableCollection<PackageItemModel> _packageItems = new();

        private int _totalDataCount;
        private int _uploadedDataCount;
        private int _abnormalDataCount;
        private bool _runningStatus;
        private SnackbarMessageQueue _homeMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _barCode = Languages.Language.ResourceManager.GetString("BarCode") ?? string.Empty;
        private decimal _weight;
        private decimal _volume;
        private decimal _length;
        private decimal _width;
        private decimal _height;
        private bool _isSwitchingState;
        private VolumeUnit _volumeUnit;
        private static SemaphoreSlim _runningSemaphoreSlim = new(1, 1);

        private OcrSettingsInfoModel _ocrSettingsInfo = new();

        private OcrInfoItemModel _ocrItemInfo = new();
        /// <summary>
        /// UI 更新任务取消源。
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        /// <summary>
        /// 待显示的包裹行。
        /// </summary>
        private readonly ConcurrentQueue<PackageItemModel> _pendingPackageItems = new();
        /// <summary>
        /// 待处理的接口响应。
        /// </summary>
        private readonly ConcurrentQueue<ApiResponseReceived> _updateResponseItems = new();

        //private ConcurrentQueue<SortingExitReceived> _sortingExitItems = new();
        /// <summary>
        /// 待处理的格口更新。
        /// </summary>
        private readonly ConcurrentQueue<PackageExitUpdateEvent> _packageExitUpdateItems = new();

        /// <summary>
        /// 待处理的云视频上传状态。
        /// </summary>
        private readonly ConcurrentQueue<CloudVideoUploadMessage> _cloudVideoUploadItems = new();
        /// <summary>
        /// 合并 UI 更新的后台任务。
        /// </summary>
        private readonly Task _uiUpdateWorker;
        private BindableBase? _currentViewModel;

        public SnackbarMessageQueue HomeMessageQueue
        {
            get => _homeMessageQueue;
            set => SetProperty(ref _homeMessageQueue, value);
        }

        public ObservableCollection<PackageItemModel> PackageItems
        {
            get => _packageItems;
            set => SetProperty(ref _packageItems, value);
        }

        /// <summary>
        /// Ocr设置
        /// </summary>
        public OcrSettingsInfoModel OcrSettingsInfo
        {
            get => _ocrSettingsInfo;
            set => SetProperty(ref _ocrSettingsInfo, value);
        }

        /// <summary>
        /// Ocr显示信息
        /// </summary>
        public OcrInfoItemModel OcrItemInfo
        {
            get => _ocrItemInfo;
            set => SetProperty(ref _ocrItemInfo, value);
        }

        /// <summary>
        /// 体积单位
        /// </summary>
        public VolumeUnit VolumeUnit
        {
            get => _volumeUnit;
            set => SetProperty(ref _volumeUnit, value);
        }

        /// <summary>
        /// 总数
        /// </summary>
        public int TotalDataCount
        {
            get => _totalDataCount;
            set => SetProperty(ref _totalDataCount, value);
        }

        /// <summary>
        /// 上传数量
        /// </summary>
        public int UploadedDataCount
        {
            get => _uploadedDataCount;
            set => SetProperty(ref _uploadedDataCount, value);
        }

        /// <summary>
        /// 异常数量
        /// </summary>
        public int AbnormalDataCount
        {
            get => _abnormalDataCount;
            set => SetProperty(ref _abnormalDataCount, value);
        }

        /// <summary>
        /// 设备运行状态
        /// </summary>
        public bool RunningStatus
        {
            get => _runningStatus;
            set => SetProperty(ref _runningStatus, value);
        }

        /// <summary>
        /// 开关按钮切换状态
        /// </summary>
        public bool IsSwitchingState
        {
            get => _isSwitchingState;
            set => SetProperty(ref _isSwitchingState, value);
        }

        /// <summary>
        /// 左边的实时视图
        /// </summary>
        public BindableBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        #region 条码信息

        /// <summary>
        /// 条码
        /// </summary>
        public string BarCode
        {
            get => _barCode;
            set => SetProperty(ref _barCode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        public decimal Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 体积
        /// </summary>
        public decimal Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        /// <summary>
        /// 长度
        /// </summary>
        public decimal Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        /// <summary>
        /// 宽度
        /// </summary>
        public decimal Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 高度
        /// </summary>
        public decimal Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        #endregion 条码信息

        public HomeViewModel(IDialogService dialogService,
            IDeviceService deviceService,
            IImageStorageService imageStorageService,
            IResultOutputService resultOutputService,
            IExternalDataService externalDataService,
            ISettingsReader settingsReader,
            ISortingService sortingService,
            IClientLicenseApi clientLicenseApi,
            CameraHomeViewModel cameraHomeViewModel)
        {
            _dialogService = dialogService;
            _deviceService = deviceService;
            _imageStorageService = imageStorageService;
            _resultOutputService = resultOutputService;
            _externalDataService = externalDataService;
            _settingsReader = settingsReader;
            _sortingService = sortingService;
            _clientLicenseApi = clientLicenseApi;
            _deviceService.BarcodeScanned += DeviceServiceOnBarcodeScanned;
            _deviceService.BarcodeMissed += async delegate (object? sender, BarcodeReadEventArgs args)
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    BarCode = args?.Barcode ?? "未识别到条码";
                }, DispatcherPriority.Background);
            };
            _deviceService.CameraDisconnected += delegate (object? sender, IReadOnlyList<ICamera> list)
            {
                //更新现有列表,例如删除相机成员
            };
            _deviceService.VolumeCaptured += DeviceServiceOnVolumeCaptured;
            _deviceService.DeviceException += async delegate (object? sender, DeviceExceptionEventArgs args)
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    HomeMessageQueue.Enqueue(args?.Exception?.Message ?? string.Empty);
                }, DispatcherPriority.Background);

                //弹出提示框
            };
            _deviceService.StableWeight += async delegate (object? sender, StableWeightEventArgs args)
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    Weight = args.Weight;
                }, DispatcherPriority.Background);
            };
            _deviceService.OcrContentRecognized += DeviceServiceOnOcrContentRecognized;
            _deviceService.BarCodeKeyReceived += async (sender, s) =>
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    BarCode = s.Barcode;
                }, DispatcherPriority.Background);
            };

            _imageStorageService.ImageSaveFailed += async delegate (object? sender, Exception exception)
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("图片保存异常") ?? string.Empty}:{exception.Message}");
                }, DispatcherPriority.Background);
            };
            _resultOutputService.OutputFailed += async delegate (object? sender, Exception exception)
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("结果输出异常") ?? string.Empty}:{exception.Message}");
                }, DispatcherPriority.Background);
            };
            //外部数据
            _externalDataService.ExternalDataException += async delegate (object? sender, Exception exception)
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    HomeMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("外部输入异常") ?? string.Empty}:{exception.Message}");
                }, DispatcherPriority.Background);
            };
            //外部全量数据
            _externalDataService.ContentInputReceived += async (sender, args) =>
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    BarCode = args?.Barcode ?? "未解析到条码";
                    Weight = args?.Weight ?? 0;
                    Length = args?.Length ?? 0;
                    Width = args?.Width ?? 0;
                    Height = args?.Height ?? 0;
                    Volume = args?.Volume ?? 0;
                }, DispatcherPriority.Background);
            };
            _externalDataService.VolumeReceived += async delegate (object? sender, ExternalVolumeInputEventArgs args)
            {
                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    Length = (decimal)args.Length;
                    Width = (decimal)args.Width;
                    Height = (decimal)args.Height;
                    Volume = (decimal)args.Volume;
                }, DispatcherPriority.Background);
            };
            //分拣
            _sortingService.HeartbeatError += delegate (object? sender, Exception exception)
            {
                HomeMessageQueue.Enqueue($"{exception.Message}");
            };
            _sortingService.ExceptionOccurred += delegate (object? sender, ExceptionEventArgs args)
            {
                HomeMessageQueue.Enqueue($"{args.ExceptionMessage}");
            };
            EventAggregator.Instance.Subscribe<PackageInfo>(info =>
            {
                if (info is { } model)
                {
                    _pendingPackageItems.Enqueue(new PackageItemModel()
                    {
                        Barcode = model.BarCodeInfo?.Barcode ?? string.Empty,
                        ScanTime = model.BarCodeInfo?.ScanTime ?? DateTime.Now,
                        Weight = (decimal)(model.WeightInfo?.FormattedWeight ?? 0),
                        Length = (decimal)(model.VolumeInfo?.FormattedLength ?? 0),
                        Width = (decimal)(model.VolumeInfo?.FormattedWidth ?? 0),
                        Height = (decimal)(model.VolumeInfo?.FormattedHeight ?? 0),
                        Volume = (decimal)(model.VolumeInfo?.FormattedVolume ?? 0),
                        TimestampMilliseconds = model.Timestamp
                    });
                }
            });
            EventAggregator.Instance.Subscribe<BarcodeTypeProviderEvent>(info =>
            {
                if (info is { } args)
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        //更新右边信息
                        BarCode = args?.Barcode ?? "未识别到条码";
                    });
                }
            });
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async info =>
            {
                try
                {
                    if (info is { } model)
                    {
                        if (model.SettingsName.Equals("VolumeSettings"))
                        {
                            var volumeSettingsDto = await _settingsReader
                                .GetAsync<VolumeSettingsDto>("VolumeSettings")
                                .ConfigureAwait(false);
                            if (volumeSettingsDto is not null)
                            {
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    VolumeUnit = volumeSettingsDto.Unit;
                                }, DispatcherPriority.Background);
                            }
                        }
                        else if (model.SettingsName.Equals("OcrSettings"))
                        {
                            var ocrSettingsDto = await _settingsReader
                                .GetAsync<OcrSettingsDto>("OcrSettings")
                                .ConfigureAwait(false);
                            if (ocrSettingsDto is not null)
                            {
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    OcrSettingsInfo = new OcrSettingsInfoModel()
                                    {
                                        IsShowSenderInfo = ocrSettingsDto.IsShowSenderInfo,
                                        IsUseOcr = ocrSettingsDto.IsUseOcr,
                                        IsShowReceiverInfo = ocrSettingsDto.IsShowReceiverInfo,
                                        IsShowRecognitionTime = ocrSettingsDto.IsShowRecognitionTime,
                                        IsThreeSegmentCode = ocrSettingsDto.IsThreeSegmentCode,
                                        RecognitionTimeout = ocrSettingsDto.RecognitionTimeout
                                    };
                                }, DispatcherPriority.Background);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            //更新上传状态
            EventAggregator.Instance.Subscribe<ApiResponseReceived>(item =>
            {
                if (item is { } model)
                {
                    _updateResponseItems.Enqueue(model);
                }
            });
            //更新云视频上传状态
            EventAggregator.Instance.Subscribe<CloudVideoUploadMessage>(item =>
            {
                if (item is { } model)
                {
                    _cloudVideoUploadItems.Enqueue(model);
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    _cancellationTokenSource.Cancel();
                }
            });
            //程序启停
            EventAggregator.Instance.Subscribe<ApplicationStatusChanged>(item =>
            {
                if (item is { } info)
                {
                    EventAggregator.Instance.Publish(new AppLogInfoModel
                    {
                        CreateTime = DateTime.Now,
                        Message = $"程序{(info.Status == ApplicationStatus.Start ? "启动" : "停止")}",
                        Type = LogType.Information
                    });
                }
            });
            //更新格口
            EventAggregator.Instance.Subscribe<PackageExitUpdateEvent>(item =>
            {
                if (item is { } model)
                {
                    _packageExitUpdateItems.Enqueue(model);
                }
            });
            _uiUpdateWorker = Task.Run(ProcessUiUpdates);
            //远程指令
            EventAggregator.Instance.Subscribe<RemoteAction>(async item =>
            {
                if (item is { } remoteAction)
                {
                    switch (remoteAction.Command)
                    {
                        case RemoteCommand.Start:
                        case RemoteCommand.Stop:
                            await StartAsync(remoteAction.Command);
                            break;
                    }
                }
            });

            //加载左边模板
            CurrentViewModel = cameraHomeViewModel;
        }

        /// <summary>
        /// Ocr识别到内容触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void DeviceServiceOnOcrContentRecognized(object? sender, OcrResult args)
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
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
            }, DispatcherPriority.Background);
        }

        private async void DeviceServiceOnVolumeCaptured(object? sender, VolumeCapturedEventArgs args)
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                Length = (decimal)args.Length;
                Width = (decimal)args.Width;
                Height = (decimal)args.Height;
                Volume = (decimal)args.Volume;
            }, DispatcherPriority.Background);
        }

        private async void DeviceServiceOnBarcodeScanned(object? sender, BarcodeReadEventArgs args)
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                BarCode = args?.Barcode ?? "未识别到条码";
            }, DispatcherPriority.Background);
        }

        public ICommand UploadStatusCommand => new DelegateCommand<PackageItemModel>(UploadStatusDelegate);

        public ICommand LoadedCommand => new DelegateCommand<Page>(LoadedDelegate);

        private async void LoadedDelegate(Page obj)
        {
            EventAggregator.Instance.Publish(new SettingsChangedEvent
            {
                SettingsName = "VolumeSettings"
            });
            EventAggregator.Instance.Publish(new SettingsChangedEvent
            {
                SettingsName = "OcrSettings"
            });

            var settingsDto = await _settingsReader
                .GetAsync<OtherSettingsDto>("OtherSettings")
                .ConfigureAwait(false);
            if (settingsDto is not null)
            {
                try
                {
                    if (settingsDto.IsAutoStart)
                    {
                        await StartAsync(null);
                    }
                }
                catch (Exception e)
                {
                    EventAggregator.Instance.Publish(new AppLogInfoModel
                    {
                        CreateTime = DateTime.Now,
                        Message = e.Message,
                        Type = LogType.Exception
                    });
                }
            }
        }

        private void UploadStatusDelegate(PackageItemModel obj)
        {
            //判断状态是否已上传再获进行弹窗
            if (obj.RequestStatus != UploadStatus.NotUploaded)
            {
                _dialogService.Show("ApiAccessDialog", new DialogParameters { { "PackageItem", obj } }, null);
            }
        }

        /// <summary>
        /// 开始按钮点击
        /// </summary>
        public ICommand StartCommand => new DelegateCommand<object>(StartDelegate);

        private async void StartDelegate(object obj)
        {
            try
            {
                await StartAsync(obj);
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
            {
                // 页面正在释放。
            }
            catch (Exception exception)
            {
                LogStartStopFailure(exception);
            }
        }

        /// <summary>
        /// 串行执行设备和分拣服务的启动或停止流程。
        /// </summary>
        private async Task StartAsync(object? obj)
        {
            var command = obj is RemoteCommand remoteCommand ? remoteCommand : RemoteCommand.None;
            await _runningSemaphoreSlim.WaitAsync(_cancellationTokenSource.Token);
            try
            {
                IsSwitchingState = true;
                if (!RunningStatus && (obj is null || command == RemoteCommand.Start))
                {
                    //效验
                    /*
                    var machineCode = await _computer.GenerateMachineCode();
                    /#1#/判断机器码
                    if (!machineCode.Equals("1E371E8FB7F89C94D93B274DDE14AC46")) {
                        return;
                    }#1#
                    //判断时间
                    var validateTime = await _certificateValidationService.ValidateTimeAsync();
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

                        if (!b) {
                            EventAggregator.Instance.Publish(new AppLogInfoModel {
                                CreateTime = DateTime.Now,
                                Message = s,
                                Type = LogType.Exception
                            });
                            HomeMessageQueue.Enqueue(s);
                            return;
                        }
                        if (data is not null) {
                            await RefreshAndActivateLicenseAsync(data, licenseDirectory,
                                _cancellationTokenSource.Token);
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

                    var (externalDataStarted, externalDataMessage) = await _externalDataService.Start();
                    if (!externalDataStarted)
                    {
                        EventAggregator.Instance.Publish(new AppLogInfoModel
                        {
                            CreateTime = DateTime.Now,
                            Message = externalDataMessage,
                            Type = LogType.Exception
                        });
                        HomeMessageQueue.Enqueue(externalDataMessage);
                        return;
                    }

                    var (deviceStarted, deviceMessage) = await _deviceService.Start();
                    if (!deviceStarted)
                    {
                        await _externalDataService.Stop();
                        EventAggregator.Instance.Publish(new AppLogInfoModel
                        {
                            CreateTime = DateTime.Now,
                            Message = deviceMessage,
                            Type = LogType.Exception
                        });
                        HomeMessageQueue.Enqueue(deviceMessage);
                        return;
                    }

                    var (sortingStarted, sortingMessage) = await _sortingService.Start();
                    if (!sortingStarted)
                    {
                        await _deviceService.Stop();
                        await _externalDataService.Stop();
                        EventAggregator.Instance.Publish(new AppLogInfoModel
                        {
                            CreateTime = DateTime.Now,
                            Message = sortingMessage,
                            Type = LogType.Exception
                        });
                        HomeMessageQueue.Enqueue(sortingMessage);
                        return;
                    }

                    EventAggregator.Instance.Publish(new ApplicationStatusChanged
                    {
                        Status = ApplicationStatus.Start
                    });
                    AppContext.SetData("IsRunning", true);
                }
                else
                {
                    HomeMessageQueue.Clear();
                    await _externalDataService.Stop();
                    await _deviceService.Stop();
                    await _sortingService.Stop();
                    EventAggregator.Instance.Publish(new ApplicationStatusChanged
                    {
                        Status = ApplicationStatus.Stop
                    });
                    AppContext.SetData("IsRunning", false);
                }

                await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    RunningStatus = _deviceService.RunningStatus;
                }, DispatcherPriority.Background);
            }
            finally
            {
                IsSwitchingState = false;
                _runningSemaphoreSlim.Release();
            }
        }

        /// <summary>
        /// 安全刷新授权文件，并在刷新完成后提交激活状态。
        /// </summary>
        private async Task RefreshAndActivateLicenseAsync(LicenseData data, string licenseDirectory,
            CancellationToken token)
        {
            try
            {
                var (created, response) = await _clientLicenseApi.CreateAuthorization(
                    data.LicenseCode, data.MachineCode, data.Remarks, token);
                if (created && response is { } result &&
                    !string.IsNullOrWhiteSpace(result.Data))
                {
                    var targetPath = Path.Combine(licenseDirectory, "License.key");
                    var downloaded = await _clientLicenseApi.DownloadFileAsync(
                        result.Data, targetPath, token);
                    if (downloaded.IsSuccess)
                    {
                        foreach (var file in Directory.GetFiles(licenseDirectory, "*.key")
                                     .Where(file => !Path.GetFullPath(file).Equals(Path.GetFullPath(targetPath),
                                         StringComparison.OrdinalIgnoreCase)))
                        {
                            File.Delete(file);
                        }
                    }
                }

                await _clientLicenseApi.ActivateAuthorization(
                    data.LicenseCode, data.MachineCode, data.Remarks, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogStartStopFailure(exception);
            }
        }

        /// <summary>
        /// 记录启动或停止流程中未处理的异常。
        /// </summary>
        private static void LogStartStopFailure(Exception exception)
        {
            EventAggregator.Instance.Publish(new AppLogInfoModel
            {
                CreateTime = DateTime.Now,
                Message = exception.Message,
                Type = LogType.Exception
            });
        }

        /// <summary>
        /// 批量处理主页 UI 更新，避免高频事件持续抢占输入线程。
        /// </summary>
        private async Task ProcessUiUpdates()
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
                while (await timer.WaitForNextTickAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    if (_pendingPackageItems.IsEmpty &&
                        _updateResponseItems.IsEmpty &&
                        _packageExitUpdateItems.IsEmpty &&
                        _cloudVideoUploadItems.IsEmpty)
                    {
                        continue;
                    }
                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    if (dispatcher is null)
                    {
                        break;
                    }
                    await dispatcher.InvokeAsync(() =>
                    {
                        var deferredResponses = new List<ApiResponseReceived>();
                        var deferredExits = new List<PackageExitUpdateEvent>();
                        var deferredUploads = new List<CloudVideoUploadMessage>();
                        for (var index = 0;
                             index < 16 && _pendingPackageItems.TryDequeue(out var packageItem);
                             index++)
                        {
                            TotalDataCount++;
                            packageItem.Num = TotalDataCount;
                            PackageItems.Insert(0, packageItem);
                            if (PackageItems.Count > 50)
                            {
                                PackageItems.RemoveAt(PackageItems.Count - 1);
                            }
                        }

                        for (var index = 0;
                             index < 32 && _updateResponseItems.TryDequeue(out var updateResponse);
                             index++)
                        {
                            var packageItem = PackageItems.FirstOrDefault(item =>
                                item.Barcode.Equals(updateResponse.Barcode) &&
                                item.ScanTime.Equals(updateResponse.ScanTime));
                            if (packageItem is null)
                            {
                                if (DateTime.Now - updateResponse.ScanTime < TimeSpan.FromSeconds(10))
                                {
                                    deferredResponses.Add(updateResponse);
                                }
                                continue;
                            }
                            var nextStatus = updateResponse.UploadResponse?.IsSuccess == true
                                ? UploadStatus.Succeeded
                                : UploadStatus.Failed;
                            if (packageItem.RequestStatus != nextStatus)
                            {
                                if (packageItem.RequestStatus == UploadStatus.Succeeded)
                                {
                                    UploadedDataCount = Math.Max(0, UploadedDataCount - 1);
                                }
                                else if (packageItem.RequestStatus == UploadStatus.Failed)
                                {
                                    AbnormalDataCount = Math.Max(0, AbnormalDataCount - 1);
                                }
                                packageItem.RequestStatus = nextStatus;
                                if (nextStatus == UploadStatus.Succeeded)
                                {
                                    UploadedDataCount++;
                                }
                                else
                                {
                                    AbnormalDataCount++;
                                }
                            }
                            packageItem.UploadInfo = new UploadItemModel
                            {
                                DurationInSeconds = updateResponse.UploadResponse?.DurationSeconds ?? 0,
                                ExceptionMessage = updateResponse.UploadResponse?.ExceptionMsg ?? string.Empty,
                                InterfaceParameters = updateResponse.UploadResponse?.ApiParameters ?? string.Empty,
                                IsSuccess = updateResponse.UploadResponse?.IsSuccess ?? false,
                                RequestContent = updateResponse.UploadResponse?.RequestContent ?? string.Empty,
                                RequestTime = updateResponse.UploadResponse?.RequestTime,
                                RequestUrl = updateResponse.UploadResponse?.RequestUrl ?? string.Empty,
                                ResponseContent = updateResponse.UploadResponse?.ResponseContent ?? string.Empty,
                                ResponseTime = updateResponse.UploadResponse?.ResponseTime
                            };
                        }

                        for (var index = 0;
                             index < 32 && _packageExitUpdateItems.TryDequeue(out var exitInfo);
                             index++)
                        {
                            var packageItem =
                                PackageItems.FirstOrDefault(item => item.TimestampMilliseconds.Equals(exitInfo.Timestamp));
                            if (packageItem is null)
                            {
                                if (DateTime.Now - exitInfo.CreateTime < TimeSpan.FromSeconds(20))
                                {
                                    deferredExits.Add(exitInfo);
                                }
                                continue;
                            }
                            if (packageItem.PackageExitStatus is PackageExitStatus.None or PackageExitStatus.Normal)
                            {
                                packageItem.ExitName = exitInfo.ExitName;
                                packageItem.PackageExitStatus = exitInfo.InstructionType switch
                                {
                                    InstructionType.SignalCallback => PackageExitStatus.Normal,
                                    InstructionType.PackageException => PackageExitStatus.Abnormal,
                                    InstructionType.PackageExceptionEx => PackageExitStatus.Abnormal,
                                    _ => PackageExitStatus.None
                                };
                            }
                        }

                        for (var index = 0;
                             index < 32 && _cloudVideoUploadItems.TryDequeue(out var uploadInfo);
                             index++)
                        {
                            var packageItem = PackageItems.FirstOrDefault(item =>
                                item.Barcode.Equals(uploadInfo.Barcode) &&
                                item.ScanTime.Equals(uploadInfo.ScanTime));
                            if (packageItem is null)
                            {
                                if (DateTime.Now - uploadInfo.ScanTime < TimeSpan.FromSeconds(10))
                                {
                                    deferredUploads.Add(uploadInfo);
                                }
                                continue;
                            }
                            packageItem.IsUploadedToCloudVideo = uploadInfo.IsSuccessful;
                        }
                        foreach (var response in deferredResponses)
                        {
                            _updateResponseItems.Enqueue(response);
                        }
                        foreach (var exit in deferredExits)
                        {
                            _packageExitUpdateItems.Enqueue(exit);
                        }
                        foreach (var upload in deferredUploads)
                        {
                            _cloudVideoUploadItems.Enqueue(upload);
                        }
                    }, DispatcherPriority.Background);
                }
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
            {
                //应用关闭时正常退出。
            }
            catch (Exception exception)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, "主页 UI 更新任务异常");
            }
        }

        /// <summary>
        /// 清空计数
        /// </summary>
        public ICommand ClearCountCommand => new DelegateCommand<object>(ClearCountDelegate);

        private async void ClearCountDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (_deviceService.RunningStatus)
                {
                    HomeMessageQueue.Enqueue("请先停止运行再清空");
                    return;
                }
                PackageItems.Clear();
                TotalDataCount =
                    UploadedDataCount =
                        AbnormalDataCount = 0;
                _pendingPackageItems.Clear();
                _updateResponseItems.Clear();
                _packageExitUpdateItems.Clear();
                _cloudVideoUploadItems.Clear();
            }, DispatcherPriority.Background);
        }
    }
}
