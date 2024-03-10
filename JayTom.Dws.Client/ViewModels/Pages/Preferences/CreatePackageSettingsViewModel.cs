using System;
using System.IO;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using JayTom.Dws.Ocr;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.Models.OcrSettingsModel;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class CreatePackageSettingsViewModel : BindableBase {
        private readonly IConfigRepository _configRepository;
        private readonly IDeviceService _deviceService;
        private CreatePackageSettingsInfoModel _createPackageSettingsInfo = new();
        private bool _isSavingInProgress;
        private SnackbarMessageQueue _createPackageSettingsMessageQueue = new();
        private bool _isLoaded;

        public CreatePackageSettingsViewModel(IConfigRepository configRepository, IDeviceService deviceService) {
            _configRepository = configRepository;
            _deviceService = deviceService;
        }

        public CreatePackageSettingsInfoModel CreatePackageSettingsInfo {
            get => _createPackageSettingsInfo;
            set => SetProperty(ref _createPackageSettingsInfo, value);
        }

        public SnackbarMessageQueue CreatePackageSettingsMessageQueue {
            get => _createPackageSettingsMessageQueue;
            set => SetProperty(ref _createPackageSettingsMessageQueue, value);
        }

        public bool IsSavingInProgress {
            get => _isSavingInProgress;
            set => SetProperty(ref _isSavingInProgress, value);
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
                    var configInfoModel = await _configRepository.FirstOrDefault(f => f.ConfigName.Equals("CreatePackageSettings"));
                    if (configInfoModel is not null) {
                        try {
                            var deserializeObject = JsonConvert.DeserializeObject<CreatePackageSettingsDto>(configInfoModel.Value) ?? new CreatePackageSettingsDto();

                            CreatePackageSettingsInfo = new CreatePackageSettingsInfoModel() {
                                IsUseEmptyPackageExpiry = deserializeObject.IsUseEmptyPackageExpiry,
                                EmptyPackageExpiryTime = deserializeObject.EmptyPackageExpiryTime,
                                IsUsePackageExpiry = deserializeObject.IsUsePackageExpiry,
                                PackageExpiryTime = deserializeObject.PackageExpiryTime,
                                BarcodeHandlingMethod = deserializeObject.BarcodeHandlingMethod,
                                PackageCreationMethods = deserializeObject.PackageCreationMethods,
                                IsUseNoRead = deserializeObject.IsUseNoRead,
                                BarcodeQueueOrder = deserializeObject.BarcodeQueueOrder,
                                PackageRemoveMethods = deserializeObject.PackageRemoveMethods,
                                ClearPackageQueueOnStop = deserializeObject.ClearPackageQueueOnStop,
                                IsUseNoReadFilter = deserializeObject.IsUseNoReadFilter,
                                FilterInterval = deserializeObject.FilterInterval,
                                IsGroupBarCode = deserializeObject.IsGroupBarCode
                            };
                            var includedEnums = Enum.GetValues(typeof(PackageCreationMethodsEnum))
                                .Cast<PackageCreationMethodsEnum>()
                                .Where(e => deserializeObject.PackageCreationMethods.HasFlag(e))
                                .ToList();
                            foreach (var infoModel in includedEnums.Select(methodsEnum => CreatePackageSettingsInfo.PackageCreationMethodItems.FirstOrDefault(f =>
                                         f.EnumValue.Equals(methodsEnum))).OfType<PackageCreationMethodItemInfoModel>()) {
                                infoModel.IsChecked = true;
                            }
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public ICommand SaveSettingsCommand {
            get => new DelegateCommand<object>(SaveSettingDelegate);
        }

        private async void SaveSettingDelegate(object obj) {
            if (!IsSavingInProgress) {
                IsSavingInProgress = true;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    if (_deviceService.RunningStatus) {
                        IsSavingInProgress = false;
                        CreatePackageSettingsMessageQueue.Enqueue($"设备工作中,无法设置");
                        return;
                    }

                    CreatePackageSettingsInfo.PackageCreationMethods = 0;
                    foreach (var item in CreatePackageSettingsInfo.PackageCreationMethodItems.Where(w => w.IsChecked).ToList()) {
                        CreatePackageSettingsInfo.PackageCreationMethods |= item.EnumValue;
                    }
                    var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                        ConfigName = "CreatePackageSettings",
                        Value = JsonConvert.SerializeObject(new CreatePackageSettingsDto() {
                            IsUseEmptyPackageExpiry = CreatePackageSettingsInfo.IsUseEmptyPackageExpiry,
                            EmptyPackageExpiryTime = CreatePackageSettingsInfo.EmptyPackageExpiryTime,
                            IsUsePackageExpiry = CreatePackageSettingsInfo.IsUsePackageExpiry,
                            PackageExpiryTime = CreatePackageSettingsInfo.PackageExpiryTime,
                            BarcodeHandlingMethod = CreatePackageSettingsInfo.BarcodeHandlingMethod,
                            PackageCreationMethods = CreatePackageSettingsInfo.PackageCreationMethods,
                            IsUseNoRead = CreatePackageSettingsInfo.IsUseNoRead,
                            BarcodeQueueOrder = CreatePackageSettingsInfo.BarcodeQueueOrder,
                            PackageRemoveMethods = CreatePackageSettingsInfo.PackageRemoveMethods,
                            ClearPackageQueueOnStop = CreatePackageSettingsInfo.ClearPackageQueueOnStop,
                            IsUseNoReadFilter = CreatePackageSettingsInfo.IsUseNoReadFilter,
                            FilterInterval = CreatePackageSettingsInfo.FilterInterval,
                            IsGroupBarCode = CreatePackageSettingsInfo.IsGroupBarCode
                        })
                    });
                    if (insertOrUpdate) {
                        EventAggregator.Instance.Publish(new SettingsChangedEvent {
                            SettingsName = "CreatePackageSettings"
                        });
                    }

                    IsSavingInProgress = false;
                    CreatePackageSettingsMessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                        Languages.Language.ResourceManager.GetString("SaveFailed"))}");
                });
            }
        }
    }
}