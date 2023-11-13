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
using CameraType = JayTom.Dws.Client.Models.CameraType;
using CameraStatus = JayTom.Dws.Client.Models.CameraStatus;

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