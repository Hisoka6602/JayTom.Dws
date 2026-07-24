using Polly;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Polly.Retry;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using JayTom.Dws.Domain.Model;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Data.LocalData;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Domain.Dto.AppDto;
using JayTom.Dws.Domain.Dto.ApiDto;
using System.Collections.Concurrent;
using JayTom.Dws.Domain.Dto.CloudDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Domain.Dto.CameraConfiguration;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using ApiExceptionType = JayTom.Dws.Interface.ApiExceptionType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using InstructionType = JayTom.Dws.Interface.Cloud.InstructionType;
using RemoteAction = JayTom.Dws.Client.EventMediators.RemoteAction;
using RemoteCommand = JayTom.Dws.Client.EventMediators.RemoteCommand;
using WindowsAction = JayTom.Dws.Client.EventMediators.WindowsAction;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using WindowsActionType = JayTom.Dws.Client.EventMediators.WindowsActionType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using SettingsChangedEvent = JayTom.Dws.Client.EventMediators.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.BackgroundService {

    public class CloudBackgroundService : Microsoft.Extensions.Hosting.BackgroundService {
        private readonly IConfigRepository _configRepository;
        private readonly ICloud _cloud;
        private readonly IPackageRepository _packageRepository;
        private readonly ICloudVideoUploadRepository _cloudVideoUploadRepository;
        private readonly INvrCameraBindingRepository _nvrCameraBindingRepository;
        private readonly IComputer _computer;
        private readonly ISyncSettingsService _syncSettingsService;
        private readonly IApiSortingRepository _apiSortingRepository;
        private readonly IBarCodeSortingRepository _barCodeSortingRepository;
        private readonly ILogisticsSortingRepository _logisticsSortingRepository;
        private readonly IOcrSortingRepository _ocrSortingRepository;
        private readonly IVolumeSortingRepository _volumeSortingRepository;
        private readonly IWeightSortingRepository _weightSortingRepository;
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private readonly ILogisticsCodeRecognitionRepository _logisticsCodeRecognitionRepository;
        private readonly IPackageExitDefinitionRepository _packageExitDefinitionRepository;
        private readonly IPackageExitLockBindingRepository _packageExitLockBindingRepository;
        private readonly ISortingInstructionBindingRepository _sortingInstructionBindingRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private CloudVideoSettingsDto _cloudVideoSettingsDto = new();
        private SyncSettingsDto _syncSettingsDto = new();
        private long _startTimeTicks = DateTime.Now.Ticks;
        private SemaphoreSlim _cloudVideoUpLoadSlim = new(2);
        private NvrCameraBindingInfoModel[] _nvrCameraBindingInfoModels =
            Array.Empty<NvrCameraBindingInfoModel>();
        private readonly SemaphoreSlim _settingsUpdateGate = new(1, 1);
        private int _isWindowsClose;

        public CloudBackgroundService(IConfigRepository configRepository,
            ICloud cloud, IPackageRepository packageRepository,
            ICloudVideoUploadRepository cloudVideoUploadRepository,
            INvrCameraBindingRepository nvrCameraBindingRepository,
            IComputer computer,
            ISyncSettingsService syncSettingsService,
            IApiSortingRepository apiSortingRepository,
            IBarCodeSortingRepository barCodeSortingRepository,
            ILogisticsSortingRepository logisticsSortingRepository,
            IOcrSortingRepository ocrSortingRepository,
            IVolumeSortingRepository volumeSortingRepository,
            IWeightSortingRepository weightSortingRepository,
            ICommunicationConnectionConfigRepository communicationConnectionConfigRepository,
            ILogisticsCodeRecognitionRepository logisticsCodeRecognitionRepository,
            IPackageExitDefinitionRepository packageExitDefinitionRepository,
            IPackageExitLockBindingRepository packageExitLockBindingRepository,
            ISortingInstructionBindingRepository sortingInstructionBindingRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository) {
            _configRepository = configRepository;
            _cloud = cloud;
            _packageRepository = packageRepository;
            _cloudVideoUploadRepository = cloudVideoUploadRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
            _computer = computer;
            _syncSettingsService = syncSettingsService;
            _apiSortingRepository = apiSortingRepository;
            _barCodeSortingRepository = barCodeSortingRepository;
            _logisticsSortingRepository = logisticsSortingRepository;
            _ocrSortingRepository = ocrSortingRepository;
            _volumeSortingRepository = volumeSortingRepository;
            _weightSortingRepository = weightSortingRepository;
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
            _logisticsCodeRecognitionRepository = logisticsCodeRecognitionRepository;
            _packageExitDefinitionRepository = packageExitDefinitionRepository;
            _packageExitLockBindingRepository = packageExitLockBindingRepository;
            _sortingInstructionBindingRepository = sortingInstructionBindingRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;

            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                await _settingsUpdateGate.WaitAsync();
                try {
                    switch (item) {
                    case { SettingsName: "CloudVideoSettings" } model: {
                            var settings = await _configRepository
                                .FirstOrDefaultEntity<CloudVideoSettingsDto>(model.SettingsName) ??
                                new CloudVideoSettingsDto();
                            Volatile.Write(ref _cloudVideoSettingsDto, settings);
                            Interlocked.Exchange(
                                ref _cloudVideoUpLoadSlim,
                                new SemaphoreSlim(Math.Max(1, settings.Concurrency)));
                            if (settings.IsAutoUploadUnsyncedData) {
                                Interlocked.Exchange(
                                    ref _startTimeTicks,
                                    new DateTime(1970, 1, 1).Ticks);
                            }
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseCloudSync: true } &&
                                model.IsLocallySaved) {
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(model.SettingsName, settings);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "SyncSettingsSettings" } syncSettingsSettings: {
                            Volatile.Write(
                                ref _syncSettingsDto,
                                await _configRepository.FirstOrDefaultEntity<SyncSettingsDto>(
                                    syncSettingsSettings.SettingsName) ?? new SyncSettingsDto());
                            break;
                        }
                    case { SettingsName: "NvrCameraBindingInfoModel" }:
                        try {
                            Volatile.Write(
                                ref _nvrCameraBindingInfoModels,
                                (await _nvrCameraBindingRepository.MemoryCacheData()).ToArray());
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }

                        break;

                    case { SettingsName: "ApiSettings", IsLocallySaved: true } apiSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var apiSettingsDto = await _configRepository.FirstOrDefaultEntity<ApiSettingsDto>(apiSettings.SettingsName) ?? new ApiSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(apiSettings.SettingsName, apiSettingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "CaiNiaoApiParameters", IsLocallySaved: true } caiNiaoApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var caiNiaoApiDto = await _configRepository.FirstOrDefaultEntity<CaiNiaoApiDto>(caiNiaoApiParameters.SettingsName) ?? new CaiNiaoApiDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(caiNiaoApiParameters.SettingsName, caiNiaoApiDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "EshippingitApiParameters", IsLocallySaved: true } eshippingitApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<EshippingitApiDto>(eshippingitApiParameters.SettingsName) ?? new EshippingitApiDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(eshippingitApiParameters.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "JtExpressApiParameters", IsLocallySaved: true } jtExpressApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<JtExpressDto>(jtExpressApiParameters.SettingsName) ?? new JtExpressDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(jtExpressApiParameters.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "JtPolarDayApiParameters", IsLocallySaved: true } jtPolarDayApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<JtPolarDayDto>(jtPolarDayApiParameters.SettingsName) ?? new JtPolarDayDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(jtPolarDayApiParameters.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "RoutDataApiParameters", IsLocallySaved: true } routDataApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<RoutDataApiDto>(routDataApiParameters.SettingsName) ?? new RoutDataApiDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(routDataApiParameters.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "SzjyApiParameters", IsLocallySaved: true } szjyApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<SzjyApiDto>(szjyApiParameters.SettingsName) ?? new SzjyApiDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(szjyApiParameters.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "WdtFlagshipApiParameters", IsLocallySaved: true } wdtFlagshipApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<WdtFlagshipApiDto>(wdtFlagshipApiParameters.SettingsName) ?? new WdtFlagshipApiDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(wdtFlagshipApiParameters.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "WdtWmsApiParameters", IsLocallySaved: true } wdtWmsApiParameters: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<WdtWmsApiDto>(wdtWmsApiParameters.SettingsName) ?? new WdtWmsApiDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(wdtWmsApiParameters.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "SaveImageSettings", IsLocallySaved: true } saveImageSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseImageStorageSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<ImageSettingsDto>(saveImageSettings.SettingsName) ?? new ImageSettingsDto();

                                var (key, value) = await _syncSettingsService.SubmitSyncContent(saveImageSettings.SettingsName, JsonConvert.SerializeObject(settingsDto));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "BarcodeFilterSettings", IsLocallySaved: true } barcodeFilterSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseFilterSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<BarcodeFilterSettingsDto>(barcodeFilterSettings.SettingsName) ?? new BarcodeFilterSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(barcodeFilterSettings.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "ContentInputSettings", IsLocallySaved: true } contentInputSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseContentInputSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>(contentInputSettings.SettingsName) ?? new ContentInputSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(contentInputSettings.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "CreatePackageSettings", IsLocallySaved: true } createPackageSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUsePackagingSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<CreatePackageSettingsDto>(createPackageSettings.SettingsName) ?? new CreatePackageSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(createPackageSettings.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "OcrSettings", IsLocallySaved: true } ocrSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseOcrSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<OcrSettingsDto>(ocrSettings.SettingsName) ?? new OcrSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(ocrSettings.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "CacheClearSettings", IsLocallySaved: true } cacheClearSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSpaceCleaningSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<CacheClearSettingsDto>(cacheClearSettings.SettingsName) ?? new CacheClearSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(cacheClearSettings.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "ApiSortingItemsSettings", IsLocallySaved: true } apiSortingItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var apiSortingInfoModels = await _apiSortingRepository.ApiSortingItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(apiSortingItemsSettings.SettingsName,
                                    JsonConvert.SerializeObject(apiSortingInfoModels,
                                    new JsonSerializerSettings() {
                                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                    }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "BarcodeSortingItemsSettings", IsLocallySaved: true } barcodeSortingItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var barCodeSortingInfoModels = await _barCodeSortingRepository.BarCodeSortingItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(barcodeSortingItemsSettings.SettingsName,
                                    JsonConvert.SerializeObject(barCodeSortingInfoModels,
                                        new JsonSerializerSettings() {
                                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                        }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "LogisticsSortingItemsSettings", IsLocallySaved: true } logisticsSortingItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var logisticsSortingInfoModels = await _logisticsSortingRepository.LogisticsSortingItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(logisticsSortingItemsSettings.SettingsName,
                                    JsonConvert.SerializeObject(logisticsSortingInfoModels,
                                        new JsonSerializerSettings() {
                                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                        }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "OcrSortingItemsSettings", IsLocallySaved: true } ocrSortingItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var ocrSortingInfoModels = await _ocrSortingRepository.OcrSortingItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(ocrSortingItemsSettings.SettingsName,
                                    JsonConvert.SerializeObject(ocrSortingInfoModels,
                                        new JsonSerializerSettings() {
                                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                        }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "VolumeSortingItemsSettings", IsLocallySaved: true } volumeSortingItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var volumeSortingInfoModels = await _volumeSortingRepository.VolumeSortingItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(volumeSortingItemsSettings.SettingsName,
JsonConvert.SerializeObject(volumeSortingInfoModels, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "WeightSortingItemsSettings", IsLocallySaved: true } weightSortingItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var weightSortingInfoModels = await _weightSortingRepository.WeightSortingItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(weightSortingItemsSettings.SettingsName, JsonConvert.SerializeObject(weightSortingInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "CommunicationsItemsSettings", IsLocallySaved: true } communicationsItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseConnectionSync: true }) {
                                var communicationConnectionConfigInfoModels = await _communicationConnectionConfigRepository.CommunicationConnectionConfigItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(communicationsItemsSettings.SettingsName,
                                    JsonConvert.SerializeObject(communicationConnectionConfigInfoModels,
                                    new JsonSerializerSettings() {
                                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                    }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "LogisticsCodeRecognitionItemSettings", IsLocallySaved: true } logisticsCodeRecognitionItemSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseLogisticsSync: true }) {
                                var logisticsCodeRecognitionInfoModels = await _logisticsCodeRecognitionRepository.LogisticsCodes(s => s.Id > 0);

                                var (key, value) = await _syncSettingsService.SubmitSyncContent(logisticsCodeRecognitionItemSettings.SettingsName, JsonConvert.SerializeObject(logisticsCodeRecognitionInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "PackageExitDefinitionItemSettings", IsLocallySaved: true } packageExitDefinitionItemSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseExitSync: true }) {
                                var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.Select(s => s.Id > 0, o => o.Id);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(packageExitDefinitionItemSettings.SettingsName, JsonConvert.SerializeObject(packageExitDefinitionInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "PackageExitLockSettings", IsLocallySaved: true } packageExitLockSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<PackageExitLockSettingsDto>(packageExitLockSettings.SettingsName) ?? new PackageExitLockSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(packageExitLockSettings.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "PackageExitLockBindingItemSettings", IsLocallySaved: true } packageExitLockBindingItemSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true }) {
                                var packageExitLockBindingInfoModels = await _packageExitLockBindingRepository.Select(s => s.Id > 0, o => o.Id);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(packageExitLockBindingItemSettings.SettingsName,
                                    JsonConvert.SerializeObject(packageExitLockBindingInfoModels,
                                    new JsonSerializerSettings() {
                                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                    }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "SortingInstructionBindingItemSettings", IsLocallySaved: true } sortingInstructionBindingItemSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseInstructionSync: true }) {
                                var sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository.InstructionBindings(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(sortingInstructionBindingItemSettings.SettingsName, JsonConvert.SerializeObject(sortingInstructionBindingInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "StackedPackageDetectionSettings", IsLocallySaved: true } stackedPackageDetectionSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseStackingSync: true }) {
                                var stackedPackageDetectionSettingsDto = await _configRepository.FirstOrDefaultEntity<StackedPackageDetectionSettingsDto>(stackedPackageDetectionSettings.SettingsName) ?? new StackedPackageDetectionSettingsDto();

                                var (key, value) = await _syncSettingsService.SubmitSyncContent(stackedPackageDetectionSettings.SettingsName, stackedPackageDetectionSettingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "SupplyCounterSettings", IsLocallySaved: true } supplyCounterSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSupplyCounterSync: true }) {
                                var supplyCounterSettingsDto = await _configRepository.FirstOrDefaultEntity<SupplyCounterSettingsDto>(supplyCounterSettings.SettingsName) ?? new SupplyCounterSettingsDto();

                                var (key, value) = await _syncSettingsService.SubmitSyncContent(supplyCounterSettings.SettingsName, supplyCounterSettingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "SortingMethodSettings", IsLocallySaved: true } sortingMethodSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var sortingMethodDto = await _configRepository.FirstOrDefaultEntity<SortingMethodDto>(sortingMethodSettings.SettingsName) ?? new SortingMethodDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(sortingMethodSettings.SettingsName, sortingMethodDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case { SettingsName: "AlgorithmSettings", IsLocallySaved: true } algorithmSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseAlgorithmSync: true }) {
                                var barcodeReaderDto = await _configRepository.FirstOrDefaultEntity<UsbBarcodeReaderDto>(algorithmSettings.SettingsName) ?? new UsbBarcodeReaderDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(algorithmSettings.SettingsName, barcodeReaderDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    }
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(e, $"更新云端同步配置失败:{item.SettingsName}");
                }
                finally {
                    _settingsUpdateGate.Release();
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(item => {
                if (item is { Type: WindowsActionType.Close }) {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                }
            });
            _syncSettingsService.SyncContentReceived += async (sender, info) => {
                try {
                    EventAggregator.Instance.Publish(new RemoteAction {
                        Command = RemoteCommand.Stop
                    });

                    switch (info.SettingsName) {
                        case "CloudVideoSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseCloudSync: true }:
                        case "ApiSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "CaiNiaoApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "EshippingitApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "JtExpressApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "JtPolarDayApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "RoutDataApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "SzjyApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "WdtFlagshipApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "WdtWmsApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "BarcodeFilterSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseFilterSync: true }:
                        case "ContentInputSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseContentInputSync: true }:
                        case "CreatePackageSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUsePackagingSync: true }:
                        case "OcrSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseOcrSync: true }:
                        case "CacheClearSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSpaceCleaningSync: true }:
                        case "PackageExitLockSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true }:
                        case "StackedPackageDetectionSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseStackingSync: true }:
                        case "SortingMethodSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
                        case "SupplyCounterSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSupplyCounterSync: true }:
                        case "AlgorithmSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseAlgorithmSync: true }:
                            await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                                ConfigName = info.SettingsName,
                                Value = JsonConvert.SerializeObject(info.SettingsInfo)
                            });
                            break;

                        case "ApiSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<ApiSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _apiSortingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }

                                break;
                            }
                        case "BarcodeSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<BarCodeSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _barCodeSortingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }

                                break;
                            }
                        case "LogisticsSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<LogisticsSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _logisticsSortingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "OcrSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<OcrSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _ocrSortingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "VolumeSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<VolumeSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _volumeSortingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "WeightSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<WeightSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _weightSortingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "CommunicationsItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseConnectionSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<CommunicationConnectionConfigInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _communicationConnectionConfigRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "LogisticsCodeRecognitionItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseLogisticsSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<LogisticsCodeRecognitionInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _logisticsCodeRecognitionRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "PackageExitDefinitionItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseExitSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<PackageExitDefinitionInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _packageExitDefinitionRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "PackageExitLockBindingItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<PackageExitLockBindingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _packageExitLockBindingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                        case "SortingInstructionBindingItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseInstructionSync: true }: {
                                try {
                                    var models = JsonConvert.DeserializeObject<List<SortingInstructionBindingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null) {
                                        await _sortingInstructionBindingRepository.SyncEntities(models);
                                    }
                                }
                                catch (Exception e) {
                                }
                                break;
                            }

                        case "SaveImageSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseImageStorageSync: true }: {
                                try {
                                    await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                                        ConfigName = info.SettingsName,
                                        Value = info.SettingsInfo?.ToString() ?? string.Empty
                                    });
                                }
                                catch (Exception e) {
                                }
                                break;
                            }
                    }
                    EventAggregator.Instance.Publish(new SettingsChangedEvent {
                        SettingsName = info.SettingsName,
                    });
                    EventAggregator.Instance.Publish(new AppLogInfoModel {
                        CreateTime = DateTime.Now,
                        Message = $"远程更新配置:{info.SettingsName}",
                        Type = LogType.Information
                    });
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    EventAggregator.Instance.Publish(new AppLogInfoModel {
                        CreateTime = DateTime.Now,
                        Message = $"远程更新配置:{info.SettingsName}失败,异常:{e.Message}",
                        Type = LogType.Information
                    });
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var initialSettings =
                await _configRepository.FirstOrDefaultEntity<CloudVideoSettingsDto>(
                    "CloudVideoSettings", stoppingToken) ??
                new CloudVideoSettingsDto();
            Volatile.Write(ref _cloudVideoSettingsDto, initialSettings);
            Interlocked.Exchange(
                ref _cloudVideoUpLoadSlim,
                new SemaphoreSlim(Math.Max(1, initialSettings.Concurrency)));
            if (initialSettings.IsAutoUploadUnsyncedData) {
                Interlocked.Exchange(ref _startTimeTicks, new DateTime(1970, 1, 1).Ticks);
            }

            Volatile.Write(
                ref _syncSettingsDto,
                await _configRepository.FirstOrDefaultEntity<SyncSettingsDto>(
                    "SyncSettingsSettings", stoppingToken) ?? new SyncSettingsDto());
            Volatile.Write(
                ref _nvrCameraBindingInfoModels,
                (await _nvrCameraBindingRepository.MemoryCacheData()).ToArray());

            // 这是低频对账工作器，不属于设备/分拣热路径。业务热回调不访问数据库和文件。
            using var reconciliationTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            while (Volatile.Read(ref _isWindowsClose) == 0 &&
                   await reconciliationTimer.WaitForNextTickAsync(stoppingToken)) {
                try {
                    var settings = Volatile.Read(ref _cloudVideoSettingsDto);
                    if (settings.IsUseCloudVideoUpload) {
                        var uploadGate = Volatile.Read(ref _cloudVideoUpLoadSlim);
                        if (uploadGate.CurrentCount > 0) {
                            var startTime = new DateTime(
                                Interlocked.Read(ref _startTimeTicks),
                                DateTimeKind.Local);
                                var (key, value) = await _packageRepository.SelectPackage(s =>
                                        s.BarCodeInfo != null &&
                                        s.BarCodeInfo.ScanTime.CompareTo(startTime) > 0 &&
                                        s.BarCodeInfo.ScanTime.CompareTo(
                                            DateTime.Now.AddSeconds(0 - settings
                                                .UploadIntervalInSeconds)) <= 0 &&
                                        (s.CloudVideoUploadInfo == null || s.CloudVideoUploadInfo.UploadTime == null),
                                    o => o.PackageCreateTime, 0,
                                    Math.Max(1, settings.Concurrency), stoppingToken);

                                if (key && value is { } packageInfoModels) {
                                    var pendingPackages = packageInfoModels
                                        .Where(package => package.BarCodeInfo != null)
                                        .ToArray();
                                    if (pendingPackages.Length > 0) {
                                        await Task.WhenAll(pendingPackages.Select(package =>
                                            PolicyVideoUploadAsync(
                                                package,
                                                settings,
                                                uploadGate,
                                                stoppingToken)));

                                        Interlocked.Exchange(
                                            ref _startTimeTicks,
                                            pendingPackages.Max(package =>
                                                package.BarCodeInfo!.ScanTime).Ticks);
                                    }
                                }
                            }
                        }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                    break;
                }
                catch (Exception e) {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }

        private async Task PolicyVideoUploadAsync(
            PackageInfoModel packageInfoModel,
            CloudVideoSettingsDto settings,
            SemaphoreSlim uploadGate,
            CancellationToken token) {
            var gateEntered = false;
            try {
                await uploadGate.WaitAsync(token);
                gateEntered = true;
                var retryPolicy = Policy.HandleResult<bool>(result => !result)
                    .Or<Exception>().RetryAsync(settings.RetryAttempts, (a, b) => {
                        EventAggregator.Instance.Publish(new CloudVideoUploadRetryMessage {
                            Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                            RetryCount = b
                        });
                    });
                await retryPolicy.ExecuteAsync(async () => {
                    var (key, value) = await _cloud.SetParameters(new Dictionary<string, object>()
                    {
                        { "WebDoMain", settings.WebDoMain },
                        { "Timeout", settings.RequestTimeout },
                    });
                    if (key) {
                        //取出绑定信息
                        var serialNumber = packageInfoModel.BarCodeInfo?.SerialNumber;
                        var nvrCameraBindingInfoModels = Volatile
                            .Read(ref _nvrCameraBindingInfoModels)
                            .Where(binding => binding.SerialNumber.Equals(
                                serialNumber,
                                StringComparison.Ordinal))
                            .ToArray();

                        var cloudUploadResponse = await UploadCloudDataAsync(new PackageCloudInfo() {
                            PackageCreateTime = packageInfoModel.PackageCreateTime,
                            PackageTimestamped = packageInfoModel.PackageTimestamped,
                            BarCodeInfo = new PackageCloudBarCodeInfo() {
                                Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                                SerialNumber = packageInfoModel.BarCodeInfo?.SerialNumber ?? string.Empty,
                                DisplayIdentifier = packageInfoModel.BarCodeInfo?.DisplayIdentifier ?? string.Empty,
                                ScanTime = packageInfoModel.BarCodeInfo?.ScanTime ?? DateTime.Now,
                                Source = (int)(packageInfoModel.BarCodeInfo?.Source ?? 0),
                            },
                            WeightInfo = new PackageCloudWeightInfo() {
                                CreateTime = packageInfoModel.WeightInfo?.CreateTime ?? DateTime.MinValue,
                                FormattedWeight = packageInfoModel.WeightInfo?.FormattedWeight ?? 0,
                                OriginalText = packageInfoModel.WeightInfo?.OriginalText ?? string.Empty,
                                SourceType = (int)(packageInfoModel.WeightInfo?.SourceType ?? 0),
                                WeighingMode = (int)(packageInfoModel.WeightInfo?.WeighingMode ?? 0),
                            },
                            VolumeInfo = new PackageCloudVolumeInfo() {
                                CreateTime = packageInfoModel.VolumeInfo?.CreateTime ?? DateTime.MinValue,
                                FormattedHeight = packageInfoModel.VolumeInfo?.FormattedHeight ?? 0,
                                FormattedWidth = packageInfoModel.VolumeInfo?.FormattedWidth ?? 0,
                                FormattedVolume = packageInfoModel.VolumeInfo?.FormattedVolume ?? 0,
                                FormattedLength = packageInfoModel.VolumeInfo?.FormattedLength ?? 0,
                                OriginalText = packageInfoModel.VolumeInfo?.OriginalText ?? string.Empty,
                                SourceType = (int)(packageInfoModel.VolumeInfo?.SourceType ?? 0),
                            },
                            UploadInfo = new PackageCloudUploadInfo() {
                                ApiExceptionType = (ApiExceptionType)(packageInfoModel.UploadInfo?.ApiExceptionType ??
                                                                      Data.Package.ApiExceptionType.None),
                                DurationInSeconds = packageInfoModel.UploadInfo?.DurationInSeconds ?? 0,
                                ExceptionMessage = packageInfoModel.UploadInfo?.ExceptionMessage ?? string.Empty,
                                InterfaceParameters = packageInfoModel.UploadInfo?.InterfaceParameters ?? string.Empty,
                                RequestContent = packageInfoModel.UploadInfo?.RequestContent ?? string.Empty,
                                RequestStatus = (int)(packageInfoModel.UploadInfo?.RequestStatus ?? 0),
                                RequestTime = packageInfoModel.UploadInfo?.RequestTime ?? DateTime.MinValue,
                                RequestUrl = packageInfoModel.UploadInfo?.RequestUrl ?? string.Empty,
                                ResponseContent = packageInfoModel.UploadInfo?.ResponseContent ?? string.Empty,
                                ResponseTime = packageInfoModel.UploadInfo?.ResponseTime ?? DateTime.MinValue,
                            },
                            ExitInfo = new PackageCloudExitInfo() {
                                PhysicalExit = packageInfoModel.ExitInfo?.PhysicalExit ?? string.Empty,
                                TheoreticalExit = packageInfoModel.ExitInfo?.TheoreticalExit ?? string.Empty,
                                PhysicalExitId = packageInfoModel.ExitInfo?.PhysicalExitId ?? 0,
                            },
                            SortingInfo = new PackageCloudSortingInfo() {
                                ConnectionName = packageInfoModel.SortingInfo?.ConnectionName ?? string.Empty,
                                ChecksumProtocolName =
                                    packageInfoModel.SortingInfo?.ChecksumProtocolName ?? string.Empty,
                                CommunicationMethod = (int)(packageInfoModel.SortingInfo?.CommunicationMethod ?? 0),
                                IsCreatedByLowerMachine =
                                    packageInfoModel.SortingInfo?.IsCreatedByLowerMachine ?? false,
                                InstructionInfos = packageInfoModel.SortingInfo?.InstructionInfos?
                                    .Select(s => new PackageCloudInstructionInfo {
                                        InstructionContent = s.InstructionContent,
                                        InstructionGeneratedTime = s.InstructionGeneratedTime,
                                        InstructionType = (InstructionType)s.InstructionType,
                                    })?.ToList(),
                                SortingMode = (int)(packageInfoModel.SortingInfo?.SortingMode ?? 0),
                                IsSortingUsed = packageInfoModel.SortingInfo?.IsSortingUsed ?? false,
                                IsAbnormalSorting = packageInfoModel.SortingInfo?.IsAbnormalSorting ?? false,
                                AbnormalSortingType =
                                    (PackageCloudAbnormalSortingType)
                                    (packageInfoModel.SortingInfo?.AbnormalSortingType ?? AbnormalSortingType.None)
                            },
                            LogisticsInfo = new PackageCloudLogisticsInfo() {
                                LogisticsCode = packageInfoModel.LogisticsInfo?.LogisticsCode ?? string.Empty,
                                LogisticsName = packageInfoModel.LogisticsInfo?.LogisticsName ?? string.Empty,
                            },
                            OcrInfo = new PackageCloudOcrInfo() {
                                CameraSerialNumber = packageInfoModel.OcrInfo?.CameraSerialNumber ?? string.Empty,

                                ElapsedMilliseconds = packageInfoModel.OcrInfo?.ElapsedMilliseconds ?? 0,
                                SubmitTimestamp = packageInfoModel.OcrInfo?.SubmitTimestamp ?? 0,
                                ThreeSegmentCode = packageInfoModel.OcrInfo?.ThreeSegmentCode ?? string.Empty,
                                RecognizeTime = packageInfoModel.OcrInfo?.RecognizeTime ?? DateTime.MinValue,
                                VirtualNumberLast4 = packageInfoModel.OcrInfo?.VirtualNumberLast4 ?? string.Empty,
                                OriginalContent = packageInfoModel.OcrInfo?.OriginalContent ?? string.Empty,
                                OcrDetailedInfos = packageInfoModel.OcrInfo?.OcrDetailedInfos?.Select(s =>
                                    new PackageCloudOcrDetailedInfo() {
                                        Address = s.Address,
                                        InformationType = (int)s.InformationType,
                                        Name = s.Name,
                                        Phone = s.Phone,
                                    })?.ToList(),
                            },
                            ImageInfos = packageInfoModel.ImageInfos?.Select(s =>
                                new PackageCloudImageInfo {
                                    CameraSerialNumber = s.CameraSerialNumber,
                                    CameraName = s.CameraName,
                                    CustomCameraName = s.CustomCameraName,
                                    Type = s.Type,
                                    Image = LoadImageSnapshot(s.LocalPath)
                                })?.ToList(),
                            DeviceInfo = new PackageCloudDeviceInfo() {
                                NodeName = settings.NodeName,
                                MachineCode = await _computer.GenerateMachineCode(),
                            },
                            CloudNvrCameraBindingInfos = nvrCameraBindingInfoModels?.Select(s =>
                                new PackageCloudNvrCameraBindingInfo {
                                    Channel = s.Channel,
                                    IpAddress = s.IpAddress,
                                    Password = s.Password,
                                    Port = s.Port,
                                    Username = s.Username
                                })?.ToList()
                        }, token: token);

                        EventAggregator.Instance.Publish(new CloudVideoUploadMessage {
                            Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                            IsSuccessful = cloudUploadResponse.IsSuccessful,
                            PanoramaImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0,
                            ScanImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                            ScanTime = packageInfoModel.BarCodeInfo?.ScanTime ?? DateTime.Now
                        });

                        if (cloudUploadResponse.IsSuccessful) {
                            var cloudVideoUploadInfoModel = await _cloudVideoUploadRepository.FirstOrDefault(f =>
                                f.PackageId.Equals(packageInfoModel.Id), token);
                            if (cloudVideoUploadInfoModel is not null) {
                                //更新
                                cloudVideoUploadInfoModel.ResponseContent = cloudUploadResponse.ResponseContent;
                                cloudVideoUploadInfoModel.TargetAddress = cloudUploadResponse.TargetAddress;
                                cloudVideoUploadInfoModel.UploadTime = cloudUploadResponse.UploadTime;
                                cloudVideoUploadInfoModel.UploadContent = cloudUploadResponse.UploadContent;
                                cloudVideoUploadInfoModel.UploadDuration = cloudUploadResponse.UploadDuration;
                                cloudVideoUploadInfoModel.ScanImageCount =
                                    packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0;
                                cloudVideoUploadInfoModel.PanoramaImageCount =
                                    packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0;

                                return await _cloudVideoUploadRepository.Update(cloudVideoUploadInfoModel, token);
                            }
                            else {
                                return await _cloudVideoUploadRepository.Insert(new CloudVideoUploadInfoModel() {
                                    PackageId = packageInfoModel.Id,
                                    ResponseContent = cloudUploadResponse.ResponseContent,
                                    TargetAddress = cloudUploadResponse.TargetAddress,
                                    UploadTime = cloudUploadResponse.UploadTime,
                                    UploadContent = cloudUploadResponse.UploadContent,
                                    UploadDuration = cloudUploadResponse.UploadDuration,
                                    ScanImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                                    PanoramaImageCount =
                                        packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0
                                }, token);
                            }
                        }
                        return false;
                    }

                    return false;
                });
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally {
                if (gateEntered) {
                    uploadGate.Release();
                }
            }
        }

        private async Task<CloudUploadResponse> UploadCloudDataAsync(
            PackageCloudInfo package,
            CancellationToken token) {
            try {
                return await _cloud.UploadData(package, token: token);
            }
            finally {
                if (package.ImageInfos is not null) {
                    foreach (var imageInfo in package.ImageInfos) {
                        imageInfo.Image?.Dispose();
                        imageInfo.Image = null;
                    }
                }
            }
        }

        /// <summary>
        /// 在云上传工作器中创建图片快照，立即解除源文件锁。
        /// 返回的图片由上传请求负责在请求结束后释放。
        /// </summary>
        private static Image? LoadImageSnapshot(string? path) {
            if (string.IsNullOrWhiteSpace(path)) {
                return null;
            }

            try {
                using var source = Image.FromFile(path);
                return new Bitmap(source);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                      or ArgumentException) {
                NLog.LogManager.GetCurrentClassLogger()
                    .Warn(e, $"读取待上传图片失败:{path}");
                return null;
            }
        }
    }
}
