using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.IO.Ports;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Client.Models;
using JayTom.Dws.Data.LocalLog;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Converters;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.VolumeSettingsModel;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Infrastructure.Repository.LocalConf;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class VolumeSettingsViewModel : SettingsPageTemplateViewModel {
        private VolumeSettingsInfoModel _volumeSettingsInfo = new();
        private ParityInfoModel _selectParity = new();
        private StopBitsInfoModel _selectStopBits = new();
        private DataFormatTypeInfoModel _selectDataFormat = new();
        private DataFormatTypeInfoModel _sendDataFormat = new();
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

        private ObservableCollection<VolumeTriggerPositionModel> _volumeTriggerPositionItems = new()
        {
            new VolumeTriggerPositionModel()
            {
                Name = Languages.Language.ResourceManager.GetString("AfterScanning")??string.Empty,
                Value = VolumeTriggerPosition.BarcodeDetected,
            },
            new VolumeTriggerPositionModel()
            {
                Name = Languages.Language.ResourceManager.GetString("AfterWeighing")??string.Empty,
                Value = VolumeTriggerPosition.WeightObtained,
            },
        };

        private VolumeTriggerPositionModel _selectTriggerPosition = new();
        private bool _isLoaded;

        private ObservableCollection<VolumeUnitInfoModel> _volumeUnitInfoItem = new()
        {
            new VolumeUnitInfoModel()
            {
                Name = "mm",
                Value = VolumeUnit.Millimeter
            },
            new VolumeUnitInfoModel()
            {
                Name = "cm",
                Value = VolumeUnit.Centimeter
            },
            new VolumeUnitInfoModel()
            {
                Name = "m",
                Value = VolumeUnit.Meter
            },
        };

        private VolumeUnitInfoModel _selectVolumeUnitInfo = new() {
            Name = "mm",
            Value = VolumeUnit.Millimeter
        };

        public VolumeSettingsViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        public VolumeSettingsInfoModel VolumeSettingsInfo {
            get => _volumeSettingsInfo;
            set => SetProperty(ref _volumeSettingsInfo, value);
        }

        public DataFormatTypeInfoModel SelectDataFormat {
            get => _selectDataFormat;
            set => SetProperty(ref _selectDataFormat, value);
        }

        /// <summary>
        /// 发送格式
        /// </summary>
        public DataFormatTypeInfoModel SendDataFormat {
            get => _sendDataFormat;
            set => SetProperty(ref _sendDataFormat, value);
        }

        public ObservableCollection<VolumeUnitInfoModel> VolumeUnitInfoItem {
            get => _volumeUnitInfoItem;
            set => SetProperty(ref _volumeUnitInfoItem, value);
        }

        public VolumeUnitInfoModel SelectVolumeUnitInfo {
            get => _selectVolumeUnitInfo;
            set => SetProperty(ref _selectVolumeUnitInfo, value);
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
        /// 效验位
        /// </summary>
        public ParityInfoModel SelectParity {
            get => _selectParity;
            set => SetProperty(ref _selectParity, value);
        }

        /// <summary>
        /// 停止位下拉选项
        /// </summary>
        public ObservableCollection<StopBitsInfoModel> StopBitsItems {
            get => _stopBitsItems;
            set => SetProperty(ref _stopBitsItems, value);
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBitsInfoModel SelectStopBits {
            get => _selectStopBits;
            set => SetProperty(ref _selectStopBits, value);
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
        /// 数据格式
        /// </summary>
        public ObservableCollection<DataFormatTypeInfoModel> DataFormatTypeItems {
            get => _dataFormatTypeItems;
            set => SetProperty(ref _dataFormatTypeItems, value);
        }

        /// <summary>
        /// 触发体积类型
        /// </summary>
        public ObservableCollection<VolumeTriggerPositionModel> VolumeTriggerPositionItems {
            get => _volumeTriggerPositionItems;
            set => SetProperty(ref _volumeTriggerPositionItems, value);
        }

        public VolumeTriggerPositionModel SelectTriggerPosition {
            get => _selectTriggerPosition;
            set => SetProperty(ref _selectTriggerPosition, value);
        }

        /// <summary>
        /// 串口刷新
        /// </summary>
        public ICommand PortUpdateCommand {
            get => new DelegateCommand(PortUpdateDelegate);
        }

        private async void PortUpdateDelegate() {
            //重新枚举串口
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                PortItems.Clear();
                PortItems.AddRange(SerialPort.GetPortNames());
            });
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand {
            get => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);
        }

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (model.ApplicationType == ItemApplicationType.VolumeInput) {
                    VolumeSettingsInfo.DataTemplate.Remove(model);
                    foreach (var item in VolumeSettingsInfo.DataTemplate) {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            VolumeSettingsInfo.DataTemplate.LastOrDefault() != item) {
                            VolumeSettingsInfo.DataTemplate.Remove(item);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 添加数据模板
        /// </summary>
        public ICommand AddOutputItemCommand {
            get => new DelegateCommand<string>(AddOutputItemDelegate);
        }

        private async void AddOutputItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                VolumeSettingsInfo.DataTemplate.Add(new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = VolumeSettingsInfo.DataTemplate.Count,
                    Type = 1,
                    ApplicationType = ItemApplicationType.VolumeInput
                });
            });
        }

        /// <summary>
        /// 添加数据模板
        /// </summary>
        public ICommand AddSeparatorItemCommand {
            get => new DelegateCommand<string>(AddSeparatorItemDelegate);
        }

        private async void AddSeparatorItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                VolumeSettingsInfo.DataTemplate.Add(new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = VolumeSettingsInfo.DataTemplate.Count,
                    Type = 2,
                    ApplicationType = ItemApplicationType.VolumeInput
                });
            });
        }

        public override string Identifier => "VolumeSettingsDialogHost";
        public override string SettingsName => "VolumeSettings";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new VolumeSettingsDto {
                    Unit = SelectVolumeUnitInfo.Value,
                    DataTemplate = VolumeSettingsInfo.DataTemplate.Select(s => new ItemTemplateInfo() {
                        ApplicationType = s.ApplicationType,
                        Content = s.Content,
                        Type = s.Type
                    })?.ToList() ?? new List<ItemTemplateInfo>(),
                    Separator = VolumeSettingsInfo.Separator,
                    IsUseExternalVolumeInput = VolumeSettingsInfo.IsUseExternalVolumeInput,
                    IsTriggerVolumeRequest = VolumeSettingsInfo.IsTriggerVolumeRequest,
                    IsUseFusionTimeout = VolumeSettingsInfo.IsUseFusionTimeout,
                    FusionTimeout = VolumeSettingsInfo.FusionTimeout,
                    VolumeInformationRequesterInfo = new VolumeInformationRequesterInfo() {
                        VolumeTriggerPosition = SelectTriggerPosition.Value,
                        SendContent = VolumeSettingsInfo.VolumeInformationRequesterInfo.SendContent,
                        SendDelay = VolumeSettingsInfo.VolumeInformationRequesterInfo.SendDelay,
                        SendCount = VolumeSettingsInfo.VolumeInformationRequesterInfo.SendCount,
                        SendInterval = VolumeSettingsInfo.VolumeInformationRequesterInfo.SendInterval,
                        VolumeRequesterType = VolumeSettingsInfo.VolumeInformationRequesterInfo.VolumeRequesterType,
                        TcpSettingsInfo = new TcpSettingsInfo() {
                            ConnectionMode = VolumeSettingsInfo.VolumeInformationRequesterInfo.TcpSettingsInfo.ConnectionMode,
                            ServerConfig = new TcpInfo() {
                                IpAddress = VolumeSettingsInfo.VolumeInformationRequesterInfo.TcpSettingsInfo.ServerConfig.IpAddress,
                                Port = VolumeSettingsInfo.VolumeInformationRequesterInfo.TcpSettingsInfo.ServerConfig.Port,
                            },
                            ClientConfig = new TcpInfo() {
                                IpAddress = VolumeSettingsInfo.VolumeInformationRequesterInfo.TcpSettingsInfo.ClientConfig.IpAddress,
                                Port = VolumeSettingsInfo.VolumeInformationRequesterInfo.TcpSettingsInfo.ClientConfig.Port,
                            }
                        },
                        SerialPortSettingsInfo = new SerialPortSettingsInfo() {
                            BaudRate = VolumeSettingsInfo.VolumeInformationRequesterInfo.SerialPortSettingsInfo.BaudRate,
                            DataBits = VolumeSettingsInfo.VolumeInformationRequesterInfo.SerialPortSettingsInfo.DataBits,
                            DataFormat = SelectDataFormat.Value,
                            Parity = SelectParity.Value,
                            PortName = VolumeSettingsInfo.VolumeInformationRequesterInfo.SerialPortSettingsInfo.PortName,
                            StopBits = SelectStopBits.Value,
                        }
                    }
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    PortItems.Clear();
                    PortItems.AddRange(SerialPort.GetPortNames());
                    var settingsDto = await _configRepository.FirstOrDefaultEntity<VolumeSettingsDto>(SettingsName) ??
                                      new VolumeSettingsDto();
                    var templateModels = settingsDto.DataTemplate.Select(s => new ItemBaseTemplateModel() {
                        ApplicationType = s.ApplicationType,
                        Content = s.Content,
                        Type = s.Type
                    })?.ToList();

                    VolumeSettingsInfo = new VolumeSettingsInfoModel() {
                        Unit = settingsDto.Unit,
                        Separator = settingsDto.Separator,
                        IsUseExternalVolumeInput = settingsDto.IsUseExternalVolumeInput,
                        IsTriggerVolumeRequest = settingsDto.IsTriggerVolumeRequest,
                        FusionTimeout = settingsDto.FusionTimeout,
                        IsUseFusionTimeout = settingsDto.IsUseFusionTimeout,
                        VolumeInformationRequesterInfo = new VolumeInformationRequesterInfoModel() {
                            VolumeTriggerPosition = settingsDto.VolumeInformationRequesterInfo.VolumeTriggerPosition,
                            SendContent = settingsDto.VolumeInformationRequesterInfo.SendContent,
                            SendDelay = settingsDto.VolumeInformationRequesterInfo.SendDelay,
                            SendCount = settingsDto.VolumeInformationRequesterInfo.SendCount,
                            SendInterval = settingsDto.VolumeInformationRequesterInfo.SendInterval,
                            VolumeRequesterType = settingsDto.VolumeInformationRequesterInfo.VolumeRequesterType,
                            TcpSettingsInfo = new TcpSettingsInfo() {
                                ConnectionMode = settingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo
                                    .ConnectionMode,
                                ServerConfig = new TcpInfo() {
                                    IpAddress = settingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo
                                        .ServerConfig.IpAddress,
                                    Port = settingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo
                                        .ServerConfig.Port,
                                },
                                ClientConfig = new TcpInfo() {
                                    IpAddress = settingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo
                                        .ClientConfig.IpAddress,
                                    Port = settingsDto.VolumeInformationRequesterInfo.TcpSettingsInfo
                                        .ClientConfig.Port,
                                }
                            },
                            SerialPortSettingsInfo = new SerialPortSettingsInfoModel() {
                                BaudRate = settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                                    .BaudRate,
                                DataBits = settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                                    .DataBits,
                                DataFormat = settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                                    .DataFormat,
                                Parity = SelectParity.Value,
                                PortName = settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                                    .PortName,
                                StopBits = settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                                    .StopBits,
                            }
                        }
                    };
                    SelectTriggerPosition = VolumeTriggerPositionItems.FirstOrDefault(f =>
                        f.Value.Equals(settingsDto.VolumeInformationRequesterInfo.VolumeTriggerPosition)) ?? new VolumeTriggerPositionModel();
                    SelectParity = ParityItems.FirstOrDefault(f =>
                        f.Value.Equals(settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                            .Parity)) ?? new ParityInfoModel();
                    SelectDataFormat = DataFormatTypeItems.FirstOrDefault(f =>
                        f.Value.Equals(settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                            .DataFormat)) ?? new DataFormatTypeInfoModel();
                    SelectStopBits = StopBitsItems.FirstOrDefault(f =>
                        f.Value.Equals(settingsDto.VolumeInformationRequesterInfo.SerialPortSettingsInfo
                            .StopBits)) ?? new StopBitsInfoModel();
                    SelectVolumeUnitInfo =
                        VolumeUnitInfoItem.FirstOrDefault(f => f.Value.Equals(settingsDto.Unit)) ??
                        new VolumeUnitInfoModel();
                    VolumeSettingsInfo.DataTemplate.AddRange(templateModels);
                });
            }
        }
    }
}