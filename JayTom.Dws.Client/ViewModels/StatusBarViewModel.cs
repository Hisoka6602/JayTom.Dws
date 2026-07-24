using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Threading;
using JayTom.Dws.Camera;
using System.Windows.Input;
using System.Windows.Media;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using System.Windows.Threading;
using JayTom.Dws.Client.Service;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.Timer;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Client.Service.Device;
using Microsoft.AspNetCore.Connections;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Models.StatusBarModels;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels {

    public class StatusBarViewModel : BindableBase {
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IDeviceService _deviceService;
        private readonly IFtp _ftp;
        private readonly IDynamicScale _dynamicScale;
        private readonly IStaticScale _staticScale;
        private readonly ITcpVolumeInput _tcpVolumeInput;
        private readonly ITcpContentInput _tcpContentInput;
        private readonly ITcpContentOutput _tcpContentOutput;
        private readonly IExitMonitor _exitMonitor;
        private readonly IStackedPackageService _stackedPackageService;
        private readonly ISortingConnectionService _sortingConnectionService;
        private readonly ISoundRepository _soundRepository;
        private readonly IGrayscaleService _grayscaleService;
        private static readonly SemaphoreSlim UpdateSlim = new(1, 1);

        private ObservableCollection<string> _exceptionItems = new()
        {
            "默认异常信息1","默认异常信息2","默认异常信息3这是很长的信息，会自动换行",
        };

        private ObservableCollection<CameraItemInfoModel> _cameraItems = new();

        private ObservableCollection<SerialPortInfoModel> _serialPortItems = new()
        {
            new SerialPortInfoModel()
            {
                Name = "COM1",
                Status = SerialPortStatus.Running,
                Type = SerialPortType.Camera
            },
            new SerialPortInfoModel()
            {
                Name = "COM2",
                Status = SerialPortStatus.NotConnected,
                Type = SerialPortType.Controller
            },
            new SerialPortInfoModel()
            {
                Name = "COM3",
                Status = SerialPortStatus.Running,
                Type = SerialPortType.Scale
            },
            new SerialPortInfoModel()
            {
                Name = "COM4",
                Status = SerialPortStatus.Running,
                Type = SerialPortType.Other
            },
        };

        private ComputerInfoModel _computerInfo = new() {
            MemoryInfo = new MemoryInfoModel() {
                UsedPercentage = 80,
                MemoryRemaining = 20,
            },
            CpuInfo = new CpuInfoModel() {
                UsagePercentage = 90,
                CpuTemperature = 76,
                Name = "Intel(R) Core(TM) i7-1065G7 CPU @ 1.30GHz"
            },
            GpuInfo = new GpuInfoModel() {
                Name = "Intel(R) Iris(R) Plus Graphics",
                UsagePercentage = 11,
            },
            HardDiskList = new List<HardDiskInfoModel>()
            {
                new()
                {
                    DiskName = "C:",
                    UsedSpacePercentage = 90
                },
                new()
                {
                    DiskName = "D:",
                    UsedSpacePercentage = 10
                },
            }
        };

        private string _formattedElapsed = string.Empty;

        private ObservableCollection<ConnectionItemInfoModel> _connectionItems = new();
        private SolidColorBrush _connectionSolidColorBrush = Brushes.DarkGray;
        private SolidColorBrush _cameraSolidColorBrush = Brushes.DarkGray;

        public StatusBarViewModel(IComputerInfoReporter computerInfoReporter,
            IDeviceService deviceService, IConfigRepository configRepository,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository,
            IFtp ftp, IDynamicScale dynamicScale,
            IStaticScale staticScale, ITcpVolumeInput tcpVolumeInput,
            ITcpContentInput tcpContentInput, ITcpContentOutput tcpContentOutput,
            IExitMonitor exitMonitor,
            IStackedPackageService stackedPackageService,
            ISortingConnectionService sortingConnectionService,
            ISoundRepository soundRepository,
            IGrayscaleService grayscaleService) {
            _computerInfoReporter = computerInfoReporter;
            _deviceService = deviceService;
            _ftp = ftp;
            _dynamicScale = dynamicScale;
            _staticScale = staticScale;
            _tcpVolumeInput = tcpVolumeInput;
            _tcpContentInput = tcpContentInput;
            _tcpContentOutput = tcpContentOutput;
            _exitMonitor = exitMonitor;
            _stackedPackageService = stackedPackageService;
            _sortingConnectionService = sortingConnectionService;
            _soundRepository = soundRepository;
            _grayscaleService = grayscaleService;
            _computerInfoReporter.ComputerInfoReceived += async delegate (object? sender, ComputerInfoModel model) {
                try {
                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    if (dispatcher is not null) {
                        await dispatcher.InvokeAsync(() => {
                            //加载到界面内容
                            ComputerInfo = model;
                            if (ConnectionItems.Any(a => a.ConnectionState is ConnectionState.ConnectionFailed)) {
                                ConnectionSolidColorBrush = Brushes.Red;
                            }
                            else if (ConnectionItems.Any(a => a.ConnectionState == ConnectionState.Disconnected)) {
                                ConnectionSolidColorBrush = Brushes.DarkGray;
                            }
                            else if (ConnectionItems.Any() &&
                                     ConnectionItems.All(a => a.ConnectionState == ConnectionState.Connected)) {
                                ConnectionSolidColorBrush = Brushes.LimeGreen;
                            }
                            else {
                                ConnectionSolidColorBrush = Brushes.DarkGray;
                            }

                            if (CameraItems.Any(a => a.Status == CameraStatus.Failure)) {
                                CameraSolidColorBrush = Brushes.Red;
                            }
                            else if (CameraItems.Any(a => a.Status == CameraStatus.Disconnected)) {
                                CameraSolidColorBrush = Brushes.DarkGray;
                            }
                            else if (CameraItems.Any() &&
                                     CameraItems.All(a => a.Status == CameraStatus.Running)) {
                                CameraSolidColorBrush = Brushes.LimeGreen;
                            }
                            else {
                                CameraSolidColorBrush = Brushes.DarkGray;
                            }
                        }, DispatcherPriority.Background);
                    }
                }
                catch (TaskCanceledException) {
                    // 应用退出时调度任务可能被取消。
                }
                catch (Exception exception) {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(exception, "更新状态栏电脑信息失败");
                }
            };
            EventAggregator.Instance.Subscribe<TimerDto>(async item => {
                if (item is TimerDto model) {
                    try {
                        if (System.Windows.Application.Current?.Dispatcher is not null) {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                //加载到界面内容
                                FormattedElapsed = model.FormattedElapsed;
                            }, DispatcherPriority.Background);
                        }
                    }
                    catch (TaskCanceledException) {
                        //
                    }
                    catch (Exception e) {
                    }
                }
            });
            EventAggregator.Instance.Subscribe<CameraItemInfoModel>(async item => {
                if (item is CameraItemInfoModel model) {
                    try {
                        await UpdateSlim.WaitAsync();
                        if (System.Windows.Application.Current?.Dispatcher is not null) {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                var cameraItemInfoModel = CameraItems?.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                                if (cameraItemInfoModel is not null) {
                                    cameraItemInfoModel.Status = model.Status;
                                }
                                else {
                                    CameraItems?.Add(model);
                                }
                            }, DispatcherPriority.Background);
                        }
                    }
                    finally {
                        UpdateSlim.Release();
                    }
                }
            });
            //解绑
            _deviceService.CameraUnbound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                try {
                    await UpdateSlim.WaitAsync();
                    if (System.Windows.Application.Current?.Dispatcher is not null) {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            var cameraItemInfoModel = CameraItems?.FirstOrDefault(f => f.SerialNumber.Equals(model.SerialNumber));
                            if (cameraItemInfoModel is not null) {
                                CameraItems?.Remove(cameraItemInfoModel);
                                cameraItemInfoModel.Dispose();
                            }
                        }, DispatcherPriority.Background);
                    }
                }
                finally {
                    UpdateSlim.Release();
                }
            };
            //断开
            _deviceService.CameraDisconnected += async delegate (object? sender, List<ICamera> list) {
                try {
                    await UpdateSlim.WaitAsync();
                    if (System.Windows.Application.Current?.Dispatcher is not null) {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            foreach (var cameraItemInfoModel in list.Select(camera => CameraItems?.FirstOrDefault(f => f.SerialNumber.Equals(camera.Info?.SerialNumber))).OfType<CameraItemInfoModel>()) {
                                cameraItemInfoModel.Status = CameraStatus.Disconnected;
                            }
                        }, DispatcherPriority.Background);
                    }
                }
                finally {
                    UpdateSlim.Release();
                }
            };
            //异常
            _deviceService.CameraFault += async delegate (object? sender, List<ICamera> list) {
                try {
                    await UpdateSlim.WaitAsync();
                    if (System.Windows.Application.Current?.Dispatcher is not null) {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            foreach (var camera in list) {
                                var cameraItemInfoModel = CameraItems?.FirstOrDefault(f => f.SerialNumber.Equals(camera.Info.SerialNumber));
                                if (cameraItemInfoModel is not null) {
                                    cameraItemInfoModel.Status = CameraStatus.Disconnected;
                                }
                            }
                        }, DispatcherPriority.Background);
                    }
                }
                finally {
                    UpdateSlim.Release();
                }
            };
            //相机初始化
            _deviceService.CameraInitialized += async delegate (object? sender, List<ICamera> list) {
                try {
                    await UpdateSlim.WaitAsync();
                    if (System.Windows.Application.Current?.Dispatcher is not null) {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            foreach (var camera in list) {
                                var cameraItemInfoModel = CameraItems?.FirstOrDefault(f => f.SerialNumber.Equals(camera.Info.SerialNumber));
                                if (cameraItemInfoModel is not null) {
                                    cameraItemInfoModel.Status = CameraStatus.Running;
                                }
                                else {
                                    CameraItems?.Add(new CameraItemInfoModel() {
                                        SerialNumber = camera?.Info?.SerialNumber ?? string.Empty,
                                        Type = (CameraType)(camera?.Info?.Type ?? Camera.CameraType.IndustrialCamera),
                                        ConnectionType = (camera?.Info?.ConnectionType ?? CameraConnectionType.Unknown),
                                        BindingType = camera?.BindingType ?? CameraBindingType.ScannerCamera,
                                        Status = CameraStatus.Running
                                    });
                                }
                            }
                        }, DispatcherPriority.Background);
                    }
                }
                finally {
                    UpdateSlim.Release();
                }
            };
            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                if (item is SettingsChangedEvent info) {
                    var newConnectionItems = new List<ConnectionItemInfoModel>();
                        //判断添加
                        //FTP图片上传
                        var imageSettingsDto = await configRepository
                            .FirstOrDefaultEntity<ImageSettingsDto>("SaveImageSettings")
                            .ConfigureAwait(false) ?? new ImageSettingsDto();
                        if (imageSettingsDto.IsFtpUploadEnabled) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "FTP图片上传",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.FTP,
                            });
                        }
                        //称重
                        var weightSettingsDto = await configRepository
                            .FirstOrDefaultEntity<WeightSettingsDto>("WeightSettings")
                            .ConfigureAwait(false) ?? new WeightSettingsDto();
                        if (weightSettingsDto.Mode == WeightMode.Dynamic) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "动态称重",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        else if (weightSettingsDto.Mode == WeightMode.Static) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "静态称重",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        //体积
                        var volumeSettingsDto = await configRepository
                            .FirstOrDefaultEntity<VolumeSettingsDto>("VolumeSettings")
                            .ConfigureAwait(false) ?? new VolumeSettingsDto();
                        if (volumeSettingsDto.IsUseExternalVolumeInput) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "外部体积",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp ? Models.StatusBarModels.ConnectionType.TCP : Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        //TCP输出结果
                        var resultOutputSettingsDto = await configRepository
                                                          .FirstOrDefaultEntity<ResultOutputSettingsDto>("ResultOutputSettings")
                                                          .ConfigureAwait(false) ??
                                                      new ResultOutputSettingsDto();
                        if (resultOutputSettingsDto.IsUseTcpOutput) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "TCP输出结果",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }
                        //串口输出结果
                        if (resultOutputSettingsDto.IsUseSerialOutput) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "串口输出结果",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        //音频输出
                        if (resultOutputSettingsDto.IsUseAudioOutput) {
                            //检测文件是否存在
                            var total = await _soundRepository.Total(t => t.Id > 0).ConfigureAwait(false);
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "音频输出",
                                ConnectionState = total > 0 ? ConnectionState.Connected : ConnectionState.ConnectionFailed,
                                ConnectionType = Models.StatusBarModels.ConnectionType.Audio,
                            });
                        }
                        //位置输出
                        if (resultOutputSettingsDto.IsUseLocationOutput) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "位置输出",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.Location,
                            });
                        }
                        //控件输入
                        var contentInputSettingsDto = await configRepository
                                                          .FirstOrDefaultEntity<ContentInputSettingsDto>("ContentInputSettings")
                                                          .ConfigureAwait(false) ??
                                                      new ContentInputSettingsDto();
                        if (contentInputSettingsDto.IsUseControlInput) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "控件输入",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.Custom,
                            });
                        }
                        //Tcp输入
                        if (contentInputSettingsDto.IsUseTcpInput) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "Tcp输入",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }
                        //获取下位机连接
                        var models = await communicationConnectionConfigRepository.
                            CommunicationConnectionConfigItems(s => s.Id > 0).ConfigureAwait(false);
                        models.ForEach(f => {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = $"[下位机]{f.ConnectionName}",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = f.CommunicationType == 1 ? Models.StatusBarModels.ConnectionType.SerialPort : Models.StatusBarModels.ConnectionType.TCP,
                            });
                        });
                        //锁格
                        var packageExitLockSettingsDto = await configRepository
                                                             .FirstOrDefaultEntity<PackageExitLockSettingsDto>("PackageExitLockSettings")
                                                             .ConfigureAwait(false) ??
                                                         new PackageExitLockSettingsDto();
                        if (packageExitLockSettingsDto.IsUsePackageExitLock) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = $"锁格检测",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }

                        //叠包

                        var stackedPackageDetectionSettingsDto = await configRepository
                                                                     .FirstOrDefaultEntity<StackedPackageDetectionSettingsDto>("StackedPackageDetectionSettings")
                                                                     .ConfigureAwait(false) ??
                                                                 new StackedPackageDetectionSettingsDto();
                        if (stackedPackageDetectionSettingsDto.IsStackedPackageDetection) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = $"叠包检测",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }

                        //灰度仪
                        var grayscaleDeviceSettingsDto = await configRepository
                                                             .FirstOrDefaultEntity<GrayscaleDeviceSettingsDto>("GrayscaleDeviceSettings")
                                                             .ConfigureAwait(false) ??
                                                         new GrayscaleDeviceSettingsDto();
                        if (grayscaleDeviceSettingsDto.IsUseGrayscaleDetector) {
                            newConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = $"灰度仪",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        var itemsToRemove = ConnectionItems.Except(newConnectionItems, new ConnectionItemInfoModelComparer()).ToList();
                        var itemsToAdd = newConnectionItems.Except(ConnectionItems, new ConnectionItemInfoModelComparer()).ToList();

                        foreach (var model in itemsToRemove) {
                            ConnectionItems.Remove(model);
                        }
                        foreach (var model in itemsToAdd) {
                            ConnectionItems.Add(model);
                        }
                    }, DispatcherPriority.Background);
                }
            });

            //FTP事件
            _ftp.Connected += (sender, args) =>
                SetConnectionState("FTP图片上传", ConnectionState.Connected);
            _ftp.Disconnected += (sender, args) =>
                SetConnectionState("FTP图片上传", ConnectionState.ConnectionFailed);

            //静态、动态称重[连接、断开]事件
            _dynamicScale.Connected += (sender, scale) =>
                SetConnectionState("动态称重", ConnectionState.Connected);
            _dynamicScale.Disconnected += (sender, scale) =>
                SetConnectionState("动态称重", ConnectionState.ConnectionFailed);

            _staticScale.Connected += (sender, scale) =>
                SetConnectionState("静态称重", ConnectionState.Connected);
            _staticScale.Disconnected += (sender, scale) =>
                SetConnectionState("静态称重", ConnectionState.ConnectionFailed);

            //外部体积输入[连接、断开]事件
            _tcpVolumeInput.Connected += (sender, volumeInput) =>
                SetConnectionState("外部体积", ConnectionState.Connected);
            _tcpVolumeInput.Disconnected += (sender, volumeInput) =>
                SetConnectionState("外部体积", ConnectionState.ConnectionFailed);
            //内容输入 [连接、断开]事件
            _tcpContentInput.Connected += (sender, contentInput) =>
                SetConnectionState("Tcp输入", ConnectionState.Connected);
            _tcpContentInput.Disconnected += (sender, contentInput) =>
                SetConnectionState("Tcp输入", ConnectionState.ConnectionFailed);
            //Tcp输出[连接、断开]事件
            _tcpContentOutput.Connected += (sender, contentInput) =>
                SetConnectionState("TCP输出结果", ConnectionState.Connected);
            _tcpContentOutput.Disconnected += (sender, contentInput) =>
                SetConnectionState("TCP输出结果", ConnectionState.ConnectionFailed);

            //锁格[连接、断开]事件

            _exitMonitor.Connected += (sender, args) =>
                SetConnectionState("锁格检测", ConnectionState.Connected);
            _exitMonitor.Disconnected += (sender, args) =>
                SetConnectionState("锁格检测", ConnectionState.ConnectionFailed);
            //叠包[连接、断开]事件

            _stackedPackageService.Connected += (sender, args) =>
                SetConnectionState("叠包检测", ConnectionState.Connected);
            _stackedPackageService.Disconnected += (sender, args) =>
                SetConnectionState("叠包检测", ConnectionState.ConnectionFailed);

            //下位机 [连接、断开]事件
            _sortingConnectionService.Connected += (sender, info) =>
                SetConnectionState(info.ConnectionName, ConnectionState.Connected, true);
            _sortingConnectionService.Disconnected += (sender, info) =>
                SetConnectionState(info.ConnectionName, ConnectionState.ConnectionFailed, true);
            //灰度仪 [连接、断开]事件
            _grayscaleService.Connected += (sender, service) =>
                SetConnectionState("灰度仪", ConnectionState.Connected);
            _grayscaleService.Disconnected += (sender, service) =>
                SetConnectionState("灰度仪", ConnectionState.ConnectionFailed);
        }

        /// <summary>
        /// 在 UI 线程更新连接状态，避免设备回调跨线程访问绑定集合。
        /// </summary>
        /// <param name="connectionName">连接名称。</param>
        /// <param name="state">目标状态。</param>
        /// <param name="matchSuffix">是否按名称后缀匹配。</param>
        private void SetConnectionState(string connectionName, ConnectionState state, bool matchSuffix = false) {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is null) {
                return;
            }
            Action updateState = () => {
                var model = ConnectionItems.FirstOrDefault(item =>
                    matchSuffix
                        ? item.ConnectionName.EndsWith(connectionName, StringComparison.Ordinal)
                        : item.ConnectionName.Equals(connectionName, StringComparison.Ordinal));
                if (model is not null) {
                    model.ConnectionState = state;
                }
            };
            if (dispatcher.CheckAccess()) {
                updateState();
            }
            else {
                _ = dispatcher.InvokeAsync(updateState, DispatcherPriority.Background);
            }
        }

        public string FormattedElapsed {
            get => _formattedElapsed;
            set => SetProperty(ref _formattedElapsed, value);
        }

        public ObservableCollection<string> ExceptionItems {
            get => _exceptionItems;
            set => SetProperty(ref _exceptionItems, value);
        }

        public ObservableCollection<CameraItemInfoModel> CameraItems {
            get => _cameraItems;
            set => SetProperty(ref _cameraItems, value);
        }

        public ObservableCollection<SerialPortInfoModel> SerialPortItems {
            get => _serialPortItems;
            set => SetProperty(ref _serialPortItems, value);
        }

        public SolidColorBrush ConnectionSolidColorBrush {
            get => _connectionSolidColorBrush;
            set => SetProperty(ref _connectionSolidColorBrush, value);
        }

        public SolidColorBrush CameraSolidColorBrush {
            get => _cameraSolidColorBrush;
            set => SetProperty(ref _cameraSolidColorBrush, value);
        }

        /// <summary>
        /// 连接信息
        /// </summary>
        public ObservableCollection<ConnectionItemInfoModel> ConnectionItems {
            get => _connectionItems;
            set => SetProperty(ref _connectionItems, value);
        }

        /// <summary>
        /// 电脑信息
        /// </summary>
        public ComputerInfoModel ComputerInfo {
            get => _computerInfo;
            set => SetProperty(ref _computerInfo, value);
        }

        public ICommand ClearExceptionCommand => new DelegateCommand<object>(ClearExceptionDelegate);

        private async void ClearExceptionDelegate(object obj) {
            //清空异常信息
            ExceptionItems?.Clear();
        }
    }
}
