using System;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Prism.Services.Dialogs;
using JayTom.Dws.Interface.Wdt;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Interface.Jushuitan;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class JushuitanApiPageViewModel : SettingsPageTemplateViewModel {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDialogService _dialogService;
        private JushuitanErpApiModel _jushuitanErpApiInfo = new();
        private bool _isRefreshing = false;
        private bool _isUploading;
        private string _barcode = string.Empty;
        private double _weight;

        public JushuitanApiPageViewModel(IConfigRepository configRepository,
            IHttpClientFactory httpClientFactory, IDialogService dialogService) : base(configRepository) {
            _httpClientFactory = httpClientFactory;
            _dialogService = dialogService;
        }

        public JushuitanErpApiModel JushuitanErpApiInfo {
            get => _jushuitanErpApiInfo;
            set => SetProperty(ref _jushuitanErpApiInfo, value);
        }

        public bool IsRefreshing {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public bool IsUploading {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public string Barcode {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        public double Weight {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        public override async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var settingsDto = await _configRepository.FirstOrDefaultEntity<JushuitanErpApiDto>(SettingsName) ?? new JushuitanErpApiDto();
                JushuitanErpApiInfo = new JushuitanErpApiModel() {
                    AppKey = settingsDto.AppKey,
                    AccessToken = settingsDto.AccessToken,
                    AppSecret = settingsDto.AppSecret,
                    IsUnLid = settingsDto.IsUnLid,
                    IsUploadWeight = settingsDto.IsUploadWeight,
                    Type = settingsDto.Type,
                    Channel = settingsDto.Channel,
                    TimeOut = settingsDto.TimeOut,
                    Url = settingsDto.Url,
                    Version = settingsDto.Version,
                    TokenExpireTime = settingsDto.TokenExpireTime,
                    LastTokenUpdateTime = settingsDto.LastTokenUpdateTime,
                };
            });
        }

        public override string Identifier => "JushuitanErpApiParametersDialogHost";
        public override string SettingsName => "JushuitanErpApiParameters";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new JushuitanErpApiDto() {
                    AppKey = JushuitanErpApiInfo.AppKey,
                    AccessToken = JushuitanErpApiInfo.AccessToken,
                    AppSecret = JushuitanErpApiInfo.AppSecret,
                    IsUnLid = JushuitanErpApiInfo.IsUnLid,
                    IsUploadWeight = JushuitanErpApiInfo.IsUploadWeight,
                    Type = JushuitanErpApiInfo.Type,
                    Channel = JushuitanErpApiInfo.Channel,
                    TimeOut = JushuitanErpApiInfo.TimeOut,
                    Url = JushuitanErpApiInfo.Url,
                    Version = JushuitanErpApiInfo.Version,
                    TokenExpireTime = JushuitanErpApiInfo.TokenExpireTime ?? DateTime.MinValue,
                    LastTokenUpdateTime = JushuitanErpApiInfo.LastTokenUpdateTime ?? DateTime.MinValue,
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public ICommand RefreshAccessTokenCommand => new DelegateCommand<object>(RefreshAccessTokenDelegate);

        private async void RefreshAccessTokenDelegate(object obj) {
            //刷新Token,并保存
            if (!IsRefreshing) {
                IsRefreshing = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    var jushuitanErpApi = new JushuitanErpApi(_httpClientFactory);
                    await jushuitanErpApi.SetParameters(new JushuitanErpApi.ApiParameters() {
                        AppKey = JushuitanErpApiInfo.AppKey,
                        AccessToken = JushuitanErpApiInfo.AccessToken,
                        AppSecret = JushuitanErpApiInfo.AppSecret,
                        IsUnLid = JushuitanErpApiInfo.IsUnLid,
                        IsUploadWeight = JushuitanErpApiInfo.IsUploadWeight,
                        Type = JushuitanErpApiInfo.Type,
                        Channel = JushuitanErpApiInfo.Channel,
                        TimeOut = JushuitanErpApiInfo.TimeOut,
                        Url = JushuitanErpApiInfo.Url,
                        Version = JushuitanErpApiInfo.Version,
                        TokenExpireTime = JushuitanErpApiInfo.TokenExpireTime ?? DateTime.MinValue,
                        LastTokenUpdateTime = JushuitanErpApiInfo.LastTokenUpdateTime ?? DateTime.MinValue,
                    });

                    var (key, value) = await jushuitanErpApi.RefreshAccessTokenAsync();
                    if (key) {
                        //赋值
                        //刷新
                        try {
                            var jObject = JObject.Parse(value);
                            if (jObject["data"]?["access_token"] is not null) {
                                JushuitanErpApiInfo.AccessToken = jObject["data"]?["access_token"]?.ToString() ?? string.Empty;
                                JushuitanErpApiInfo.LastTokenUpdateTime = DateTime.Now;
                                var expiresIn = Convert.ToInt32(jObject["data"]?["expires_in"] ?? "0");
                                if (expiresIn > 0) {
                                    JushuitanErpApiInfo.TokenExpireTime = DateTime.Now.AddSeconds(expiresIn);
                                }
                                base.MessageQueue.Enqueue("刷新成功");
                            }
                            else {
                                base.MessageQueue.Enqueue($"access_token字段不存在");
                            }
                        }
                        catch (Exception e) {
                            base.MessageQueue.Enqueue($"解析返回内容失败:{e.Message}");
                        }
                    }
                    else {
                        //提示错误
                        base.MessageQueue.Enqueue(value);
                    }
                    IsRefreshing = false;
                });
            }
        }

        public ICommand UploadCommand => new DelegateCommand<object>(UploadDelegate);

        private async void UploadDelegate(object obj) {
            //上传测试

            if (!IsUploading) {
                IsUploading = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    //上传
                    var jushuitanErpApi = new JushuitanErpApi(_httpClientFactory);
                    await jushuitanErpApi.SetParameters(new JushuitanErpApi.ApiParameters() {
                        AppKey = JushuitanErpApiInfo.AppKey,
                        AccessToken = JushuitanErpApiInfo.AccessToken,
                        AppSecret = JushuitanErpApiInfo.AppSecret,
                        IsUnLid = JushuitanErpApiInfo.IsUnLid,
                        IsUploadWeight = JushuitanErpApiInfo.IsUploadWeight,
                        Type = JushuitanErpApiInfo.Type,
                        Channel = JushuitanErpApiInfo.Channel,
                        TimeOut = JushuitanErpApiInfo.TimeOut,
                        Url = JushuitanErpApiInfo.Url,
                        Version = JushuitanErpApiInfo.Version,
                        TokenExpireTime = JushuitanErpApiInfo.TokenExpireTime ?? DateTime.MinValue,
                        LastTokenUpdateTime = JushuitanErpApiInfo.LastTokenUpdateTime ?? DateTime.MinValue,
                    });
                    var uploadResponse = await jushuitanErpApi.UploadData(Barcode, Weight);
                    IsUploading = false;
                    //弹窗
                    _dialogService.ShowDialog("ApiTestDialog", new DialogParameters { { "UploadResponse", uploadResponse } }, null);
                });
            }
        }
    }
}