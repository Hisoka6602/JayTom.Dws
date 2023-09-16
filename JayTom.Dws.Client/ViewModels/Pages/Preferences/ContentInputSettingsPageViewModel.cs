using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Client.Models.ContentInputSettingsModels;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class ContentInputSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private bool _isUseTcpInput;
        private bool _isUseControlInput;
        private ControlInputInfoModel _controlInputInfo = new();
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private SnackbarMessageQueue _contentInputSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isSavingInProgress;
        private bool _isLoaded;

        public ContentInputSettingsPageViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        public SnackbarMessageQueue ContentInputSettingsMessageQueue {
            get => _contentInputSettingsMessageQueue;
            set => SetProperty(ref _contentInputSettingsMessageQueue, value);
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

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "ContentInputSettings",
                        Value = JsonConvert.SerializeObject(new ContentInputSettingsDto {
                            IsUseControlInput = IsUseControlInput,
                            IsUseTcpInput = IsUseTcpInput,
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
                            }
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "ContentInputSettings"
                        });
                    }
                    IsSavingInProgress = false;
                    ContentInputSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("Save") ?? string.Empty}{(insertOrUpdate ?
                        Languages.Language.ResourceManager.GetString("Success") : Languages.Language.ResourceManager.GetString("Failure"))}");
                });
            }
        }

        /// <summary>
        /// 页面加载完成
        /// </summary>
        public ICommand LoadedCommand {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("ContentInputSettings"));
                    if (configInfoModel is not null) {
                        try {
                            var settingsDto = JsonConvert.DeserializeObject<ContentInputSettingsDto>(configInfoModel.Value);
                            if (settingsDto is not null) {
                                IsUseTcpInput = settingsDto.IsUseTcpInput;
                                IsUseControlInput = settingsDto.IsUseControlInput;
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
                            }
                        }
                        catch (Exception e) {
                            ContentInputSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("加载设置失败") ?? string.Empty,}:{e.Message}");
                        }
                    }
                });
            }
        }
    }
}