using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Client.Models.ContentInputSettingsModels;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {
    public class ContentInputSettingsPageViewModel : SettingsPageTemplateViewModel {
        private bool _isUseTcpInput;
        private bool _isUseControlInput;
        private ControlInputInfoModel _controlInputInfo = new();
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private ObservableCollection<ItemBaseTemplateModel> _dataTemplate = new();
        private string _separator = string.Empty;
        private bool _isUseBarcodeScannerInput;
        private bool _isUseRegularFilter;

        public ContentInputSettingsPageViewModel(IConfigRepository configRepository) : base(configRepository) {
        }

        /// <summary>
        /// Json示例
        /// </summary>
        public string ExampleJson => JsonConvert.SerializeObject(new {
            barcode = "123456",
            weight = 10.1,
            length = 5.1,
            width = 4.1,
            height = 3.1,
            volume = 2.1
        }, Formatting.Indented);

        /// <summary>
        /// 是否使用Tcp输入
        /// </summary>
        public bool IsUseTcpInput {
            get => _isUseTcpInput;
            set => SetProperty(ref _isUseTcpInput, value);
        }

        /// <summary>
        /// 是否使用控件输入
        /// </summary>
        public bool IsUseControlInput {
            get => _isUseControlInput;
            set => SetProperty(ref _isUseControlInput, value);
        }

        /// <summary>
        /// 是否使用扫码枪输入
        /// </summary>
        public bool IsUseBarcodeScannerInput {
            get => _isUseBarcodeScannerInput;
            set => SetProperty(ref _isUseBarcodeScannerInput, value);
        }

        /// <summary>
        /// 是否使用常规过滤
        /// </summary>
        public bool IsUseRegularFilter {
            get => _isUseRegularFilter;
            set => SetProperty(ref _isUseRegularFilter, value);
        }

        public ObservableCollection<ItemBaseTemplateModel> DataTemplate {
            get => _dataTemplate;
            set => SetProperty(ref _dataTemplate, value);
        }

        public string Separator {
            get => _separator;
            set => SetProperty(ref _separator, value);
        }

        /// <summary>
        /// 控件输入设置
        /// </summary>
        public ControlInputInfoModel ControlInputInfo {
            get => _controlInputInfo;
            set => SetProperty(ref _controlInputInfo, value);
        }

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfoModel TcpSettingsInfo {
            get => _tcpSettingsInfo;
            set => SetProperty(ref _tcpSettingsInfo, value);
        }

        public override string Identifier => "ContentInputSettingsDialogHost";
        public override string SettingsName => "ContentInputSettings";

        /// <summary>
        /// 添加输入Item
        /// </summary>
        public ICommand AddInputItemCommand => new DelegateCommand<string>(AddInputItemDelegate);

        private async void AddInputItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                DataTemplate.Add(new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = DataTemplate.Count,
                    Type = 1,
                    ApplicationType = ItemApplicationType.DataInput
                });
            });
        }

        /// <summary>
        /// 添加分隔符
        /// </summary>
        public ICommand AddSeparatorItemCommand => new DelegateCommand<string>(AddSeparatorItemDelegate);

        private async void AddSeparatorItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                DataTemplate.Add(new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = DataTemplate.Count,
                    Type = 2,
                    ApplicationType = ItemApplicationType.DataInput
                });
            });
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (model.ApplicationType == ItemApplicationType.DataInput) {
                    DataTemplate.Remove(model);
                    foreach (var item in DataTemplate) {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            DataTemplate.LastOrDefault() != item) {
                            DataTemplate.Remove(item);
                        }
                    }
                }
            });
        }

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new ContentInputSettingsDto {
                    IsUseControlInput = IsUseControlInput,
                    IsUseTcpInput = IsUseTcpInput,
                    IsUseBarcodeScannerInput = IsUseBarcodeScannerInput,
                    IsUseRegularFilter = IsUseRegularFilter,
                    DataTemplate = DataTemplate.Select(s => new ItemTemplateInfo() {
                        ApplicationType = s.ApplicationType,
                        Content = s.Content,
                        Type = s.Type
                    })?.ToList() ?? new List<ItemTemplateInfo>(),
                    ControlInputInfo = new ControlInputInfo() {
                        IsReceiveBarcode = ControlInputInfo.IsReceiveBarcode,
                        IsReceiveHeight = ControlInputInfo.IsReceiveHeight,
                        IsReceiveLength = ControlInputInfo.IsReceiveLength,
                        IsReceiveVolume = ControlInputInfo.IsReceiveVolume,
                        IsReceiveWeight = ControlInputInfo.IsReceiveWeight,
                        IsReceiveWidth = ControlInputInfo.IsReceiveWidth,
                    },
                    TcpSettingsInfo = new TcpSettingsInfo() {
                        ConnectionMode = TcpSettingsInfo.ConnectionMode,
                        ClientConfig = new TcpInfo() {
                            IpAddress = TcpSettingsInfo.ClientConfig.IpAddress,
                            Port = TcpSettingsInfo.ClientConfig.Port,
                        },
                        ServerConfig = new TcpInfo() {
                            IpAddress = TcpSettingsInfo.ServerConfig.IpAddress,
                            Port = TcpSettingsInfo.ServerConfig.Port,
                        }
                    },
                    Separator = Separator
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            var settingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>(SettingsName) ?? new ContentInputSettingsDto();
            IsUseTcpInput = settingsDto.IsUseTcpInput;
            IsUseControlInput = settingsDto.IsUseControlInput;
            IsUseBarcodeScannerInput = settingsDto.IsUseBarcodeScannerInput;
            IsUseRegularFilter = settingsDto.IsUseRegularFilter;
            ControlInputInfo = new ControlInputInfoModel {
                IsReceiveBarcode = settingsDto.ControlInputInfo.IsReceiveBarcode,
                IsReceiveHeight = settingsDto.ControlInputInfo.IsReceiveHeight,
                IsReceiveLength = settingsDto.ControlInputInfo.IsReceiveLength,
                IsReceiveVolume = settingsDto.ControlInputInfo.IsReceiveVolume,
                IsReceiveWeight = settingsDto.ControlInputInfo.IsReceiveWeight,
                IsReceiveWidth = settingsDto.ControlInputInfo.IsReceiveWidth
            };
            TcpSettingsInfo = new TcpSettingsInfoModel {
                ConnectionMode = settingsDto.TcpSettingsInfo.ConnectionMode,
                ClientConfig = new TcpInfoModel() {
                    IpAddress = settingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                    Port = settingsDto.TcpSettingsInfo.ClientConfig.Port
                },
                ServerConfig = new TcpInfoModel() {
                    IpAddress = settingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                    Port = settingsDto.TcpSettingsInfo.ServerConfig.Port
                }
            };
            Separator = settingsDto.Separator;
            var templateModels = settingsDto.DataTemplate.Select(s => new ItemBaseTemplateModel() {
                ApplicationType = s.ApplicationType,
                Content = s.Content,
                Type = s.Type
            })?.ToList();
            DataTemplate.Clear();
            DataTemplate.AddRange(templateModels);
        }
    }
}