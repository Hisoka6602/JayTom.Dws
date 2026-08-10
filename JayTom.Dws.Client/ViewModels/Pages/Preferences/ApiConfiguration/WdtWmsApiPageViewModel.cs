using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Interface;
using JayTom.Dws.Client.Extensions;
using JayTom.Dws.Abstractions.Integrations;
using JayTom.Dws.Application.Configuration;
using System;
using Prism.Mvvm;
using Prism.Commands;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows.Input;
using Prism.Services.Dialogs;
using System.Threading.Tasks;
using JayTom.Dws.Interface.Wdt;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using JayTom.Dws.Domain.Dto.ApiDto;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    public class WdtWmsApiPageViewModel : SettingsPageTemplateViewModel
    {
        private readonly IProviderRegistry<IDataUploader> _providerRegistry;
        private readonly IDialogService _dialogService;
        private WdtWmsApiInfo _wdtWmsApiInfo = new();
        private bool _isLoaded;
        private string _barcode = string.Empty;
        private double _weight;
        private bool _isUploading;
        private string _boxBarcode = string.Empty;

        public WdtWmsApiPageViewModel(IProviderRegistry<IDataUploader> providerRegistry,
            IDialogService dialogService,
            ISettingsStore settingsStore) : base(settingsStore)
        {
            _providerRegistry = providerRegistry;
            _dialogService = dialogService;
        }

        public WdtWmsApiInfo WdtWmsApiInfo
        {
            get => _wdtWmsApiInfo;
            set => SetProperty(ref _wdtWmsApiInfo, value);
        }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value);
        }

        /// <summary>
        /// 重量
        /// </summary>
        public double Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 箱子条码
        /// </summary>
        public string BoxBarcode
        {
            get => _boxBarcode;
            set => SetProperty(ref _boxBarcode, value);
        }

        /// <summary>
        /// 上传中
        /// </summary>
        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public override string Identifier => "WdtWmsApiParametersDialogHost";
        public override string SettingsName => "WdtWmsApiParameters";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new WdtWmsApiDto()
                {
                    AppKey = WdtWmsApiInfo.AppKey,
                    AppSecret = WdtWmsApiInfo.AppSecret,
                    Sid = WdtWmsApiInfo.Sid,
                    Method = WdtWmsApiInfo.Method,
                    Url = WdtWmsApiInfo.Url,
                    TimeOut = WdtWmsApiInfo.TimeOut,
                    MustIncludeBoxBarcode = WdtWmsApiInfo.MustIncludeBoxBarcode,
                    AnyStartCodes = WdtWmsApiInfo.AnyStartCodes
                });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    var settingsDto = await _settingsStore.GetAsync<WdtWmsApiDto>(SettingsName) ?? new WdtWmsApiDto();
                    WdtWmsApiInfo = new WdtWmsApiInfo()
                    {
                        Url = settingsDto.Url,
                        AppKey = settingsDto.AppKey,
                        AppSecret = settingsDto.AppSecret,
                        Sid = settingsDto.Sid,
                        Method = settingsDto.Method,
                        TimeOut = settingsDto.TimeOut,
                        MustIncludeBoxBarcode = settingsDto.MustIncludeBoxBarcode,
                        AnyStartCodes = settingsDto.AnyStartCodes
                    };
                });
            }
        }

        public ICommand UploadCommand => new DelegateCommand<object>(UploadDelegate);

        private async void UploadDelegate(object obj)
        {
            if (!IsUploading)
            {
                IsUploading = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    //上传
                    var wdtWmsApi = _providerRegistry.Resolve<WdtWmsApi>(ApiType.WdtWmsApi);
                    await wdtWmsApi.SetParameters(new WdtWmsApi.ApiParameter
                    {
                        AppKey = WdtWmsApiInfo.AppKey,
                        AppSecret = WdtWmsApiInfo.AppSecret,
                        Sid = WdtWmsApiInfo.Sid,
                        Method = WdtWmsApiInfo.Method,
                        Url = WdtWmsApiInfo.Url,
                        TimeOut = WdtWmsApiInfo.TimeOut,
                        MustIncludeBoxBarcode = WdtWmsApiInfo.MustIncludeBoxBarcode
                    });
                    var uploadResponse = await wdtWmsApi.UploadData(Barcode, Weight, other: BoxBarcode);
                    IsUploading = false;
                    //弹窗
                    _dialogService.ShowDialog("ApiTestDialog", new DialogParameters { { "UploadResponse", uploadResponse } }, null);
                });
            }
        }
    }
}