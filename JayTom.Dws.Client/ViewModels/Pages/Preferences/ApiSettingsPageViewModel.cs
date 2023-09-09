using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {
    public class ApiSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;

        private ObservableCollection<ApiTypeInfoModel> _apiTypeItems = new()
        {
            new ApiTypeInfoModel()
            {
                Name = "不上传",
                Value = ApiType.None
            },
            new ApiTypeInfoModel()
            {
                Name = "基础接口",
                Value = ApiType.DefaultApi
            },
        };

        private ApiTypeInfoModel? _selectApiType = new();
        private SnackbarMessageQueue _apiSettingsMessageQueue = new(TimeSpan.FromSeconds(2));

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
                ApiSettingsMessageQueue.Enqueue("切换Api接口失败!");
            }
            else {
                EventAggregator.Instance.Publish(new SettingsChangedEvent {
                    SettingsName = "ApiSettings"
                });
            }

        }
    }
}