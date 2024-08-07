using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Reflection;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Interface;
using System.Collections.ObjectModel;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Domain.Interface.Attributes;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class ApiSettingsPageViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;

        private ObservableCollection<ApiTypeInfoModel> _apiTypeItems = new();

        private ApiTypeInfoModel? _selectApiType = new();
        private SnackbarMessageQueue _apiSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public ApiSettingsPageViewModel(IConfigRepository configRepository) {
            _configRepository = configRepository;
            //遍历接口
            ApiTypeItems.Clear();
            ApiTypeItems.Add(new ApiTypeInfoModel() {
                Name = "不使用接口上传",
                Value = "None"
            });
            var interfaceType = typeof(IApiUploader<BaseApiParameters>);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => interfaceType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
                .ToList();

            var apiTypeInfoModels = types.Select(s => new ApiTypeInfoModel {
                Name = $"{s.GetCustomAttribute<ApiClassAttribute>()?.DisplayName ?? string.Empty}({s.GetCustomAttribute<ApiClassAttribute>()?.Version ?? string.Empty})",
                Value = s.GetCustomAttribute<ApiClassAttribute>()?.Name ?? string.Empty
            })?.ToList() ?? new List<ApiTypeInfoModel>();
            ApiTypeItems.AddRange(apiTypeInfoModels);
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

        public ICommand OptionSelectionChangedCommand => new DelegateCommand<SelectionChangedEventArgs>(OptionSelectionChangedDelegate);

        private async void OptionSelectionChangedDelegate(SelectionChangedEventArgs obj) {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = "ApiSettings",
                Value = JsonConvert.SerializeObject(new ApiSettingsDto() {
                    ApiName = SelectApiType?.Value ?? string.Empty,
                })
            });
            if (!insertOrUpdate) {
                SelectApiType = ApiTypeItems.FirstOrDefault(f => f.Value == string.Empty);
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
        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj) {
            if (!_isLoaded) {
                _isLoaded = true;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var settingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>("ApiSettings") ?? new ApiSettingsDto();
                    SelectApiType = ApiTypeItems.FirstOrDefault(f => f.Value == settingsDto.ApiName);
                });
            }
        }
    }
}