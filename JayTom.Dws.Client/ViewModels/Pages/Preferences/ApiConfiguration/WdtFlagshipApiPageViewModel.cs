using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Net.Http;
using Newtonsoft.Json;
using TouchSocket.Core;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Interface.Szjy188;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class WdtFlagshipApiPageViewModel : BindableBase {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDialogService _dialogService;
        private readonly IConfigRepository _configRepository;
        private WdtFlagshipApiInfoModel _wdtFlagshipApiInfo = new();
        private string _barcode = string.Empty;
        private double _weight;
        private bool _isUploading;
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _wdtFlagshipApiMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoaded;

        public WdtFlagshipApiPageViewModel(IHttpClientFactory httpClientFactory,
            IDialogService dialogService,
            IConfigRepository configRepository) {
            _httpClientFactory = httpClientFactory;
            _dialogService = dialogService;
            _configRepository = configRepository;
        }

        public WdtFlagshipApiInfoModel WdtFlagshipApiInfo {
            get => _wdtFlagshipApiInfo;
            set => SetProperty(ref _wdtFlagshipApiInfo, value);
        }

        public SnackbarMessageQueue WdtFlagshipApiMessageQueue {
            get => _wdtFlagshipApiMessageQueue;
            set => SetProperty(ref _wdtFlagshipApiMessageQueue, value);
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
                        ConfigName = "WdtFlagshipApiParameters",
                        Value = JsonConvert.SerializeObject(new WdtFlagshipApiDto() {
                            Key = WdtFlagshipApiInfo.Key,
                            Appsecret = WdtFlagshipApiInfo.Appsecret,
                            Sid = WdtFlagshipApiInfo.Sid,
                            Method = WdtFlagshipApiInfo.Method,
                            V = WdtFlagshipApiInfo.V,
                            Salt = WdtFlagshipApiInfo.Salt,
                            PackagerId = WdtFlagshipApiInfo.PackagerId,
                            OperateTableName = WdtFlagshipApiInfo.OperateTableName,
                            Force = WdtFlagshipApiInfo.Force,
                            Url = WdtFlagshipApiInfo.Url,
                            TimeOut = WdtFlagshipApiInfo.TimeOut,
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "WdtFlagshipApiParameters"
                        });
                    }
                    IsSavingInProgress = false;
                    WdtFlagshipApiMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
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
                    var configInfoModel = await _configRepository.FirstOrDefault(w => w.ConfigName.Equals("WdtFlagshipApiParameters"));
                    if (configInfoModel is not null) {
                        var settingsDto = JsonConvert.DeserializeObject<WdtFlagshipApiDto>(configInfoModel.Value);
                        if (settingsDto is not null) {
                            WdtFlagshipApiInfo = new WdtFlagshipApiInfoModel() {
                                Url = settingsDto.Url,
                                Key = settingsDto.Key,
                                Appsecret = settingsDto.Appsecret,
                                Sid = settingsDto.Sid,
                                Method = settingsDto.Method,
                                V = settingsDto.V,
                                Salt = settingsDto.Salt,
                                PackagerId = settingsDto.PackagerId,
                                OperateTableName = settingsDto.OperateTableName,
                                Force = settingsDto.Force,
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
                    var wdtFlagshipApi = new WdtFlagshipApi(_httpClientFactory);
                    await wdtFlagshipApi.SetParameters(new WdtFlagshipApi.ApiParameter {
                        Key = WdtFlagshipApiInfo.Key,
                        Appsecret = WdtFlagshipApiInfo.Appsecret,
                        Sid = WdtFlagshipApiInfo.Sid,
                        Method = WdtFlagshipApiInfo.Method,
                        V = WdtFlagshipApiInfo.V,
                        Salt = WdtFlagshipApiInfo.Salt,
                        PackagerId = WdtFlagshipApiInfo.PackagerId,
                        OperateTableName = WdtFlagshipApiInfo.OperateTableName,
                        Force = WdtFlagshipApiInfo.Force,
                        Url = WdtFlagshipApiInfo.Url,
                        TimeOut = WdtFlagshipApiInfo.TimeOut,
                    });
                    var uploadResponse = await wdtFlagshipApi.UploadData(Barcode, Weight);
                    IsUploading = false;
                    //弹窗
                    _dialogService.ShowDialog("ApiTestDialog", new DialogParameters { { "UploadResponse", uploadResponse } }, null);
                });
            }
        }
    }
}