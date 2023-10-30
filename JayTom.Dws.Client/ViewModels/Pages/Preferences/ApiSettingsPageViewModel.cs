using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Net.Http.Json;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class ApiSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;

        private ObservableCollection<ApiTypeInfoModel> _apiTypeItems = new()
        {
            new ApiTypeInfoModel()
            {
                Name = Languages.Language.ResourceManager.GetString("NoneApi")??string.Empty,
                Value = ApiType.None
            },
            new ApiTypeInfoModel()
            {
                Name = Languages.Language.ResourceManager.GetString("DefaultApi")??string.Empty,
                Value = ApiType.DefaultApi
            },
            new ApiTypeInfoModel()
            {
                Name = "SunnenApi",
                Value = ApiType.SunnenApi
            },
            new ApiTypeInfoModel()
            {
                Name = "旺店通WMS",
                Value = ApiType.WdtWmsApi
            },
            new ApiTypeInfoModel()
            {
                Name = "旺店通ERP旗舰版",
                Value = ApiType.WdtErpFlagShipApi
            },
            new ApiTypeInfoModel()
            {
                Name = "神州集运后台接口",
                Value = ApiType.SzjyApi
            },
        };

        private ApiTypeInfoModel? _selectApiType = new();
        private SnackbarMessageQueue _apiSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public ApiSettingsPageViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
        }

        public SnackbarMessageQueue ApiSettingsMessageQueue {
            get => _apiSettingsMessageQueue;
            set => SetProperty(ref _apiSettingsMessageQueue, value);
        }

        public ObservableCollection<ApiTypeInfoModel> ApiTypeItems {
            get => _apiTypeItems;
            set => SetProperty(ref _apiTypeItems, value);
        }

        public ApiTypeInfoModel? SelectApiType {
            get => _selectApiType;
            set => SetProperty(ref _selectApiType, value);
        }

        public ICommand OptionSelectionChangedCommand {
            get => new DelegateCommand<SelectionChangedEventArgs>(OptionSelectionChangedDelegate);
        }

        private async void OptionSelectionChangedDelegate(SelectionChangedEventArgs obj) {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = "ApiSettings",
                Value = JsonConvert.SerializeObject(new ApiSettingsDto() {
                    Type = SelectApiType?.Value ?? ApiType.None
                })
            });
            if (!insertOrUpdate) {
                SelectApiType = ApiTypeItems.FirstOrDefault(f => f.Value == ApiType.None);
                ApiSettingsMessageQueue.Enqueue($"{Languages.Language.ResourceManager.GetString("切换Api接口失败") ?? string.Empty}");
            }
            else {
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = "ApiSettings"
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
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("ApiSettings"));
                    if (configInfoModel is not null) {
                        var settingsDto = JsonConvert.DeserializeObject<ApiSettingsDto>(configInfoModel.Value);
                        if (settingsDto is not null) {
                            SelectApiType = ApiTypeItems.FirstOrDefault(f => f.Value == settingsDto.Type);
                        }
                    }
                });
            }
        }
    }
}