using System;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows.Input;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Interface.ZhouYi;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Interface.Jushuitan;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration {

    public class ZhouYiApiPageViewModel : SettingsPageTemplateViewModel {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDialogService _dialogService;
        private ZhouYiApiModel _zhouYiApiInfo = new();
        private bool _isUploading;
        private string _barcode = string.Empty;
        private double _weight;

        public ZhouYiApiPageViewModel(IConfigRepository configRepository,
            IHttpClientFactory httpClientFactory, IDialogService dialogService) : base(configRepository) {
            _httpClientFactory = httpClientFactory;
            _dialogService = dialogService;
        }

        public ZhouYiApiModel ZhouYiApiInfo {
            get => _zhouYiApiInfo;
            set => SetProperty(ref _zhouYiApiInfo, value);
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

        public override string Identifier => "ZhouYiApiParametersDialogHost";
        public override string SettingsName => "ZhouYiApiParameters";

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new ZhouYiApiDto() {
                    AppKey = ZhouYiApiInfo.AppKey,
                    AppId = ZhouYiApiInfo.AppId,
                    NeedUpload = ZhouYiApiInfo.NeedUpload,
                    IsFstCode = ZhouYiApiInfo.IsFstCode,
                    TimeOut = ZhouYiApiInfo.TimeOut,
                    Url = ZhouYiApiInfo.Url,
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                var settingsDto = await _configRepository.FirstOrDefaultEntity<ZhouYiApiDto>(SettingsName) ?? new ZhouYiApiDto();
                ZhouYiApiInfo = new ZhouYiApiModel() {
                    AppKey = settingsDto.AppKey,
                    AppId = settingsDto.AppId,
                    NeedUpload = settingsDto.NeedUpload,
                    IsFstCode = settingsDto.IsFstCode,
                    TimeOut = settingsDto.TimeOut,
                    Url = settingsDto.Url,
                };
            });
        }

        public ICommand UploadCommand => new DelegateCommand<object>(UploadDelegate);

        private async void UploadDelegate(object obj) {
            //上传测试

            if (!IsUploading) {
                IsUploading = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    //上传
                    var zhouYiApi = new ZhouYiApi(_httpClientFactory);
                    await zhouYiApi.SetParameters(new ZhouYiApi.ApiParameters() {
                        AppKey = ZhouYiApiInfo.AppKey,
                        AppId = ZhouYiApiInfo.AppId,
                        NeedUpload = ZhouYiApiInfo.NeedUpload,
                        IsFstCode = ZhouYiApiInfo.IsFstCode,
                        TimeOut = ZhouYiApiInfo.TimeOut,
                        Url = ZhouYiApiInfo.Url,
                    });
                    var uploadResponse = await zhouYiApi.UploadData(Barcode, Weight);
                    IsUploading = false;
                    //弹窗
                    _dialogService.ShowDialog("ApiTestDialog", new DialogParameters { { "UploadResponse", uploadResponse } }, null);
                });
            }
        }
    }
}