using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class WdtWmsApiPageViewModel : BindableBase {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDialogService _dialogService;
        private readonly IConfigRepository _configRepository;
        private WdtWmsApiInfo _wdtWmsApiInfo = new();
        private bool _isLoaded;
        private SnackbarMessageQueue _wdtWmsApiMessageQueue = new(TimeSpan.FromSeconds(2));
        private string _barcode = string.Empty;
        private double _weight;
        private bool _isUploading;
        private bool _isSavingInProgress;

        public WdtWmsApiPageViewModel(IHttpClientFactory httpClientFactory,
            IDialogService dialogService,
            IConfigRepository configRepository) {
            _httpClientFactory = httpClientFactory;
            _dialogService = dialogService;
            _configRepository = configRepository;
        }

        public WdtWmsApiInfo WdtWmsApiInfo {
            get => _wdtWmsApiInfo;
            set => SetProperty(ref _wdtWmsApiInfo, value);
        }

        public SnackbarMessageQueue WdtWmsApiMessageQueue {
            get => _wdtWmsApiMessageQueue;
            set => SetProperty(ref _wdtWmsApiMessageQueue, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        public double Weight {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 上传中
        /// </summary>
        public bool IsUploading {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        /// <summary>
        /// 是否保存中
        /// </summary>
        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
        }

        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "WdtWmsApiParameters",
                        Value = JsonConvert.SerializeObject(new WdtWmsApiDto() {
                            AppKey = WdtWmsApiInfo.AppKey,
                            AppSecret = WdtWmsApiInfo.AppSecret,
                            Sid = WdtWmsApiInfo.Sid,
                            Method = WdtWmsApiInfo.Method,
                            Url = WdtWmsApiInfo.Url,
                            TimeOut = WdtWmsApiInfo.TimeOut,
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "WdtWmsApiParameters"
                        });
                    }
                    IsSavingInProgress = false;
                    WdtWmsApiMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                        Languages.Language.ResourceManager.GetString("SaveFailed"))}");
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
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("WdtWmsApiParameters"));
                    if (configInfoModel is not null) {
                        var settingsDto = JsonConvert.DeserializeObject<WdtWmsApiDto>(configInfoModel.Value);
                        if (settingsDto is not null) {
                            WdtWmsApiInfo = new WdtWmsApiInfo() {
                                Url = settingsDto.Url,
                                AppKey = settingsDto.AppKey,
                                AppSecret = settingsDto.AppSecret,
                                Sid = settingsDto.Sid,
                                Method = settingsDto.Method,
                                TimeOut = settingsDto.TimeOut,
                            };
                        }
                    }
                });
            }
        }

        public ICommand UploadCommand {
            get => new DelegateCommand<object>(UploadDelegate);
        }

        private async void UploadDelegate(object obj) {
            if (!IsUploading) {
                IsUploading = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    //上传
                    var wdtWmsApi = new WdtWmsApi(_httpClientFactory);
                    await wdtWmsApi.SetParameters(new WdtWmsApi.ApiParameter {
                        AppKey = WdtWmsApiInfo.AppKey,
                        AppSecret = WdtWmsApiInfo.AppSecret,
                        Sid = WdtWmsApiInfo.Sid,
                        Method = WdtWmsApiInfo.Method,
                        Url = WdtWmsApiInfo.Url,
                        TimeOut = WdtWmsApiInfo.TimeOut,
                    });
                    var uploadResponse = await wdtWmsApi.UploadData(Barcode, Weight);
                    IsUploading = false;
                    //弹窗
                    _dialogService.ShowDialog("ApiTestDialog", new DialogParameters { { "UploadResponse", uploadResponse } }, null);
                });
            }
        }
    }
}