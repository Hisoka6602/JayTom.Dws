using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using Newtonsoft.Json;
using System.IO.Ports;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.PluginInterface.Utils;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.PackageSorting.Rule;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Models.CommunicationsSettingsModel;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.Models.PackageSorting.CommunicationConnectionSub;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration
{

    public class CommunicationsSettingsViewModel : BindableBase
    {
        private readonly IConfigRepository _configRepository;
        private readonly ISortingService _sortingService;
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        //private CommunicationsSettingsInfoModel _communicationsSettingsInfo = new();

        private ObservableCollection<CommunicationsTypeInfoModel> _communicationsTypeItems = new()
        {
            new CommunicationsTypeInfoModel()
            {
                Name = "不使用分拣",
                Value = CommunicationsType.None,
            },
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
            new CommunicationsTypeInfoModel()
            {
                Name = "USB通讯",
                Value = CommunicationsType.USB,
            },

            new CommunicationsTypeInfoModel()
            {
                Name = "CAN总线通讯",
                Value = CommunicationsType.CAN,
            },
        };

        private ObservableCollection<CommunicationProtocolInfoModel> _communicationProtocolItems = new()
        {
            new CommunicationProtocolInfoModel()
            {
                Name = "无协议",
                Value = CommunicationProtocol.None,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "Modbus",
                Value = CommunicationProtocol.ModBus,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "CC-Link",
                Value = CommunicationProtocol.CCLink,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "Profibus",
                Value = CommunicationProtocol.ProfiBus,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "Profinet",
                Value = CommunicationProtocol.Profinet,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "CANopen",
                Value = CommunicationProtocol.CANopen,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "无限创科协议",
                Value = CommunicationProtocol.Wxkc,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "江腾-窄带协议",
                Value = CommunicationProtocol.JT_ST,
            },
            new CommunicationProtocolInfoModel()
            {
                Name = "菜鸟分拣协议",
                Value = CommunicationProtocol.CaiNiao,
            },
        };

        private CommunicationsTypeInfoModel _selectCommunicationsType = new();
        private CommunicationProtocolInfoModel _selectCommunicationProtocol = new();
        private ObservableCollection<string> _portItems = new();

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

        private ParityInfoModel _selectParity = new();
        private StopBitsInfoModel _selectStopBits = new();
        private DataFormatTypeInfoModel _selectDataFormat = new();
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _communicationsSettingsMessageQueue = new(TimeSpan.FromSeconds(2));

        private ObservableCollection<CommunicationConnectionItemInfoModel> _communicationConnectionItems = new();

        public CommunicationsSettingsViewModel(IConfigRepository configRepository,
            ISortingService sortingService,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository)
        {
            _configRepository = configRepository;
            _sortingService = sortingService;
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _sortingService.ExceptionOccurred += delegate (object? sender, ExceptionEventArgs args)
            {
                CommunicationsSettingsMessageQueue.Enqueue(args.ExceptionMessage);
            };
        }

        public string SettingsName => "CommunicationsItemsSettings";

        public SnackbarMessageQueue CommunicationsSettingsMessageQueue
        {
            get => _communicationsSettingsMessageQueue;
            set => SetProperty(ref _communicationsSettingsMessageQueue, value);
        }

        /*public CommunicationsSettingsInfoModel CommunicationsSettingsInfo {
            get => _communicationsSettingsInfo;
            set => SetProperty(ref _communicationsSettingsInfo, value);
        }*/

        public ObservableCollection<CommunicationsTypeInfoModel> CommunicationsTypeItems
        {
            get => _communicationsTypeItems;
            set => SetProperty(ref _communicationsTypeItems, value);
        }

        public ObservableCollection<CommunicationProtocolInfoModel> CommunicationProtocolItems
        {
            get => _communicationProtocolItems;
            set => SetProperty(ref _communicationProtocolItems, value);
        }

        public CommunicationsTypeInfoModel SelectCommunicationsType
        {
            get => _selectCommunicationsType;
            set => SetProperty(ref _selectCommunicationsType, value);
        }

        public CommunicationProtocolInfoModel SelectCommunicationProtocol
        {
            get => _selectCommunicationProtocol;
            set => SetProperty(ref _selectCommunicationProtocol, value);
        }

        public ObservableCollection<DataFormatTypeInfoModel> DataFormatTypeItems
        {
            get => _dataFormatTypeItems;
            set => SetProperty(ref _dataFormatTypeItems, value);
        }

        public DataFormatTypeInfoModel SelectDataFormat
        {
            get => _selectDataFormat;
            set => SetProperty(ref _selectDataFormat, value);
        }

        /// <summary>
        /// 串口列表
        /// </summary>
        public ObservableCollection<string> PortItems
        {
            get => _portItems;
            set => SetProperty(ref _portItems, value);
        }

        /// <summary>
        /// 效验位下拉选项
        /// </summary>
        public ObservableCollection<ParityInfoModel> ParityItems
        {
            get => _parityItems;
            set => SetProperty(ref _parityItems, value);
        }

        /// <summary>
        /// 效验位
        /// </summary>
        public ParityInfoModel SelectParity
        {
            get => _selectParity;
            set => SetProperty(ref _selectParity, value);
        }

        /// <summary>
        /// 停止位下拉选项
        /// </summary>
        public ObservableCollection<StopBitsInfoModel> StopBitsItems
        {
            get => _stopBitsItems;
            set => SetProperty(ref _stopBitsItems, value);
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBitsInfoModel SelectStopBits
        {
            get => _selectStopBits;
            set => SetProperty(ref _selectStopBits, value);
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public ObservableCollection<int> BaudRateItems
        {
            get => _baudRateItems;
            set => SetProperty(ref _baudRateItems, value);
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public ObservableCollection<int> DataBitsItems
        {
            get => _dataBitsItems;
            set => SetProperty(ref _dataBitsItems, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress
        {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public ObservableCollection<CommunicationConnectionItemInfoModel> CommunicationConnectionItems
        {
            get => _communicationConnectionItems;
            set => SetProperty(ref _communicationConnectionItems, value);
        }

        /// <summary>
        /// 串口刷新
        /// </summary>
        public ICommand PortUpdateCommand
        {
            get => new DelegateCommand(PortUpdateDelegate);
        }

        private async void PortUpdateDelegate()
        {
            //重新枚举串口
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PortItems.Clear();
                PortItems.AddRange(SerialPort.GetPortNames());
            });
        }

        /// <summary>
        /// 窗口加载
        /// </summary>
        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private void LoadedDelegate(object obj)
        {
            RefreshData();
        }

        public ICommand AddCommand
        {
            get => new DelegateCommand<object>(AddDelegate);
        }

        private async void AddDelegate(object obj)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var recognitionEditor = new CommunicationConnectionConfigEditor();
                if (recognitionEditor.DataContext is CommunicationConnectionConfigEditorViewModel model)
                {
                    model.Identifier = "CommunicationsSettingsDialog";
                    await DialogHost.Show(recognitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        CommunicationsSettingsMessageQueue.Enqueue(model.ExceptionContent);
                        return;
                    }

                    if (model.IsOk)
                    {
                        //添加到数据库
                        var insertDetailAsync = await _communicationConnectionConfigRepository.InsertDetailAsync(
                            new CommunicationConnectionConfigInfoModel()
                            {
                                CommunicationProtocol =
                                    model.CommunicationConnectionItem.CommunicationProtocol.Value.ToString(),
                                CommunicationType = (int)model.CommunicationConnectionItem.CommunicationType.Value,
                                ConnectionName = model.CommunicationConnectionItem.ConnectionName,
                                CreateTime = DateTime.Now,
                                DeviceExtensionConfigInfo = new DeviceExtensionConfigInfoModel()
                                {
                                    CreateTime = DateTime.Now,
                                    ModifyTime = DateTime.Now,
                                    MaxRetryCount = model.CommunicationConnectionItem.DeviceExtensionConfigInfo
                                        ?.MaxRetryCount ?? 0,
                                    ValidateDeviceResponse = model.CommunicationConnectionItem.DeviceExtensionConfigInfo
                                        ?.ValidateDeviceResponse ?? false,
                                },
                                HeartbeatConfigInfo = new HeartbeatConfigInfoModel()
                                {
                                    CreateTime = DateTime.Now,
                                    ModifyTime = DateTime.Now,
                                    HeartbeatInterval = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.HeartbeatInterval ?? 0,
                                    HeartbeatContent =
                                        model.CommunicationConnectionItem.HeartbeatConfigInfo?.HeartbeatContent ??
                                        string.Empty,
                                    IsHeartbeatActive = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.IsHeartbeatActive ?? false,
                                    IsHeartbeatEnabled = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.IsHeartbeatEnabled ?? false,
                                    IsFixedHeartbeatContent = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.IsFixedHeartbeatContent ?? false,
                                },
                                IsActive = true,
                                IsAutoReconnect = model.CommunicationConnectionItem.IsAutoReconnect,
                                SerialPortConfigInfo = new SerialPortConfigInfoModel()
                                {
                                    BaudRate = model.CommunicationConnectionItem.SerialPortConfigInfo?.BaudRate ?? 0,
                                    CreateTime = DateTime.Now,
                                    DataBits = model.CommunicationConnectionItem.SerialPortConfigInfo?.DataBits ?? 0,
                                    DataFormat = (int)(model.CommunicationConnectionItem.SerialPortConfigInfo
                                        ?.DataFormat?.Value ?? 0),
                                    ModifyTime = DateTime.Now,
                                    Parity =
                                        (int)(model.CommunicationConnectionItem.SerialPortConfigInfo?.Parity.Value ??
                                              0),
                                    PortName = model.CommunicationConnectionItem.SerialPortConfigInfo?.PortName ??
                                               string.Empty,
                                    StopBits = (int)(model.CommunicationConnectionItem.SerialPortConfigInfo?.StopBits
                                        .Value ?? 0),
                                },
                                TcpConnectionConfigInfo = new TcpConnectionConfigInfoModel()
                                {
                                    CreateTime = DateTime.Now,
                                    ModifyTime = DateTime.Now,
                                    ConnectionMode = (int)(model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                        ?.ConnectionMode ?? 0),
                                    TcpConfigItems = new List<TcpConfigInfoModel>()
                                    {
                                        new() {
                                            IpAddress = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ServerParameter?.IpAddress ?? string.Empty,
                                            Port = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ServerParameter?.Port ?? 0,
                                            Type = 1
                                        },
                                        new() {
                                            IpAddress = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ClientParameter?.IpAddress ?? string.Empty,
                                            Port = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ClientParameter?.Port ?? 0,
                                            Type = 0
                                        }
                                    },
                                    DataFormat = (int)(model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                        ?.DataFormat?.Value ?? 0)
                                },
                            });
                        if (insertDetailAsync)
                        {
                            CommunicationsSettingsMessageQueue.Enqueue("保存成功");
                            RefreshData();
                            EventAggregator.Instance.Publish(new SettingsChangedEvent
                            {
                                SettingsName = SettingsName,
                                IsLocallySaved = true
                            });
                        }
                        else
                        {
                            CommunicationsSettingsMessageQueue.Enqueue("保存失败");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 修改
        /// </summary>
        public ICommand ModifyCommand
        {
            get => new DelegateCommand<CommunicationConnectionItemInfoModel>(ModifyDelegate);
        }

        private async void ModifyDelegate(CommunicationConnectionItemInfoModel obj)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var recognitionEditor = new CommunicationConnectionConfigEditor();
                if (recognitionEditor.DataContext is CommunicationConnectionConfigEditorViewModel model)
                {
                    model.Identifier = "CommunicationsSettingsDialog";
                    model.CommunicationProtocolItems = CommunicationProtocolItems;
                    model.DataFormatTypeItems = DataFormatTypeItems;
                    model.StopBitsItems = StopBitsItems;
                    model.BaudRateItems = BaudRateItems;
                    model.DataBitsItems = DataBitsItems;
                    model.CommunicationsTypeItems = CommunicationsTypeItems;
                    model.ParityItems = ParityItems;
                    model.CommunicationConnectionItem = obj;
                    await DialogHost.Show(recognitionEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.ExceptionContent))
                    {
                        CommunicationsSettingsMessageQueue.Enqueue(model.ExceptionContent);
                        RefreshData();
                        return;
                    }

                    if (model.IsOk)
                    {
                        //更新到数据库
                        var insertDetailAsync = await _communicationConnectionConfigRepository.UpdateDetailAsync(
                            new CommunicationConnectionConfigInfoModel()
                            {
                                Id = model.CommunicationConnectionItem.Id,
                                CommunicationProtocol =
                                    model.CommunicationConnectionItem.CommunicationProtocol.Value.ToString(),
                                CommunicationType = (int)model.CommunicationConnectionItem.CommunicationType.Value,
                                ConnectionName = model.CommunicationConnectionItem.ConnectionName,
                                DeviceExtensionConfigInfo = new DeviceExtensionConfigInfoModel()
                                {
                                    ModifyTime = DateTime.Now,
                                    MaxRetryCount = model.CommunicationConnectionItem.DeviceExtensionConfigInfo
                                        ?.MaxRetryCount ?? 0,
                                    ValidateDeviceResponse = model.CommunicationConnectionItem.DeviceExtensionConfigInfo
                                        ?.ValidateDeviceResponse ?? false,
                                },
                                HeartbeatConfigInfo = new HeartbeatConfigInfoModel()
                                {
                                    ModifyTime = DateTime.Now,
                                    HeartbeatInterval = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.HeartbeatInterval ?? 0,
                                    HeartbeatContent =
                                        model.CommunicationConnectionItem.HeartbeatConfigInfo?.HeartbeatContent ??
                                        string.Empty,
                                    IsHeartbeatActive = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.IsHeartbeatActive ?? false,
                                    IsHeartbeatEnabled = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.IsHeartbeatEnabled ?? false,
                                    IsFixedHeartbeatContent = model.CommunicationConnectionItem.HeartbeatConfigInfo
                                        ?.IsFixedHeartbeatContent ?? false,
                                },
                                IsActive = true,
                                IsAutoReconnect = model.CommunicationConnectionItem.IsAutoReconnect,
                                SerialPortConfigInfo = new SerialPortConfigInfoModel()
                                {
                                    BaudRate = model.CommunicationConnectionItem.SerialPortConfigInfo?.BaudRate ?? 0,
                                    DataBits = model.CommunicationConnectionItem.SerialPortConfigInfo?.DataBits ?? 0,
                                    DataFormat = (int)(model.CommunicationConnectionItem.SerialPortConfigInfo
                                        ?.DataFormat?.Value ?? 0),
                                    ModifyTime = DateTime.Now,
                                    Parity =
                                        (int)(model.CommunicationConnectionItem.SerialPortConfigInfo?.Parity.Value ??
                                              0),
                                    PortName = model.CommunicationConnectionItem.SerialPortConfigInfo?.PortName ??
                                               string.Empty,
                                    StopBits = (int)(model.CommunicationConnectionItem.SerialPortConfigInfo?.StopBits
                                        .Value ?? 0),
                                },
                                TcpConnectionConfigInfo = new TcpConnectionConfigInfoModel()
                                {
                                    ModifyTime = DateTime.Now,
                                    ConnectionMode = (int)(model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                        ?.ConnectionMode ?? 0),
                                    TcpConfigItems = new List<TcpConfigInfoModel>()
                                    {
                                        new() {
                                            IpAddress = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ServerParameter?.IpAddress ?? string.Empty,
                                            Port = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ServerParameter?.Port ?? 0,
                                            Type = 1
                                        },
                                        new() {
                                            IpAddress = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ClientParameter?.IpAddress ?? string.Empty,
                                            Port = model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                                ?.ClientParameter?.Port ?? 0,
                                            Type = 0
                                        }
                                    },
                                    DataFormat = (int)(model.CommunicationConnectionItem.TcpConnectionConfigInfo
                                    ?.DataFormat?.Value ?? 0)
                                },
                            });
                        CommunicationsSettingsMessageQueue.Enqueue(insertDetailAsync ? "保存成功" : "保存失败");
                        RefreshData();
                        EventAggregator.Instance.Publish(new SettingsChangedEvent
                        {
                            SettingsName = SettingsName,
                            IsLocallySaved = true
                        });
                    }
                }
            });
        }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand
        {
            get => new DelegateCommand<CommunicationConnectionItemInfoModel>(DeleteDelegate);
        }

        private async void DeleteDelegate(CommunicationConnectionItemInfoModel obj)
        {
            var communicationConnectionConfigInfoModel = await _communicationConnectionConfigRepository.
                FirstOrDefault(f =>
                    f.Id.Equals(obj.Id));
            if (communicationConnectionConfigInfoModel is not null)
            {
                var delete = await _communicationConnectionConfigRepository.Delete(communicationConnectionConfigInfoModel);
                if (delete)
                {
                    //刷新列表
                    RefreshData();
                    EventAggregator.Instance.Publish(new SettingsChangedEvent
                    {
                        SettingsName = SettingsName,
                        IsLocallySaved = true
                    });
                }
            }
        }

        private async void RefreshData()
        {
            var loadingDialog = new LoadingDialog();
            if (loadingDialog.DataContext is not LoadingDialogViewModel model) return;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                model.Identifier = "CommunicationsSettingsDialog";
                DialogHost.Show(loadingDialog, model.Identifier).ConfigureAwait(false);
            });
            var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0,
                o => o.Id);

            var models = await _communicationConnectionConfigRepository.
                CommunicationConnectionConfigItems(s => s.Id > 0);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CommunicationConnectionItems.Clear();
                var infoModels = models?.Select((s, i) => new CommunicationConnectionItemInfoModel
                {
                    ModifyCommand = ModifyCommand,
                    DeleteCommand = DeleteCommand,
                    CreateTime = s.CreateTime,
                    Id = s.Id,
                    ModifyTime = s.ModifyTime,
                    Num = i + 1,
                    Remarks = s.Remarks,
                    CommunicationProtocol = CommunicationProtocolItems.FirstOrDefault(f => f.Value.ToString().Equals(s.CommunicationProtocol)) ?? new CommunicationProtocolInfoModel(),
                    CommunicationType = CommunicationsTypeItems.FirstOrDefault(f => (int)f.Value == s.CommunicationType) ?? new CommunicationsTypeInfoModel(),
                    ConnectionName = s.ConnectionName,
                    DeviceExtensionConfigInfo = new DeviceExtensionConfigItemInfoModel()
                    {
                        CreateTime = s.CreateTime,
                        ModifyTime = s.ModifyTime,
                        MaxRetryCount = s.DeviceExtensionConfigInfo
                            ?.MaxRetryCount ?? 0,
                        ValidateDeviceResponse = s.DeviceExtensionConfigInfo
                            ?.ValidateDeviceResponse ?? false,
                    },
                    HeartbeatConfigInfo = new HeartbeatConfigItemInfoModel()
                    {
                        CreateTime = s.CreateTime,
                        ModifyTime = s.ModifyTime,
                        HeartbeatInterval = s.HeartbeatConfigInfo
                            ?.HeartbeatInterval ?? 0,
                        HeartbeatContent =
                           s.HeartbeatConfigInfo?.HeartbeatContent ??
                            string.Empty,
                        IsHeartbeatActive = s.HeartbeatConfigInfo
                            ?.IsHeartbeatActive ?? false,
                        IsHeartbeatEnabled = s.HeartbeatConfigInfo
                            ?.IsHeartbeatEnabled ?? false,
                        IsFixedHeartbeatContent = s.HeartbeatConfigInfo?.IsFixedHeartbeatContent ?? false
                    },
                    IsActive = s.IsActive,
                    IsAutoReconnect = s.IsAutoReconnect,
                    SerialPortConfigInfo = new SerialPortConfigItemInfoModel()
                    {
                        BaudRate = s.SerialPortConfigInfo?.BaudRate ?? 0,
                        CreateTime = s.CreateTime,
                        DataBits = s.SerialPortConfigInfo?.DataBits ?? 0,
                        DataFormat = DataFormatTypeItems.FirstOrDefault(f => (int)f.Value == s.SerialPortConfigInfo
                            .DataFormat) ?? new DataFormatTypeInfoModel(),
                        ModifyTime = DateTime.Now,
                        Parity = ParityItems.FirstOrDefault(f => (int)f.Value == s.SerialPortConfigInfo.Parity) ?? new ParityInfoModel(),

                        PortName = s.SerialPortConfigInfo?.PortName ??
                                   string.Empty,
                        StopBits = StopBitsItems.FirstOrDefault(f => (int)f.Value == s.SerialPortConfigInfo?.StopBits) ?? new StopBitsInfoModel(),
                    },
                    TcpConnectionConfigInfo = new TcpConnectionConfigItemInfoModel()
                    {
                        ConnectionMode = (TcpConnectionMode)(s.TcpConnectionConfigInfo
                            ?.ConnectionMode ?? 0),
                        ServerParameter = new TcpConfigItemInfoModel()
                        {
                            IpAddress = s.TcpConnectionConfigInfo?.TcpConfigItems?.FirstOrDefault(f => f.Type == 1)?.IpAddress ?? string.Empty,
                            Port = s.TcpConnectionConfigInfo?.TcpConfigItems?.FirstOrDefault(f => f.Type == 1)?.Port ?? 0,
                        },
                        ClientParameter = new TcpConfigItemInfoModel()
                        {
                            IpAddress = s.TcpConnectionConfigInfo?.TcpConfigItems?.FirstOrDefault(f => f.Type == 0)?.IpAddress ?? string.Empty,
                            Port = s.TcpConnectionConfigInfo?.TcpConfigItems?.FirstOrDefault(f => f.Type == 0)?.Port ?? 0,
                        },
                        DataFormat = DataFormatTypeItems.FirstOrDefault(f => (int)f.Value == s.TcpConnectionConfigInfo
                            .DataFormat) ?? new DataFormatTypeInfoModel(),
                    },
                    ConnectionCount = packageExitDefinitionInfoModels?.Where(w => w.CommunicationConnectionId.Equals(s.Id))?.Count() ?? 0
                })?.ToList();
                CommunicationConnectionItems.AddRange(infoModels);
                if (DialogHost.IsDialogOpen(model.Identifier))
                {
                    DialogHost.Close(model.Identifier);
                }
            });
        }
    }
}