using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.IO.Ports;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.Filters;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration {

    public class StackedPackageDetectionSettingsViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private readonly IDeviceService _deviceService;
        private bool _isSavingInProgress;
        private ObservableCollection<string> _portItems = new();

        private ObservableCollection<CommunicationsTypeInfoModel> _communicationsTypeItems = new()
        {
            new CommunicationsTypeInfoModel()
            {
                Name = "串口通讯",
                Value = CommunicationsType.SerialPort,
            },
            new CommunicationsTypeInfoModel()
            {
                Name = "TCP通讯",
                Value = CommunicationsType.TCP,
            },
        };

        private CommunicationsTypeInfoModel _selectCommunicationsType = new();

        private ObservableCollection<ParityInfoModel> _parityItems = new()
        {
            new ParityInfoModel()
            {
                Name = "None",
                Value = Parity.None
            },
            new ParityInfoModel()
            {
                Name = "Odd",
                Value = Parity.Odd
            },
            new ParityInfoModel()
            {
                Name = "Even",
                Value = Parity.Even
            },
            new ParityInfoModel()
            {
                Name = "Mark",
                Value = Parity.Mark
            },
            new ParityInfoModel()
            {
                Name = "Space",
                Value = Parity.Space
            },
        };

        private ObservableCollection<StopBitsInfoModel> _stopBitsItems = new()
        {
            new StopBitsInfoModel()
            {
                Name = "None",
                Value = 0,
            },
            new StopBitsInfoModel()
            {
                Name = "One",
                Value = StopBits.One,
            },
            new StopBitsInfoModel()
            {
                Name = "Two",
                Value = StopBits.Two,
            },
            new StopBitsInfoModel()
            {
                Name = "OnePointFive",
                Value = StopBits.OnePointFive,
            },
        };

        private ObservableCollection<int> _baudRateItems = new()
        {
            4800,9600,14400,19200,38400,115200
        };

        private ObservableCollection<int> _dataBitsItems = new()
        {
            5,6,7,8,
        };

        private ObservableCollection<DataFormatTypeInfoModel> _dataFormatTypeItems = new()
        {
            new DataFormatTypeInfoModel()
            {
                Name = "Ascii",
                Value = DataFormatType.Ascii
            },
            new DataFormatTypeInfoModel()
            {
                Name = "Hex",
                Value = DataFormatType.Hex
            },
        };

        private SnackbarMessageQueue _stackedPackageDetectionSettingsMessageQueue = new();
        private StackedPackageDetectionItemInfoModel _stackedPackageDetectionItemInfo = new();
        private bool _isLoaded;

        public StackedPackageDetectionSettingsViewModel(IConfigRepository configRepository,
            IDeviceService deviceService) {
            _configRepository = configRepository;
            _deviceService = deviceService;
        }

        public ObservableCollection<CommunicationsTypeInfoModel> CommunicationsTypeItems {
            get => _communicationsTypeItems;
            set => SetProperty(ref _communicationsTypeItems, value);
        }

        public CommunicationsTypeInfoModel SelectCommunicationsType {
            get => _selectCommunicationsType;
            set => SetProperty(ref _selectCommunicationsType, value);
        }

        public ObservableCollection<DataFormatTypeInfoModel> DataFormatTypeItems {
            get => _dataFormatTypeItems;
            set => SetProperty(ref _dataFormatTypeItems, value);
        }

        /// <summary>
        /// 串口列表
        /// </summary>
        public ObservableCollection<string> PortItems {
            get => _portItems;
            set => SetProperty(ref _portItems, value);
        }

        /// <summary>
        /// 效验位下拉选项
        /// </summary>
        public ObservableCollection<ParityInfoModel> ParityItems {
            get => _parityItems;
            set => SetProperty(ref _parityItems, value);
        }

        /// <summary>
        /// 停止位下拉选项
        /// </summary>
        public ObservableCollection<StopBitsInfoModel> StopBitsItems {
            get => _stopBitsItems;
            set => SetProperty(ref _stopBitsItems, value);
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public ObservableCollection<int> BaudRateItems {
            get => _baudRateItems;
            set => SetProperty(ref _baudRateItems, value);
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public ObservableCollection<int> DataBitsItems {
            get => _dataBitsItems;
            set => SetProperty(ref _dataBitsItems, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public SnackbarMessageQueue StackedPackageDetectionSettingsMessageQueue {
            get => _stackedPackageDetectionSettingsMessageQueue;
            set => SetProperty(ref _stackedPackageDetectionSettingsMessageQueue, value);
        }

        public StackedPackageDetectionItemInfoModel StackedPackageDetectionItemInfo {
            get => _stackedPackageDetectionItemInfo;
            set => SetProperty(ref _stackedPackageDetectionItemInfo, value);
        }

        /// <summary>
        /// 串口刷新
        /// </summary>
        public ICommand PortUpdateCommand => new DelegateCommand(PortUpdateDelegate);

        private async void PortUpdateDelegate() {
            //重新枚举串口
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                PortItems.Clear();
                PortItems.AddRange(SerialPort.GetPortNames());
            });
        }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveSettingsCommand => new DelegateCommand(SaveDelegate);

        private async void SaveDelegate() {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    if (_deviceService.RunningStatus) {
                        IsSavingInProgress = false;
                        StackedPackageDetectionSettingsMessageQueue.Enqueue($"设备工作中,无法设置");
                        return;
                    }

                    string regularExpression = string.Empty;
                    if (!string.IsNullOrEmpty(StackedPackageDetectionItemInfo.CheckerContent)) {
                        var strings = StackedPackageDetectionItemInfo.CheckerContent.Split(";");
                        var patterns = strings.Select(s => $"(?=.*{s})").ToList();
                        regularExpression = string.Join("|", patterns);
                    }

                    var stackedPackageDetectionSettingsDto = new StackedPackageDetectionSettingsDto() {
                        CheckerContent = StackedPackageDetectionItemInfo.CheckerContent,
                        CommunicationType = StackedPackageDetectionItemInfo.CommunicationsType.Value,
                        IsStackedPackageDetection = StackedPackageDetectionItemInfo.IsStackedPackageDetection,
                        RegularExpression = regularExpression,
                        SerialPortConfigInfo = new SerialPortSettingsInfo() {
                            BaudRate = StackedPackageDetectionItemInfo.SerialPortConfigInfo?.BaudRate ?? 0,
                            DataBits = StackedPackageDetectionItemInfo.SerialPortConfigInfo?.DataBits ?? 0,
                            DataFormat = StackedPackageDetectionItemInfo.SerialPortConfigInfo?.DataFormat?.Value ??
                                         DataFormatType.Ascii,
                            PortName = StackedPackageDetectionItemInfo.SerialPortConfigInfo?.PortName ?? string.Empty,
                            Parity = StackedPackageDetectionItemInfo.SerialPortConfigInfo?.Parity?.Value ?? Parity.None,
                            StopBits = StackedPackageDetectionItemInfo.SerialPortConfigInfo?.StopBits?.Value ?? StopBits.None
                        },
                        TcpConnectionConfigInfo = new TcpSettingsInfo() {
                            ConnectionMode = StackedPackageDetectionItemInfo.TcpConnectionConfigInfo?.ConnectionMode ??
                                             TcpConnectionMode.Client,
                            DataFormat = StackedPackageDetectionItemInfo.TcpConnectionConfigInfo?.DataFormat?.Value ??
                                         DataFormatType.Ascii,

                            ClientConfig = new TcpInfo() {
                                IpAddress = StackedPackageDetectionItemInfo.TcpConnectionConfigInfo?.ClientParameter
                                                ?.IpAddress ??
                                            string.Empty,
                                Port = StackedPackageDetectionItemInfo.TcpConnectionConfigInfo?.ClientParameter?.Port ??
                                       0,
                            },
                            ServerConfig = new TcpInfo() {
                                IpAddress = StackedPackageDetectionItemInfo.TcpConnectionConfigInfo?.ServerParameter
                                                ?.IpAddress ??
                                            string.Empty,
                                Port = StackedPackageDetectionItemInfo.TcpConnectionConfigInfo?.ServerParameter?.Port ??
                                       0,
                            }
                        }
                    };
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "StackedPackageDetectionSettings",
                        Value = JsonConvert.SerializeObject(stackedPackageDetectionSettingsDto)
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "StackedPackageDetectionSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    StackedPackageDetectionSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(insertOrUpdate ?
                        Languages.Language.ResourceManager.GetString("Success") :
                        Languages.Language.ResourceManager.GetString("Failure"))}");
                });
            }
        }

        /// <summary>
        /// 加载方法
        /// </summary>
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    PortItems.Clear();
                    PortItems.AddRange(SerialPort.GetPortNames());
                    //读配置

                    var configInfoModel = await _configRepository.
                        FirstOrDefault(f => f.ConfigName.Equals("StackedPackageDetectionSettings"));

                    if (configInfoModel is not null) {
                        try {
                            var settingsDto = JsonConvert.DeserializeObject<StackedPackageDetectionSettingsDto>(configInfoModel.Value);
                            if (settingsDto is not null) {
                                StackedPackageDetectionItemInfo = new StackedPackageDetectionItemInfoModel() {
                                    CheckerContent = settingsDto.CheckerContent,
                                    CommunicationsType =
                                        CommunicationsTypeItems.FirstOrDefault(f =>
                                            f.Value.Equals(settingsDto.CommunicationType)) ??
                                        new CommunicationsTypeInfoModel(),
                                    IsStackedPackageDetection = settingsDto.IsStackedPackageDetection,
                                    RegularExpression = settingsDto.RegularExpression,
                                    SerialPortConfigInfo = new SerialPortConfigItemInfoModel() {
                                        BaudRate = settingsDto.SerialPortConfigInfo?.BaudRate ?? 0,
                                        DataBits = settingsDto.SerialPortConfigInfo?.DataBits ?? 0,
                                        DataFormat =
                                            DataFormatTypeItems.FirstOrDefault(f =>
                                                f.Value.Equals(settingsDto.SerialPortConfigInfo?.DataFormat)) ??
                                            new DataFormatTypeInfoModel(),
                                        PortName = settingsDto.SerialPortConfigInfo?.PortName ?? string.Empty,
                                        Parity = ParityItems.FirstOrDefault(f =>
                                                     f.Value.Equals(settingsDto.SerialPortConfigInfo?.Parity)) ??
                                                 new ParityInfoModel(),
                                        StopBits = StopBitsItems.FirstOrDefault(f =>
                                            f.Value.Equals(settingsDto.SerialPortConfigInfo?.StopBits)) ?? new StopBitsInfoModel()
                                    },
                                    TcpConnectionConfigInfo = new TcpConnectionConfigItemInfoModel() {
                                        ConnectionMode = settingsDto.TcpConnectionConfigInfo?.ConnectionMode ??
                                                         TcpConnectionMode.Client,
                                        DataFormat = DataFormatTypeItems.FirstOrDefault(f =>
                                                         f.Value.Equals(settingsDto.TcpConnectionConfigInfo?.DataFormat)) ??
                                                     new DataFormatTypeInfoModel(),
                                        ClientParameter = new TcpConfigItemInfoModel() {
                                            IpAddress = settingsDto.TcpConnectionConfigInfo?.ClientConfig?.IpAddress ??
                                                        string.Empty,
                                            Port = settingsDto.TcpConnectionConfigInfo?.ClientConfig?.Port ?? 0,
                                        },
                                        ServerParameter = new TcpConfigItemInfoModel() {
                                            IpAddress = settingsDto.TcpConnectionConfigInfo?.ServerConfig?.IpAddress ??
                                                        string.Empty,
                                            Port = settingsDto.TcpConnectionConfigInfo?.ServerConfig?.Port ?? 0,
                                        }
                                    }
                                };
                            }
                        }
                        catch (Exception e) {
                            StackedPackageDetectionSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty}:{e.Message}");
                        }
                    }
                });
            }
        }
    }
}