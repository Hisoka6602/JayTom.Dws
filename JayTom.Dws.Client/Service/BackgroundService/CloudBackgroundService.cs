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
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Data.LocalConf.CloudConfig;
using JayTom.Dws.Client.Service.ImageStorage;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Domain.Dto.PackageExitLockDto;
using JayTom.Dws.Domain.Dto.CameraConfiguration;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using ApiExceptionType = JayTom.Dws.Interface.ApiExceptionType;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using InstructionType = JayTom.Dws.Interface.Cloud.InstructionType;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

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
        private CloudVideoSettingsDto _cloudVideoSettingsDto = new();
        private SyncSettingsDto _syncSettingsDto = new();
        private DateTime _startTime = DateTime.Now;
        private SemaphoreSlim _cloudVideoUpLoadSlim = new(2);
        private List<NvrCameraBindingInfoModel> _nvrCameraBindingInfoModels = new();
        private SemaphoreSlim _setNvrCameraBindingSlim = new(1);
        private ConcurrentQueue<SavedImageInfo> _savedImageItems = new();
        private static bool _isWindowsClose;

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
            ISortingInstructionBindingRepository sortingInstructionBindingRepository) {
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

            EventAggregator.Instance.Subscribe<SettingsChangedEvent>(async item => {
                switch (item) {
                    case SettingsChangedEvent { SettingsName: "CloudVideoSettings" } model: {
                            _cloudVideoSettingsDto = await _configRepository.FirstOrDefaultEntity<CloudVideoSettingsDto>(model.SettingsName) ?? new CloudVideoSettingsDto();
                            _cloudVideoUpLoadSlim = new SemaphoreSlim(_cloudVideoSettingsDto.Concurrency);
                            if (_cloudVideoSettingsDto.IsAutoUploadUnsyncedData) {
                                _startTime = new DateTime(1970, 1, 1);
                            }
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseCloudSync: true } &&
                                model.IsLocallySaved) {
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(model.SettingsName, _cloudVideoSettingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case SettingsChangedEvent { SettingsName: "SyncSettingsSettings" } syncSettingsSettings: {
                            _syncSettingsDto = await _configRepository.FirstOrDefaultEntity<SyncSettingsDto>(syncSettingsSettings.SettingsName) ?? new SyncSettingsDto();
                            break;
                        }
                    case SettingsChangedEvent { SettingsName: "NvrCameraBindingInfoModel" }:
                        try {
                            await _setNvrCameraBindingSlim.WaitAsync();
                            _nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.Select(s => s.Id > 0,
                                o => o.Id);
                        }
                        catch (Exception e) {
                            NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                        }
                        finally {
                            _setNvrCameraBindingSlim.Release();
                        }

                        break;

                    case SettingsChangedEvent { SettingsName: "ApiSettings", IsLocallySaved: true } apiSettings: {
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
                    case SettingsChangedEvent { SettingsName: "CaiNiaoApiParameters", IsLocallySaved: true } caiNiaoApiParameters: {
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
                    case SettingsChangedEvent { SettingsName: "EshippingitApiParameters", IsLocallySaved: true } eshippingitApiParameters: {
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
                    case SettingsChangedEvent { SettingsName: "JtExpressApiParameters", IsLocallySaved: true } jtExpressApiParameters: {
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
                    case SettingsChangedEvent { SettingsName: "RoutDataApiParameters", IsLocallySaved: true } routDataApiParameters: {
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
                    case SettingsChangedEvent { SettingsName: "SzjyApiParameters", IsLocallySaved: true } szjyApiParameters: {
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
                    case SettingsChangedEvent { SettingsName: "WdtFlagshipApiParameters", IsLocallySaved: true } wdtFlagshipApiParameters: {
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
                    case SettingsChangedEvent { SettingsName: "WdtWmsApiParameters", IsLocallySaved: true } wdtWmsApiParameters: {
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
                    case SettingsChangedEvent { SettingsName: "SaveImageSettings", IsLocallySaved: true } saveImageSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseImageStorageSync: true }) {
                                var settingsDto = await _configRepository.FirstOrDefaultEntity<ImageSettingsDto>(saveImageSettings.SettingsName) ?? new ImageSettingsDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(saveImageSettings.SettingsName, settingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case SettingsChangedEvent { SettingsName: "BarcodeFilterSettings", IsLocallySaved: true } barcodeFilterSettings: {
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
                    case SettingsChangedEvent { SettingsName: "ContentInputSettings", IsLocallySaved: true } contentInputSettings: {
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
                    case SettingsChangedEvent { SettingsName: "CreatePackageSettings", IsLocallySaved: true } createPackageSettings: {
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
                    case SettingsChangedEvent { SettingsName: "OcrSettings", IsLocallySaved: true } ocrSettings: {
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
                    case SettingsChangedEvent { SettingsName: "CacheClearSettings", IsLocallySaved: true } cacheClearSettings: {
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
                    case SettingsChangedEvent { SettingsName: "ApiSortingItemsSettings", IsLocallySaved: true } apiSortingItemsSettings: {
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
                    case SettingsChangedEvent { SettingsName: "BarcodeSortingItemsSettings", IsLocallySaved: true } barcodeSortingItemsSettings: {
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
                    case SettingsChangedEvent { SettingsName: "LogisticsSortingItemsSettings", IsLocallySaved: true } logisticsSortingItemsSettings: {
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
                    case SettingsChangedEvent { SettingsName: "OcrSortingItemsSettings", IsLocallySaved: true } ocrSortingItemsSettings: {
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
                    case SettingsChangedEvent { SettingsName: "VolumeSortingItemsSettings", IsLocallySaved: true } volumeSortingItemsSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var volumeSortingInfoModels = await _volumeSortingRepository.VolumeSortingItems(s => s.Id > 0);
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(volumeSortingItemsSettings.SettingsName,
JsonConvert.SerializeObject(volumeSortingInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case SettingsChangedEvent { SettingsName: "WeightSortingItemsSettings", IsLocallySaved: true } weightSortingItemsSettings: {
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
                    case SettingsChangedEvent { SettingsName: "CommunicationsItemsSettings", IsLocallySaved: true } communicationsItemsSettings: {
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
                    case SettingsChangedEvent { SettingsName: "LogisticsCodeRecognitionItemSettings", IsLocallySaved: true } logisticsCodeRecognitionItemSettings: {
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
                    case SettingsChangedEvent { SettingsName: "PackageExitDefinitionItemSettings", IsLocallySaved: true } packageExitDefinitionItemSettings: {
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
                    case SettingsChangedEvent { SettingsName: "PackageExitLockSettings", IsLocallySaved: true } packageExitLockSettings: {
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
                    case SettingsChangedEvent { SettingsName: "PackageExitLockBindingItemSettings", IsLocallySaved: true } packageExitLockBindingItemSettings: {
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
                    case SettingsChangedEvent { SettingsName: "SortingInstructionBindingItemSettings", IsLocallySaved: true } sortingInstructionBindingItemSettings: {
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
                    case SettingsChangedEvent { SettingsName: "StackedPackageDetectionSettings", IsLocallySaved: true } stackedPackageDetectionSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseInstructionSync: true }) {
                                var stackedPackageDetectionSettingsDto = await _configRepository.FirstOrDefaultEntity<StackedPackageDetectionSettingsDto>(stackedPackageDetectionSettings.SettingsName) ?? new StackedPackageDetectionSettingsDto();

                                var (key, value) = await _syncSettingsService.SubmitSyncContent(stackedPackageDetectionSettings.SettingsName, stackedPackageDetectionSettingsDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                    case SettingsChangedEvent { SettingsName: "SortingMethodSettings", IsLocallySaved: true } sortingMethodSettings: {
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
                    case SettingsChangedEvent { SettingsName: "AlgorithmSettings", IsLocallySaved: true } algorithmSettings: {
                            //同步
                            if (_syncSettingsService.IsConnected &&
                                _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }) {
                                var barcodeReaderDto = await _configRepository.FirstOrDefaultEntity<UsbBarcodeReaderDto>(algorithmSettings.SettingsName) ?? new UsbBarcodeReaderDto();
                                var (key, value) = await _syncSettingsService.SubmitSyncContent(algorithmSettings.SettingsName, barcodeReaderDto);
                                if (!key) {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                }
                            }

                            break;
                        }
                }
            });
            EventAggregator.Instance.Subscribe<WindowsAction>(async item => {
                if (item is WindowsAction { Type: WindowsActionType.Close }) {
                    _isWindowsClose = true;
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
                        case "RoutDataApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "SzjyApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "WdtFlagshipApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "WdtWmsApiParameters" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true }:
                        case "SaveImageSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseImageStorageSync: true }:
                        case "BarcodeFilterSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseFilterSync: true }:
                        case "ContentInputSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseContentInputSync: true }:
                        case "CreatePackageSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUsePackagingSync: true }:
                        case "OcrSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseOcrSync: true }:
                        case "CacheClearSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSpaceCleaningSync: true }:
                        case "PackageExitLockSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true }:
                        case "StackedPackageDetectionSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseStackingSync: true }:
                        case "SortingMethodSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
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
                        case "OcrSortingInfoModel" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }: {
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
            _cloudVideoSettingsDto = await _configRepository.FirstOrDefaultEntity<CloudVideoSettingsDto>("CloudVideoSettings", stoppingToken) ??
                                     new CloudVideoSettingsDto();
            _cloudVideoUpLoadSlim = new SemaphoreSlim(_cloudVideoSettingsDto.Concurrency);
            if (_cloudVideoSettingsDto.IsAutoUploadUnsyncedData) {
                _startTime = new DateTime(1970, 1, 1);
            }

            _syncSettingsDto = await _configRepository.FirstOrDefaultEntity<SyncSettingsDto>("SyncSettingsSettings", stoppingToken) ??
                 new SyncSettingsDto();

            _nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.Select(s => s.Id > 0,
                o => o.Id, stoppingToken);

            while (!stoppingToken.IsCancellationRequested && !_isWindowsClose) {
                //设置参数
                //提交到云端
                if (_cloudVideoSettingsDto.IsUseCloudVideoUpload) {
                    if (_cloudVideoUpLoadSlim.CurrentCount > 0) {
                        var (key, value) = await _packageRepository.SelectPackage(s =>
                            s.BarCodeInfo != null &&
                                s.BarCodeInfo.ScanTime.CompareTo(_startTime) > 0 &&
                                s.BarCodeInfo.ScanTime.CompareTo(
                                    DateTime.Now.AddSeconds(0 - _cloudVideoSettingsDto.UploadIntervalInSeconds)) <= 0 &&
                                (s.CloudVideoUploadInfo == null || s.CloudVideoUploadInfo.UploadTime == null),
                            o => o.PackageCreateTime, 0,
                            _cloudVideoSettingsDto.Concurrency, stoppingToken);

                        if (key && value is { } packageInfoModels) {
                            if (packageInfoModels?.Where(w => w.BarCodeInfo != null)?.Any() == true) {
                                foreach (var packageInfoModel in packageInfoModels?.Where(w => w.BarCodeInfo != null)?.ToList()!) {
                                    PolicyVideoUpLoad(packageInfoModel, stoppingToken);
                                }

                                _startTime = packageInfoModels.Max(m => m.BarCodeInfo.ScanTime);
                            }
                        }
                    }
                }
                await Task.Delay(50, stoppingToken);
            }
        }

        private async void PolicyVideoUpLoad(PackageInfoModel packageInfoModel, CancellationToken token) {
            try {
                await _cloudVideoUpLoadSlim.WaitAsync(token);
                var retryPolicy = Policy.HandleResult<bool>(result => !result)
               .Or<Exception>().RetryAsync(_cloudVideoSettingsDto.RetryAttempts, (a, b) => {
                   EventAggregator.Instance.Publish(new CloudVideoUploadRetryMessage {
                       Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                       RetryCount = b
                   });
               });
                await retryPolicy.ExecuteAsync(async () => {
                    //获取数据
                    //创建多线程

                    //位置输出*/
                    var (key, value) = await _cloud.SetParameters(new Dictionary<string, object>() {
                    { "WebDoMain", _cloudVideoSettingsDto.WebDoMain },
                    { "Timeout", _cloudVideoSettingsDto.RequestTimeout },
                    });
                    if (key) {
                        var cameraSerialNumber = packageInfoModel.ImageInfos?.FirstOrDefault(f => f.Type == 0)?.CameraSerialNumber;
                        //取出绑定信息
                        List<NvrCameraBindingInfoModel> nvrCameraBindingInfoModels;
                        try {
                            await _setNvrCameraBindingSlim.WaitAsync(token);
                            nvrCameraBindingInfoModels = _nvrCameraBindingInfoModels.Where(f => !string.IsNullOrEmpty(cameraSerialNumber)
                                                                                                                                 && f.BarcodeScannerSerialNumber.Equals(
                                                                                                                                     cameraSerialNumber))?.ToList() ??
                                                                                          new List<NvrCameraBindingInfoModel>();
                        }
                        finally {
                            _setNvrCameraBindingSlim.Release();
                        }

                        /*
                        var cloudUploadResponse = await _cloud.UploadData(packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                            packageInfoModel.BarCodeInfo?.ScanTime ?? DateTime.Now, packageInfoModel.WeightInfo?.FormattedWeight ?? 0,
                            _cloudVideoSettingsDto.NodeName,
                            null, packageInfoModel.ImageInfos?.Select(s =>
                                new CloudUploadImageInfo {
                                    CameraSerialNumber = s.CameraSerialNumber,
                                    CameraName = s.CameraName,
                                    CustomCameraName = s.CustomCameraName,
                                    Type = s.Type,
                                    Image = File.Exists(s.LocalPath) ? Image.FromFile(s.LocalPath) : null
                                })?.ToList(), nvrCameraBindingInfos: nvrCameraBindingInfoModels.Select(nvr =>
                           new CloudNvrCameraBindingInfo {
                               BarcodeScannerSerialNumber = nvr.BarcodeScannerSerialNumber,
                               Channel = nvr.Channel,
                               IpAddress = nvr.IpAddress,
                               Password = nvr.Password,
                               Port = nvr.Port,
                               Username = nvr.Username
                           })?.ToList() ?? new List<CloudNvrCameraBindingInfo>(), token: token);
                           */

                        var cloudUploadResponse = await _cloud.UploadData(new PackageCloudInfo() {
                            PackageCreateTime = packageInfoModel.PackageCreateTime,
                            PackageTimestamped = packageInfoModel.PackageTimestamped,
                            BarCodeInfo = new PackageCloudBarCodeInfo() {
                                Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                                CameraSerialNumber = packageInfoModel.BarCodeInfo?.CameraSerialNumber ?? string.Empty,
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
                                AbnormalSortingType = (PackageCloudAbnormalSortingType)(packageInfoModel.SortingInfo?.AbnormalSortingType ?? AbnormalSortingType.None)
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
                                new PackageCloudImageInfo() {
                                    CameraSerialNumber = s.CameraSerialNumber,
                                    CameraName = s.CameraName,
                                    CustomCameraName = s.CustomCameraName,
                                    Type = s.Type,
                                    Image = File.Exists(s.LocalPath) ? Image.FromFile(s.LocalPath) : null
                                })?.ToList(),
                            DeviceInfos = new PackageCloudDeviceInfo() {
                                NodeName = _cloudVideoSettingsDto.NodeName,
                                MachineCode = await _computer.GenerateMachineCode(),
                            },
                            CloudNvrCameraBindingInfos = nvrCameraBindingInfoModels?.Select(s =>
                                new PackageCloudNvrCameraBindingInfo {
                                    BarcodeScannerSerialNumber = s.BarcodeScannerSerialNumber,
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
                                cloudVideoUploadInfoModel.ScanImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0;
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
                    else {
                        return false;
                    }
                });
            }
            finally {
                _cloudVideoUpLoadSlim.Release();
            }
        }

        //重试方法
    }
}