using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Integrations;
using JayTom.Dws.Client.Extensions;
using JayTom.Dws.Abstractions.Integrations;
using JayTom.Dws.Application.Configuration;
using System;
using System.Linq;
using System.Text;
using Prism.Commands;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows.Input;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Integrations.ZhouYi;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;
using JayTom.Dws.Integrations.Jushuitan;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    public class ZhouYiApiPageViewModel : SettingsPageTemplateViewModel
    {
        private readonly IProviderRegistry<IDataUploader> _providerRegistry;
        private readonly IDialogService _dialogService;
        private ZhouYiApiModel _zhouYiApiInfo = new();
        private bool _isUploading;
        private string _barcode = string.Empty;
        private decimal _weight;

        public ZhouYiApiPageViewModel(ISettingsStore settingsStore,
            IProviderRegistry<IDataUploader> providerRegistry, IDialogService dialogService, JayTom.Dws.Application.Messaging.IEventBus eventBus) : base(settingsStore, eventBus)
        {
            _providerRegistry = providerRegistry;
            _dialogService = dialogService;
        }

        public ZhouYiApiModel ZhouYiApiInfo
        {
            get => _zhouYiApiInfo;
            set => SetProperty(ref _zhouYiApiInfo, value);
        }

        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public string Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        public decimal Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        public override string Identifier => "ZhouYiApiParametersDialogHost";
        public override string SettingsName => "ZhouYiApiParameters";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new ZhouYiApiDto()
                {
                    AppKey = ZhouYiApiInfo.AppKey,
                    ApplicationCode = ZhouYiApiInfo.ApplicationCode,
                    NeedUpload = ZhouYiApiInfo.NeedUpload,
                    IsFstCode = ZhouYiApiInfo.IsFstCode,
                    TimeOut = ZhouYiApiInfo.TimeOut,
                    Url = ZhouYiApiInfo.Url,
                });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var settingsDto = await _settingsStore.GetAsync<ZhouYiApiDto>(SettingsName) ?? new ZhouYiApiDto();
                ZhouYiApiInfo = new ZhouYiApiModel()
                {
                    AppKey = settingsDto.AppKey,
                    ApplicationCode = settingsDto.ApplicationCode,
                    NeedUpload = settingsDto.NeedUpload,
                    IsFstCode = settingsDto.IsFstCode,
                    TimeOut = settingsDto.TimeOut,
                    Url = settingsDto.Url,
                };
            });
        }

        public ICommand UploadCommand => new DelegateCommand<object>(UploadDelegate);

        private async void UploadDelegate(object obj)
        {
            //上传测试

            if (!IsUploading)
            {
                IsUploading = true;
                await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    //上传
                    var zhouYiApi = _providerRegistry.Resolve<ZhouYiApi>(ApiType.ZhouYi);
                    await zhouYiApi.SetParameters(new ZhouYiApi.ApiParameters()
                    {
                        AppKey = ZhouYiApiInfo.AppKey,
                        ApplicationCode = ZhouYiApiInfo.ApplicationCode,
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