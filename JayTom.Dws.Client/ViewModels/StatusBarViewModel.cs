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
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.StatusBarModels;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using CameraStatus = JayTom.Dws.Client.Models.CameraStatus;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

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
        private SolidColorBrush _connectionSolidColorBrush = new(Colors.DarkGray);
        private SolidColorBrush _cameraSolidColorBrush = new(Colors.DarkGray);

        public StatusBarViewModel(IComputerInfoReporter computerInfoReporter,
            IDeviceService deviceService, IConfigRepository configRepository,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository,
            IFtp ftp, IDynamicScale dynamicScale,
            IStaticScale staticScale, ITcpVolumeInput tcpVolumeInput,
            ITcpContentInput tcpContentInput, ITcpContentOutput tcpContentOutput,
            IExitMonitor exitMonitor,
            IStackedPackageService stackedPackageService,
            ISortingConnectionService sortingConnectionService) {
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
            _computerInfoReporter.ComputerInfoReceived += async delegate (object? sender, ComputerInfoModel model) {
                await Task.Run(async () => {
                    try {
                        if (System.Windows.Application.Current?.Dispatcher is not null) {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                //加载到界面内容
                                ComputerInfo = model;
                                if (ConnectionItems.Any(a => a.ConnectionState is ConnectionState.ConnectionFailed)) {
                                    ConnectionSolidColorBrush = new SolidColorBrush(Colors.Red);
                                }
                                else if (ConnectionItems.Any(a => a.ConnectionState == ConnectionState.Disconnected)) {
                                    ConnectionSolidColorBrush = new SolidColorBrush(Colors.DarkGray);
                                }
                                else if (ConnectionItems.All(a => a.ConnectionState == ConnectionState.Connected)) {
                                    ConnectionSolidColorBrush = new SolidColorBrush(Colors.LimeGreen);
                                }

                                if (CameraItems.Any(a => a.Status == CameraStatus.Failure)) {
                                    CameraSolidColorBrush = new SolidColorBrush(Colors.Red);
                                }
                                else if (CameraItems.Any(a => a.Status == CameraStatus.Disconnected)) {
                                    CameraSolidColorBrush = new SolidColorBrush(Colors.DarkGray);
                                }
                                else if (CameraItems.All(a => a.Status == CameraStatus.Running)) {
                                    CameraSolidColorBrush = new SolidColorBrush(Colors.LimeGreen);
                                }
                            }, DispatcherPriority.Background);
                        }
                    }
                    catch (TaskCanceledException) {
                        //
                    }
                    catch (Exception e) {
                    }
                });
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
                                        ConnectionType = (ConnectionType)(camera?.Info?.ConnectionType ?? CameraConnectionType.Unknown),
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
                    await Task.Yield();
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                        ConnectionItems.Clear();
                        //判断添加
                        //FTP图片上传
                        var imageSettingsDto = await configRepository.FirstOrDefaultEntity<ImageSettingsDto>("SaveImageSettings") ?? new ImageSettingsDto();
                        if (imageSettingsDto.IsFtpUploadEnabled) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "FTP图片上传",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.FTP,
                            });
                        }
                        //称重
                        var weightSettingsDto = await configRepository.FirstOrDefaultEntity<WeightSettingsDto>("WeightSettings") ?? new WeightSettingsDto();
                        if (weightSettingsDto.Mode == WeightMode.Dynamic) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "动态称重",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        else if (weightSettingsDto.Mode == WeightMode.Static) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "静态称重",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        //体积
                        var volumeSettingsDto = await configRepository.FirstOrDefaultEntity<VolumeSettingsDto>("VolumeSettings") ?? new VolumeSettingsDto();
                        if (volumeSettingsDto.IsUseExternalVolumeInput) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "外部体积",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = volumeSettingsDto.VolumeInformationRequesterInfo.VolumeRequesterType == VolumeRequesterType.Tcp ? Models.StatusBarModels.ConnectionType.TCP : Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        //TCP输出结果
                        var resultOutputSettingsDto = await configRepository
                                                          .FirstOrDefaultEntity<ResultOutputSettingsDto>("ResultOutputSettings") ??
                                                      new ResultOutputSettingsDto();
                        if (resultOutputSettingsDto.IsUseTcpOutput) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "TCP输出结果",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }
                        //串口输出结果
                        if (resultOutputSettingsDto.IsUseSerialOutput) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "串口输出结果",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
                            });
                        }
                        //音频输出
                        if (resultOutputSettingsDto.IsUseAudioOutput) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "音频输出",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.Audio,
                            });
                        }
                        //位置输出
                        if (resultOutputSettingsDto.IsUseLocationOutput) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "位置输出",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.Location,
                            });
                        }
                        //控件输入
                        var contentInputSettingsDto = await configRepository
                                                          .FirstOrDefaultEntity<ContentInputSettingsDto>("ContentInputSettings") ??
                                                      new ContentInputSettingsDto();
                        if (contentInputSettingsDto.IsUseControlInput) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "控件输入",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.Custom,
                            });
                        }
                        //Tcp输入
                        if (contentInputSettingsDto.IsUseTcpInput) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = "Tcp输入",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }
                        //获取下位机连接
                        var models = await communicationConnectionConfigRepository.
                            CommunicationConnectionConfigItems(s => s.Id > 0);
                        models.ForEach(f => {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = $"[下位机]{f.ConnectionName}",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = f.CommunicationType == 1 ? Models.StatusBarModels.ConnectionType.SerialPort : Models.StatusBarModels.ConnectionType.TCP,
                            });
                        });
                        //锁格
                        var packageExitLockSettingsDto = await configRepository
                                                             .FirstOrDefaultEntity<PackageExitLockSettingsDto>("PackageExitLockSettings") ??
                                                         new PackageExitLockSettingsDto();
                        if (packageExitLockSettingsDto.IsUsePackageExitLock) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = $"锁格检测",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }

                        //叠包

                        var stackedPackageDetectionSettingsDto = await configRepository
                                                                     .FirstOrDefaultEntity<StackedPackageDetectionSettingsDto>("StackedPackageDetectionSettings") ??
                                                                 new StackedPackageDetectionSettingsDto();
                        if (stackedPackageDetectionSettingsDto.IsStackedPackageDetection) {
                            ConnectionItems.Add(new ConnectionItemInfoModel() {
                                ConnectionName = $"叠包检测",
                                ConnectionState = ConnectionState.Disconnected,
                                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
                            });
                        }
                    });
                }
            });

            //FTP事件
            _ftp.Connected += (sender, args) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("FTP图片上传"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _ftp.Disconnected += (sender, args) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("FTP图片上传"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };

            //静态、动态称重[连接、断开]事件
            _dynamicScale.Connected += (sender, scale) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("动态称重"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _dynamicScale.Disconnected += (sender, scale) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("动态称重"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };

            _staticScale.Connected += (sender, scale) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("静态称重"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _staticScale.Disconnected += (sender, scale) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("静态称重"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };

            //外部体积输入[连接、断开]事件
            _tcpVolumeInput.Connected += (sender, volumeInput) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("外部体积"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _tcpVolumeInput.Disconnected += (sender, volumeInput) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("外部体积"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };
            //内容输入 [连接、断开]事件
            _tcpContentInput.Connected += (sender, contentInput) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("Tcp输入"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _tcpContentInput.Disconnected += (sender, contentInput) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("Tcp输入"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };
            //Tcp输出[连接、断开]事件
            _tcpContentOutput.Connected += (sender, contentInput) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("TCP输出结果"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _tcpContentOutput.Disconnected += (sender, contentInput) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("TCP输出结果"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };

            //锁格[连接、断开]事件

            _exitMonitor.Connected += (sender, args) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("锁格检测"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _exitMonitor.Disconnected += (sender, args) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("锁格检测"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };
            //叠包[连接、断开]事件

            _stackedPackageService.Connected += (sender, args) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("叠包检测"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _stackedPackageService.Disconnected += (sender, args) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.Equals("叠包检测"));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };

            //下位机 [连接、断开]事件
            _sortingConnectionService.Connected += (sender, info) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.EndsWith(info.ConnectionName));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.Connected;
                }
            };
            _sortingConnectionService.Disconnected += (sender, info) => {
                var model = ConnectionItems.FirstOrDefault(f => f.ConnectionName.EndsWith(info.ConnectionName));
                if (model is not null) {
                    model.ConnectionState = ConnectionState.ConnectionFailed;
                }
            };
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

        public ICommand ClearExceptionCommand {
            get => new DelegateCommand<object>(ClearExceptionDelegate);
        }

        private async void ClearExceptionDelegate(object obj) {
            //清空异常信息
            ExceptionItems?.Clear();
        }
    }
}