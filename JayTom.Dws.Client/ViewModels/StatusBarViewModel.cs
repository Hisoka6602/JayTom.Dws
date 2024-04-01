using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Threading;
using JayTom.Dws.Camera;
using System.Windows.Input;
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
using JayTom.Dws.Client.Models.StatusBarModels;
using CameraType = JayTom.Dws.Client.Models.CameraType;
using CameraStatus = JayTom.Dws.Client.Models.CameraStatus;
using ConnectionType = JayTom.Dws.Client.Models.ConnectionType;

namespace JayTom.Dws.Client.ViewModels {

    public class StatusBarViewModel : BindableBase {
        private readonly IComputerInfoReporter _computerInfoReporter;
        private readonly IDeviceService _deviceService;
        private static SemaphoreSlim _updateSlim = new(1, 1);

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

        private ObservableCollection<ConnectionItemInfoModel> _connectionItems = new() {
            new ConnectionItemInfoModel()
            {
                ConnectionName = "FTP图片上传",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.FTP,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "静态称重",
                ConnectionState = ConnectionState.ConnectionFailed,
                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "动态称重",
                ConnectionState = ConnectionState.Connecting,
                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "外部体积",
                ConnectionState = ConnectionState.Connecting,
                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "TCP输出结果",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "串口输出结果",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.SerialPort,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "音频输出",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.Audio,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "位置输出",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.Location,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "控件输入",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.Custom,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "下位机通讯[分拣]",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
            },
            new ConnectionItemInfoModel()
            {
                ConnectionName = "下位机通讯[下包]",
                ConnectionState = ConnectionState.Connected,
                ConnectionType = Models.StatusBarModels.ConnectionType.TCP,
            },
        };

        public StatusBarViewModel(IComputerInfoReporter computerInfoReporter,
            IDeviceService deviceService) {
            _computerInfoReporter = computerInfoReporter;
            _deviceService = deviceService;
            _computerInfoReporter.ComputerInfoReceived += async delegate (object? sender, ComputerInfoModel model) {
                await Task.Run(async () => {
                    try {
                        if (System.Windows.Application.Current?.Dispatcher is not null) {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                //加载到界面内容
                                ComputerInfo = model;
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
                        await _updateSlim.WaitAsync();
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
                        _updateSlim.Release();
                    }
                }
            });
            //解绑
            _deviceService.CameraUnbound += async delegate (object? sender, CameraFinderItemInfoModel model) {
                try {
                    await _updateSlim.WaitAsync();
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
                    _updateSlim.Release();
                }
            };
            //断开
            _deviceService.CameraDisconnected += async delegate (object? sender, List<ICamera> list) {
                try {
                    await _updateSlim.WaitAsync();
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
                    _updateSlim.Release();
                }
            };
            //异常
            _deviceService.CameraFault += async delegate (object? sender, List<ICamera> list) {
                try {
                    await _updateSlim.WaitAsync();
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
                    _updateSlim.Release();
                }
            };
            //相机初始化
            _deviceService.CameraInitialized += async delegate (object? sender, List<ICamera> list) {
                try {
                    await _updateSlim.WaitAsync();
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
                    _updateSlim.Release();
                }
            };

            //启动事件

            //停止事件

            //FTP事件

            //静态、动态称重[连接、断开]事件
            //外部体积输入[连接、断开]事件
            //Tcp输出[连接、断开]事件
            //串口输出[连接、断开]事件
            //音频内容检测(写在启动触发后)
            //内容输入 [连接、断开]事件
            //下位机 [连接、断开]事件
            //锁格[连接、断开]事件
            //叠包[连接、断开]事件
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