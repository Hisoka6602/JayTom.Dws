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
using JayTom.Dws.Domain.Manager;
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
            new ApiTypeInfoModel()
            {
                Name = "筋斗云Wms",
                Value = ApiType.JdyWms
            },
            new ApiTypeInfoModel()
            {
                Name = "极兔Api接口",
                Value = ApiType.JtExpressApi
            },
            new ApiTypeInfoModel()
            {
                Name = "络道科技Api",
                Value = ApiType.RoutDataApi
            },
            new ApiTypeInfoModel()
            {
                Name = "Geek+",
                Value = ApiType.GeekPlusApi
            },
            new ApiTypeInfoModel()
            {
                Name = "菜鸟Api",
                Value = ApiType.CaiNiaoApi
            },
            new ApiTypeInfoModel()
            {
                Name = "海通智运Api",
                Value = ApiType.EshippingitApi
            },
            new ApiTypeInfoModel()
            {
                Name = "邮政处理中心Api",
                Value = ApiType.PostApi
            },
            new ApiTypeInfoModel()
            {
                Name = "邮政揽投部Api",
                Value = ApiType.PostInApi
            },
            new ApiTypeInfoModel()
            {
                Name = "拙燕仓Api",
                Value = ApiType.ZhuoYanScm
            },
            new ApiTypeInfoModel()
            {
                Name = "通天晓Api",
                Value = ApiType.TtxApi
            },
            new ApiTypeInfoModel()
            {
                Name = "旺店通+通天晓Api",
                Value = ApiType.WdtWmsApiAndTtxApi
            },
            new ApiTypeInfoModel()
            {
                Name = "聚水潭Erp",
                Value = ApiType.Jushuitan
            },
        };

        private ApiTypeInfoModel? _selectApiType = new();
        private SnackbarMessageQueue _apiSettingsMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;
        private bool _isUseLocalConfig;
        private IApiUploader<BaseApiParameters>? _apiUploader;

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

            SubmitApiInfoManager.ApiUploaderChanged += async (sender, uploader) => {
                _apiUploader = uploader;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    IsUseLocalConfig =
                        _apiUploader?.GetType()?.GetCustomAttribute<ApiClassAttribute>()?.UseLocalConfig ?? false;
                });
            };
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

        public bool IsUseLocalConfig {
            get => _isUseLocalConfig;
            set => SetProperty(ref _isUseLocalConfig, value);
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

            if (SelectApiType?.Value?.Equals("None", StringComparison.CurrentCultureIgnoreCase) == true) {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    IsUseLocalConfig = false;
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

        public ICommand OpenConfigFileCommand => new DelegateCommand<object>(OpenConfigFileDelegate);

        private void OpenConfigFileDelegate(object obj) {
            _apiUploader?.OpenJsonConfigFile();
        }
    }
}