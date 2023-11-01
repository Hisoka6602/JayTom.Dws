using JayTom.Dws.Client.Models;
using JayTom.Dws.Client.Service;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace JayTom.Dws.Client.ViewModels {

    public class StatusBarViewModel : BindableBase {
        private readonly IComputerInfoReporter _computerInfoReporter;

        private ObservableCollection<string> _exceptionItems = new()
        {
            "默认异常信息1","默认异常信息2","默认异常信息3这是很长的信息，会自动换行",
        };

        private ObservableCollection<CameraItemInfoModel> _cameraItems = new()
        {
            new CameraItemInfoModel()
            {
                CameraName = "海康工业相机.1",
                Status = CameraStatus.Failure,
                Type = CameraType.IndustrialCamera,
                ConnectionType = ConnectionType.Bluetooth,
            },
            new CameraItemInfoModel()
            {
                CameraName = "海康工业相机.2",
                Status = CameraStatus.Running,
                Type = CameraType.PanoramicCamera,
                ConnectionType = ConnectionType.Ethernet,
            },
            new CameraItemInfoModel()
            {
                CameraName = "海康工业相机.2",
                Status = CameraStatus.Running,
                Type = CameraType.PanoramicCamera,
                ConnectionType = ConnectionType.Ethernet,
            },
        };

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

        public StatusBarViewModel() {
        }

        public StatusBarViewModel(IComputerInfoReporter computerInfoReporter) {
            _computerInfoReporter = computerInfoReporter;
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