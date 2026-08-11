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
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Client.Models.ApiSettingsModel.ApiConfigurationModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration
{

    public class WdtFlagshipApiPageViewModel : SettingsPageTemplateViewModel
    {
        private readonly IProviderRegistry<IDataUploader> _providerRegistry;
        private readonly IDialogService _dialogService;
        private WdtFlagshipApiInfoModel _wdtFlagshipApiInfo = new();
        private string _barcode = string.Empty;
        private decimal _weight;
        private bool _isUploading;

        private bool _isLoaded;

        public WdtFlagshipApiPageViewModel(IProviderRegistry<IDataUploader> providerRegistry,
            IDialogService dialogService,
            ISettingsStore settingsStore) : base(settingsStore)
        {
            _providerRegistry = providerRegistry;
            _dialogService = dialogService;
        }

        public WdtFlagshipApiInfoModel WdtFlagshipApiInfo
        {
            get => _wdtFlagshipApiInfo;
            set => SetProperty(ref _wdtFlagshipApiInfo, value);
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
        public decimal Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        /// <summary>
        /// 上传中
        /// </summary>
        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public override string Identifier => "WdtFlagshipApiParametersDialogHost";
        public override string SettingsName => "WdtFlagshipApiParameters";

        protected override async Task<bool> SaveSettingsProcess()
        {
            var insertOrUpdate = await _settingsStore.SaveAsync(SettingsName,new WdtFlagshipApiDto()
                {
                    Key = WdtFlagshipApiInfo.Key,
                    Appsecret = WdtFlagshipApiInfo.Appsecret,
                    Sid = WdtFlagshipApiInfo.Sid,
                    Method = WdtFlagshipApiInfo.Method,
                    V = WdtFlagshipApiInfo.V,
                    Salt = WdtFlagshipApiInfo.Salt,
                    PackagerId = WdtFlagshipApiInfo.PackagerId,
                    PackagerNo = WdtFlagshipApiInfo.PackagerNo,
                    OperateTableName = WdtFlagshipApiInfo.OperateTableName,
                    Force = WdtFlagshipApiInfo.Force,
                    Url = WdtFlagshipApiInfo.Url,
                    TimeOut = WdtFlagshipApiInfo.TimeOut,
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
                    var settingsDto = await _settingsStore.GetAsync<WdtFlagshipApiDto>(SettingsName) ??
                                      new WdtFlagshipApiDto();
                    WdtFlagshipApiInfo = new WdtFlagshipApiInfoModel()
                    {
                        Url = settingsDto.Url,
                        Key = settingsDto.Key,
                        Appsecret = settingsDto.Appsecret,
                        Sid = settingsDto.Sid,
                        Method = settingsDto.Method,
                        V = settingsDto.V,
                        Salt = settingsDto.Salt,
                        PackagerId = settingsDto.PackagerId,
                        PackagerNo = settingsDto.PackagerNo,
                        OperateTableName = settingsDto.OperateTableName,
                        Force = settingsDto.Force,
                        TimeOut = settingsDto.TimeOut,
                    };
                });
            }
        }

        public ICommand UploadCommand
        {
            get => new DelegateCommand<object>(UploadDelegate);
        }

        private async void UploadDelegate(object obj)
        {
            if (!IsUploading)
            {
                IsUploading = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsyncUnwrapped(async () =>
                {
                    //上传
                    var wdtFlagshipApi = _providerRegistry.Resolve<WdtFlagshipApi>(ApiType.WdtErpFlagShipApi);
                    await wdtFlagshipApi.SetParameters(new WdtFlagshipApi.ApiParameter
                    {
                        Key = WdtFlagshipApiInfo.Key,
                        Appsecret = WdtFlagshipApiInfo.Appsecret,
                        Sid = WdtFlagshipApiInfo.Sid,
                        Method = WdtFlagshipApiInfo.Method,
                        V = WdtFlagshipApiInfo.V,
                        Salt = WdtFlagshipApiInfo.Salt,
                        PackagerId = WdtFlagshipApiInfo.PackagerId,
                        PackagerNo = WdtFlagshipApiInfo.PackagerNo,
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