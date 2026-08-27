using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.SortingInstructions;
using JayTom.Dws.Application.PackageExits;
using JayTom.Dws.Application.Communications;
using JayTom.Dws.Application.SortingConfigurations;
using JayTom.Dws.Models.LocalConf.IpcNvrConfig;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Application.CameraConfigurations;
using JayTom.Dws.Application.PackageHistory;
using Polly;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Polly.Retry;
using System.Drawing;
using Newtonsoft.Json;
using System.Threading;
using JayTom.Dws.Legacy.Contracts.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Models.Package;
using JayTom.Dws.Legacy.Contracts.Model;
using JayTom.Dws.Models.LocalLog;
using JayTom.Dws.Models.LocalData;
using JayTom.Dws.Models.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Integrations.Cloud;
using JayTom.Dws.Legacy.Contracts.Dto.AppDto;
using JayTom.Dws.Legacy.Contracts.Dto.ApiDto;
using System.Collections.Concurrent;
using JayTom.Dws.Legacy.Contracts.Dto.CloudDto;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalData;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Legacy.Contracts.Dto.PackageExitLockDto;
using JayTom.Dws.Legacy.Contracts.Dto.CameraConfiguration;
using JayTom.Dws.Models.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CloudConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.CameraConfig;
using ApiExceptionType = JayTom.Dws.Integrations.Contracts.ApiExceptionType;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig;
using InstructionType = JayTom.Dws.Integrations.Cloud.InstructionType;
using RemoteAction = JayTom.Dws.Application.Events.RemoteAction;
using RemoteCommand = JayTom.Dws.Application.Events.RemoteCommand;
using WindowsAction = JayTom.Dws.Client.Events.WindowsAction;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.RuleConfig;
using WindowsActionType = JayTom.Dws.Client.Events.WindowsActionType;
using JayTom.Dws.Legacy.Contracts.Repositories.LocalConf.PackageSortingConfig.ConnectionParams;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.Service.BackgroundService
{

    public class CloudBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly ISettingsStore _settingsStore;
        private readonly ICloud _cloud;
        private readonly ICloudVideoTransferQueue _cloudVideoTransfers;
        private readonly ICameraConfigurationCatalog<NvrCameraBindingInfoModel> _nvrCameraBindingRepository;
        private readonly IComputer _computer;
        private readonly ISyncSettingsService _syncSettingsService;
        private readonly ISortingConfigurationCatalog<ApiSortingInfoModel> _apiSortingRepository;
        private readonly ISortingConfigurationCatalog<BarCodeSortingInfoModel> _barCodeSortingRepository;
        private readonly ISortingConfigurationCatalog<LogisticsSortingInfoModel> _logisticsSortingRepository;
        private readonly ISortingConfigurationCatalog<OcrSortingInfoModel> _ocrSortingRepository;
        private readonly ISortingConfigurationCatalog<VolumeSortingInfoModel> _volumeSortingRepository;
        private readonly ISortingConfigurationCatalog<WeightSortingInfoModel> _weightSortingRepository;
        private readonly ICommunicationConfigurationCatalog _communicationConnectionConfigRepository;
        private readonly ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> _logisticsCodeRecognitionRepository;
        private readonly IPackageExitManagement _packageExitDefinitionRepository;
        private readonly IPackageExitLockBindingCatalog _packageExitLockBindingRepository;
        private readonly ISortingInstructionBindingCatalog _sortingInstructionBindingRepository;
        private readonly ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> _barcodeScannerCameraConfigRepository;
        private CloudVideoSettingsDto _cloudVideoSettingsDto = new();
        private SyncSettingsDto _syncSettingsDto = new();
        private long _startTimeTicks = DateTime.Now.Ticks;
        private SemaphoreSlim _cloudVideoUpLoadSlim = new(2);
        /// <summary>
        /// 等待服务停止后统一释放的旧上传并发门。
        /// </summary>
        private readonly ConcurrentQueue<SemaphoreSlim> _retiredCloudVideoUploadGates = new();
        private NvrCameraBindingInfoModel[] _nvrCameraBindingInfoModels =
            [];
        private readonly SemaphoreSlim _settingsUpdateGate = new(1, 1);
        private int _isWindowsClose;

        public CloudBackgroundService(ISettingsStore settingsStore,
            ICloud cloud,
            ICloudVideoTransferQueue cloudVideoTransfers,
            ICameraConfigurationCatalog<NvrCameraBindingInfoModel> nvrCameraBindingRepository,
            IComputer computer,
            ISyncSettingsService syncSettingsService,
            ISortingConfigurationCatalog<ApiSortingInfoModel> apiSortingRepository,
            ISortingConfigurationCatalog<BarCodeSortingInfoModel> barCodeSortingRepository,
            ISortingConfigurationCatalog<LogisticsSortingInfoModel> logisticsSortingRepository,
            ISortingConfigurationCatalog<OcrSortingInfoModel> ocrSortingRepository,
            ISortingConfigurationCatalog<VolumeSortingInfoModel> volumeSortingRepository,
            ISortingConfigurationCatalog<WeightSortingInfoModel> weightSortingRepository,
            ICommunicationConfigurationCatalog communicationConnectionConfigRepository,
            ISortingConfigurationCatalog<LogisticsCodeRecognitionInfoModel> logisticsCodeRecognitionRepository,
            IPackageExitManagement packageExitDefinitionRepository,
            IPackageExitLockBindingCatalog packageExitLockBindingRepository,
            ISortingInstructionBindingCatalog sortingInstructionBindingRepository,
            ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> barcodeScannerCameraConfigRepository,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _settingsStore = settingsStore;
            _cloud = cloud;
            _cloudVideoTransfers = cloudVideoTransfers;
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

            _eventBus.SubscribeAsync<SettingsChangedEvent>(async item =>
            {
                await _settingsUpdateGate.WaitAsync();
                try
                {
                    switch (item)
                    {
                        case { SettingsName: "CloudVideoSettings" } model:
                            {
                                var settings = await _settingsStore
                                    .GetAsync<CloudVideoSettingsDto>(model.SettingsName) ??
                                    new CloudVideoSettingsDto();
                                Volatile.Write(ref _cloudVideoSettingsDto, settings);
                                ReplaceCloudVideoUploadGate(settings.Concurrency);
                                if (settings.IsAutoUploadUnsyncedData)
                                {
                                    Interlocked.Exchange(
                                        ref _startTimeTicks,
                                        new DateTime(1970, 1, 1).Ticks);
                                }
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseCloudSync: true } &&
                                    model.IsLocallySaved)
                                {
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(model.SettingsName, settings);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "SyncSettingsSettings" } syncSettingsSettings:
                            {
                                Volatile.Write(
                                    ref _syncSettingsDto,
                                    await _settingsStore.GetAsync<SyncSettingsDto>(
                                        syncSettingsSettings.SettingsName) ?? new SyncSettingsDto());
                                break;
                            }
                        case { SettingsName: "NvrCameraBindingInfoModel" }:
                            try
                            {
                                Volatile.Write(
                                    ref _nvrCameraBindingInfoModels,
                                    [.. (await _nvrCameraBindingRepository.MemoryCacheData())]);
                            }
                            catch (Exception e)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                            }

                            break;

                        case { SettingsName: "ApiSettings", IsLocallySaved: true } apiSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var apiSettingsDto = await _settingsStore.GetAsync<ApiSettingsDto>(apiSettings.SettingsName) ?? new ApiSettingsDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(apiSettings.SettingsName, apiSettingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "CaiNiaoApiParameters", IsLocallySaved: true } caiNiaoApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var caiNiaoApiDto = await _settingsStore.GetAsync<CaiNiaoApiDto>(caiNiaoApiParameters.SettingsName) ?? new CaiNiaoApiDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(caiNiaoApiParameters.SettingsName, caiNiaoApiDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "EshippingitApiParameters", IsLocallySaved: true } eshippingitApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<EshippingitApiDto>(eshippingitApiParameters.SettingsName) ?? new EshippingitApiDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(eshippingitApiParameters.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "JtExpressApiParameters", IsLocallySaved: true } jtExpressApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<JtExpressDto>(jtExpressApiParameters.SettingsName) ?? new JtExpressDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(jtExpressApiParameters.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "JtPolarDayApiParameters", IsLocallySaved: true } jtPolarDayApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<JtPolarDayDto>(jtPolarDayApiParameters.SettingsName) ?? new JtPolarDayDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(jtPolarDayApiParameters.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "RoutDataApiParameters", IsLocallySaved: true } routDataApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<RoutDataApiDto>(routDataApiParameters.SettingsName) ?? new RoutDataApiDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(routDataApiParameters.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "SzjyApiParameters", IsLocallySaved: true } szjyApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<SzjyApiDto>(szjyApiParameters.SettingsName) ?? new SzjyApiDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(szjyApiParameters.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "WdtFlagshipApiParameters", IsLocallySaved: true } wdtFlagshipApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<WdtFlagshipApiDto>(wdtFlagshipApiParameters.SettingsName) ?? new WdtFlagshipApiDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(wdtFlagshipApiParameters.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "WdtWmsApiParameters", IsLocallySaved: true } wdtWmsApiParameters:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseApiSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<WdtWmsApiDto>(wdtWmsApiParameters.SettingsName) ?? new WdtWmsApiDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(wdtWmsApiParameters.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "SaveImageSettings", IsLocallySaved: true } saveImageSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseImageStorageSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<ImageSettingsDto>(saveImageSettings.SettingsName) ?? new ImageSettingsDto();

                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(saveImageSettings.SettingsName, JsonConvert.SerializeObject(settingsDto));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "BarcodeFilterSettings", IsLocallySaved: true } barcodeFilterSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseFilterSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<BarcodeFilterSettingsDto>(barcodeFilterSettings.SettingsName) ?? new BarcodeFilterSettingsDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(barcodeFilterSettings.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "ContentInputSettings", IsLocallySaved: true } contentInputSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseContentInputSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<ContentInputSettingsDto>(contentInputSettings.SettingsName) ?? new ContentInputSettingsDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(contentInputSettings.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "CreatePackageSettings", IsLocallySaved: true } createPackageSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUsePackagingSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<CreatePackageSettingsDto>(createPackageSettings.SettingsName) ?? new CreatePackageSettingsDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(createPackageSettings.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "OcrSettings", IsLocallySaved: true } ocrSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseOcrSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<OcrSettingsDto>(ocrSettings.SettingsName) ?? new OcrSettingsDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(ocrSettings.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "CacheClearSettings", IsLocallySaved: true } cacheClearSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSpaceCleaningSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<CacheClearSettingsDto>(cacheClearSettings.SettingsName) ?? new CacheClearSettingsDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(cacheClearSettings.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "ApiSortingItemsSettings", IsLocallySaved: true } apiSortingItemsSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true })
                                {
                                    var apiSortingInfoModels = await _apiSortingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(apiSortingItemsSettings.SettingsName,
                                        JsonConvert.SerializeObject(apiSortingInfoModels,
                                        new JsonSerializerSettings()
                                        {
                                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                        }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "BarcodeSortingItemsSettings", IsLocallySaved: true } barcodeSortingItemsSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true })
                                {
                                    var barCodeSortingInfoModels = await _barCodeSortingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(barcodeSortingItemsSettings.SettingsName,
                                        JsonConvert.SerializeObject(barCodeSortingInfoModels,
                                            new JsonSerializerSettings()
                                            {
                                                PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                            }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "LogisticsSortingItemsSettings", IsLocallySaved: true } logisticsSortingItemsSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true })
                                {
                                    var logisticsSortingInfoModels = await _logisticsSortingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(logisticsSortingItemsSettings.SettingsName,
                                        JsonConvert.SerializeObject(logisticsSortingInfoModels,
                                            new JsonSerializerSettings()
                                            {
                                                PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                            }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "OcrSortingItemsSettings", IsLocallySaved: true } ocrSortingItemsSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true })
                                {
                                    var ocrSortingInfoModels = await _ocrSortingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(ocrSortingItemsSettings.SettingsName,
                                        JsonConvert.SerializeObject(ocrSortingInfoModels,
                                            new JsonSerializerSettings()
                                            {
                                                PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                            }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "VolumeSortingItemsSettings", IsLocallySaved: true } volumeSortingItemsSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true })
                                {
                                    var volumeSortingInfoModels = await _volumeSortingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(volumeSortingItemsSettings.SettingsName,
    JsonConvert.SerializeObject(volumeSortingInfoModels, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "WeightSortingItemsSettings", IsLocallySaved: true } weightSortingItemsSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true })
                                {
                                    var weightSortingInfoModels = await _weightSortingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(weightSortingItemsSettings.SettingsName, JsonConvert.SerializeObject(weightSortingInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "CommunicationsItemsSettings", IsLocallySaved: true } communicationsItemsSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseConnectionSync: true })
                                {
                                    var communicationConnectionConfigInfoModels = await _communicationConnectionConfigRepository.ListWithDetailsAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(communicationsItemsSettings.SettingsName,
                                        JsonConvert.SerializeObject(communicationConnectionConfigInfoModels,
                                        new JsonSerializerSettings()
                                        {
                                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                        }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "LogisticsCodeRecognitionItemSettings", IsLocallySaved: true } logisticsCodeRecognitionItemSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseLogisticsSync: true })
                                {
                                    var logisticsCodeRecognitionInfoModels = await _logisticsCodeRecognitionRepository.ListAsync();

                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(logisticsCodeRecognitionItemSettings.SettingsName, JsonConvert.SerializeObject(logisticsCodeRecognitionInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "PackageExitDefinitionItemSettings", IsLocallySaved: true } packageExitDefinitionItemSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseExitSync: true })
                                {
                                    var packageExitDefinitionInfoModels = await _packageExitDefinitionRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(packageExitDefinitionItemSettings.SettingsName, JsonConvert.SerializeObject(packageExitDefinitionInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "PackageExitLockSettings", IsLocallySaved: true } packageExitLockSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true })
                                {
                                    var settingsDto = await _settingsStore.GetAsync<PackageExitLockSettingsDto>(packageExitLockSettings.SettingsName) ?? new PackageExitLockSettingsDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(packageExitLockSettings.SettingsName, settingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "PackageExitLockBindingItemSettings", IsLocallySaved: true } packageExitLockBindingItemSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true })
                                {
                                    var packageExitLockBindingInfoModels = await _packageExitLockBindingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(packageExitLockBindingItemSettings.SettingsName,
                                        JsonConvert.SerializeObject(packageExitLockBindingInfoModels,
                                        new JsonSerializerSettings()
                                        {
                                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                                        }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "SortingInstructionBindingItemSettings", IsLocallySaved: true } sortingInstructionBindingItemSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseInstructionSync: true })
                                {
                                    var sortingInstructionBindingInfoModels = await _sortingInstructionBindingRepository.ListAsync();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(sortingInstructionBindingItemSettings.SettingsName, JsonConvert.SerializeObject(sortingInstructionBindingInfoModels, new JsonSerializerSettings() { PreserveReferencesHandling = PreserveReferencesHandling.Objects }));
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "StackedPackageDetectionSettings", IsLocallySaved: true } stackedPackageDetectionSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseStackingSync: true })
                                {
                                    var stackedPackageDetectionSettingsDto = await _settingsStore.GetAsync<StackedPackageDetectionSettingsDto>(stackedPackageDetectionSettings.SettingsName) ?? new StackedPackageDetectionSettingsDto();

                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(stackedPackageDetectionSettings.SettingsName, stackedPackageDetectionSettingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "SupplyCounterSettings", IsLocallySaved: true } supplyCounterSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSupplyCounterSync: true })
                                {
                                    var supplyCounterSettingsDto = await _settingsStore.GetAsync<SupplyCounterSettingsDto>(supplyCounterSettings.SettingsName) ?? new SupplyCounterSettingsDto();

                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(supplyCounterSettings.SettingsName, supplyCounterSettingsDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "SortingMethodSettings", IsLocallySaved: true } sortingMethodSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true })
                                {
                                    var sortingMethodDto = await _settingsStore.GetAsync<SortingMethodDto>(sortingMethodSettings.SettingsName) ?? new SortingMethodDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(sortingMethodSettings.SettingsName, sortingMethodDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                        case { SettingsName: "AlgorithmSettings", IsLocallySaved: true } algorithmSettings:
                            {
                                //同步
                                if (_syncSettingsService.IsConnected &&
                                    _syncSettingsDto is { IsUseSyncSettings: true, IsUseAlgorithmSync: true })
                                {
                                    var barcodeReaderDto = await _settingsStore.GetAsync<UsbBarcodeReaderDto>(algorithmSettings.SettingsName) ?? new UsbBarcodeReaderDto();
                                    var (key, value) = await _syncSettingsService.SubmitSyncContent(algorithmSettings.SettingsName, barcodeReaderDto);
                                    if (!key)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"提交同步失败!");
                                    }
                                }

                                break;
                            }
                    }
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger()
                        .Error(e, $"更新云端同步配置失败:{item.SettingsName}");
                }
                finally
                {
                    _settingsUpdateGate.Release();
                }
            });
            _eventBus.Subscribe<WindowsAction>(item =>
            {
                if (item is { Type: WindowsActionType.Close })
                {
                    Interlocked.Exchange(ref _isWindowsClose, 1);
                }
            });
            _syncSettingsService.SyncContentReceived += async (sender, info) =>
            {
                try
                {
                    _eventBus.Publish(new RemoteAction
                    {
                        Command = RemoteCommand.Stop
                    });

                    switch (info.SettingsName)
                    {
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
                            await _settingsStore.SaveAsync(info.SettingsName,info.SettingsInfo);
                            break;

                        case "ApiSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<ApiSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _apiSortingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }

                                break;
                            }
                        case "BarcodeSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<BarCodeSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _barCodeSortingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }

                                break;
                            }
                        case "LogisticsSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<LogisticsSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _logisticsSortingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "OcrSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<OcrSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _ocrSortingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "VolumeSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<VolumeSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _volumeSortingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "WeightSortingItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseSortingModeSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<WeightSortingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _weightSortingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "CommunicationsItemsSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseConnectionSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<CommunicationConnectionConfigInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _communicationConnectionConfigRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "LogisticsCodeRecognitionItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseLogisticsSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<LogisticsCodeRecognitionInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _logisticsCodeRecognitionRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "PackageExitDefinitionItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseExitSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<PackageExitDefinitionInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _packageExitDefinitionRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "PackageExitLockBindingItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseLockerExitSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<PackageExitLockBindingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _packageExitLockBindingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                        case "SortingInstructionBindingItemSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseInstructionSync: true }:
                            {
                                try
                                {
                                    var models = JsonConvert.DeserializeObject<List<SortingInstructionBindingInfoModel>>(info.SettingsInfo?.ToString() ?? string.Empty);
                                    if (models is not null)
                                    {
                                        await _sortingInstructionBindingRepository.SyncAsync(models);
                                    }
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }

                        case "SaveImageSettings" when _syncSettingsDto is { IsUseSyncSettings: true, IsUseImageStorageSync: true }:
                            {
                                try
                                {
                                    await _settingsStore.SaveRawAsync(info.SettingsName,info.SettingsInfo?.ToString() ?? string.Empty
);
                                }
                                catch (Exception e)
                                {
                                    NLog.LogManager.GetCurrentClassLogger()
                                        .Error(e, $"远程同步配置失败:{info.SettingsName}");
                                }
                                break;
                            }
                    }
                    _eventBus.Publish(new SettingsChangedEvent
                    {
                        SettingsName = info.SettingsName,
                    });
                    _eventBus.Publish(new AppLogInfoModel
                    {
                        CreateTime = DateTime.Now,
                        Message = $"远程更新配置:{info.SettingsName}",
                        Type = LogType.Information
                    });
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                    _eventBus.Publish(new AppLogInfoModel
                    {
                        CreateTime = DateTime.Now,
                        Message = $"远程更新配置:{info.SettingsName}失败,异常:{e.Message}",
                        Type = LogType.Information
                    });
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var initialSettings =
                await _settingsStore.GetAsync<CloudVideoSettingsDto>(
                    "CloudVideoSettings", stoppingToken) ??
                new CloudVideoSettingsDto();
            Volatile.Write(ref _cloudVideoSettingsDto, initialSettings);
            ReplaceCloudVideoUploadGate(initialSettings.Concurrency);
            if (initialSettings.IsAutoUploadUnsyncedData)
            {
                Interlocked.Exchange(ref _startTimeTicks, new DateTime(1970, 1, 1).Ticks);
            }

            Volatile.Write(
                ref _syncSettingsDto,
                await _settingsStore.GetAsync<SyncSettingsDto>(
                    "SyncSettingsSettings", stoppingToken) ?? new SyncSettingsDto());
            Volatile.Write(
                ref _nvrCameraBindingInfoModels,
                [.. (await _nvrCameraBindingRepository.MemoryCacheData())]);

            // 这是低频对账工作器，不属于设备/分拣热路径。业务热回调不访问数据库和文件。
            using var reconciliationTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            while (Volatile.Read(ref _isWindowsClose) == 0 &&
                   await reconciliationTimer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var settings = Volatile.Read(ref _cloudVideoSettingsDto);
                    if (settings.IsUseCloudVideoUpload)
                    {
                        var uploadGate = Volatile.Read(ref _cloudVideoUpLoadSlim);
                        if (uploadGate.CurrentCount > 0)
                        {
                            var startTime = new DateTime(
                                Interlocked.Read(ref _startTimeTicks),
                                DateTimeKind.Local);
                            var packageInfoModels = await _cloudVideoTransfers.ListPendingAsync(
                                startTime,
                                DateTime.Now.AddSeconds(-settings.UploadIntervalInSeconds),
                                Math.Max(1, settings.Concurrency),
                                stoppingToken);

                            if (packageInfoModels.Count > 0)
                            {
                                var pendingPackages = packageInfoModels
                                    .Where(package => package.BarCodeInfo != null)
                                    .ToArray();
                                if (pendingPackages.Length > 0)
                                {
                                    await Task.WhenAll(pendingPackages.Select(package =>
                                        PolicyVideoUploadAsync(
                                            package,
                                            settings,
                                            uploadGate,
                                            stoppingToken)));
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                }
            }
        }

        /// <summary>
        /// 原子替换云视频上传并发门，并延迟释放仍可能被在途任务使用的旧实例。
        /// </summary>
        private void ReplaceCloudVideoUploadGate(int concurrency)
        {
            var previousGate = Interlocked.Exchange(
                ref _cloudVideoUpLoadSlim,
                new SemaphoreSlim(Math.Max(1, concurrency)));
            _retiredCloudVideoUploadGates.Enqueue(previousGate);
        }

        /// <summary>
        /// 停止云服务并释放当前及已退役的并发门。
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _isWindowsClose, 1);
            await base.StopAsync(cancellationToken);
            while (_retiredCloudVideoUploadGates.TryDequeue(out var retiredGate))
            {
                retiredGate.Dispose();
            }
            Volatile.Read(ref _cloudVideoUpLoadSlim).Dispose();
        }

        private async Task PolicyVideoUploadAsync(
            PackageInfoModel packageInfoModel,
            CloudVideoSettingsDto settings,
            SemaphoreSlim uploadGate,
            CancellationToken token)
        {
            var gateEntered = false;
            try
            {
                await uploadGate.WaitAsync(token);
                gateEntered = true;
                var retryPolicy = Policy.HandleResult<bool>(result => !result)
                    .Or<Exception>()
                    .WaitAndRetryAsync(
                        Math.Max(0, settings.RetryAttempts),
                        retryAttempt => TimeSpan.FromMilliseconds(
                            Math.Min(250 * Math.Pow(2, retryAttempt - 1), 5000)),
                        (outcome, delay, retryCount, context) =>
                        {
                            _eventBus.Publish(new CloudVideoUploadRetryMessage
                            {
                                Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                                RetryCount = retryCount
                            });
                        });
                await retryPolicy.ExecuteAsync(async () =>
                {
                    var (key, value) = await _cloud.SetParameters(new Dictionary<string, object>()
                    {
                        { "WebDoMain", settings.WebDoMain },
                        { "Timeout", settings.RequestTimeout },
                    });
                    if (key)
                    {
                        //取出绑定信息
                        var serialNumber = packageInfoModel.BarCodeInfo?.SerialNumber;
                        var nvrCameraBindingInfoModels = Volatile
                            .Read(ref _nvrCameraBindingInfoModels)
                            .Where(binding => binding.SerialNumber.Equals(
                                serialNumber,
                                StringComparison.Ordinal))
                            .ToArray();

                        var cloudUploadResponse = await UploadCloudDataAsync(new PackageCloudInfo()
                        {
                            PackageCreateTime = packageInfoModel.PackageCreateTime,
                            PackageTimestamped = packageInfoModel.PackageTimestamped,
                            BarCodeInfo = new PackageCloudBarCodeInfo()
                            {
                                Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                                SerialNumber = packageInfoModel.BarCodeInfo?.SerialNumber ?? string.Empty,
                                DisplayIdentifier = packageInfoModel.BarCodeInfo?.DisplayIdentifier ?? string.Empty,
                                ScanTime = packageInfoModel.BarCodeInfo?.ScanTime ?? DateTime.Now,
                                Source = (int)(packageInfoModel.BarCodeInfo?.Source ?? 0),
                            },
                            WeightInfo = new PackageCloudWeightInfo()
                            {
                                CreateTime = packageInfoModel.WeightInfo?.CreateTime ?? DateTime.MinValue,
                                FormattedWeight = packageInfoModel.WeightInfo?.FormattedWeight ?? 0,
                                OriginalText = packageInfoModel.WeightInfo?.OriginalText ?? string.Empty,
                                SourceType = (int)(packageInfoModel.WeightInfo?.SourceType ?? 0),
                                WeighingMode = (int)(packageInfoModel.WeightInfo?.WeighingMode ?? 0),
                            },
                            VolumeInfo = new PackageCloudVolumeInfo()
                            {
                                CreateTime = packageInfoModel.VolumeInfo?.CreateTime ?? DateTime.MinValue,
                                FormattedHeight = packageInfoModel.VolumeInfo?.FormattedHeight ?? 0,
                                FormattedWidth = packageInfoModel.VolumeInfo?.FormattedWidth ?? 0,
                                FormattedVolume = packageInfoModel.VolumeInfo?.FormattedVolume ?? 0,
                                FormattedLength = packageInfoModel.VolumeInfo?.FormattedLength ?? 0,
                                OriginalText = packageInfoModel.VolumeInfo?.OriginalText ?? string.Empty,
                                SourceType = (int)(packageInfoModel.VolumeInfo?.SourceType ?? 0),
                            },
                            UploadInfo = new PackageCloudUploadInfo()
                            {
                                ApiExceptionType = (ApiExceptionType)(packageInfoModel.UploadInfo?.ApiExceptionType ??
                                                                      JayTom.Dws.Models.Package.ApiExceptionType.None),
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
                            ExitInfo = new PackageCloudExitInfo()
                            {
                                PhysicalExit = packageInfoModel.ExitInfo?.PhysicalExit ?? string.Empty,
                                TheoreticalExit = packageInfoModel.ExitInfo?.TheoreticalExit ?? string.Empty,
                                PhysicalExitId = packageInfoModel.ExitInfo?.PhysicalExitId ?? 0,
                            },
                            SortingInfo = new PackageCloudSortingInfo()
                            {
                                ConnectionName = packageInfoModel.SortingInfo?.ConnectionName ?? string.Empty,
                                ChecksumProtocolName =
                                    packageInfoModel.SortingInfo?.ChecksumProtocolName ?? string.Empty,
                                CommunicationMethod = (int)(packageInfoModel.SortingInfo?.CommunicationMethod ?? 0),
                                IsCreatedByLowerMachine =
                                    packageInfoModel.SortingInfo?.IsCreatedByLowerMachine ?? false,
                                InstructionInfos = packageInfoModel.SortingInfo?.InstructionInfos?
                                    .Select(s => new PackageCloudInstructionInfo
                                    {
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
                            LogisticsInfo = new PackageCloudLogisticsInfo()
                            {
                                LogisticsCode = packageInfoModel.LogisticsInfo?.LogisticsCode ?? string.Empty,
                                LogisticsName = packageInfoModel.LogisticsInfo?.LogisticsName ?? string.Empty,
                            },
                            OcrInfo = new PackageCloudOcrInfo()
                            {
                                CameraSerialNumber = packageInfoModel.OcrInfo?.CameraSerialNumber ?? string.Empty,

                                ElapsedMilliseconds = packageInfoModel.OcrInfo?.ElapsedMilliseconds ?? 0,
                                SubmitTimestamp = packageInfoModel.OcrInfo?.SubmitTimestamp ?? 0,
                                ThreeSegmentCode = packageInfoModel.OcrInfo?.ThreeSegmentCode ?? string.Empty,
                                RecognizeTime = packageInfoModel.OcrInfo?.RecognizeTime ?? DateTime.MinValue,
                                VirtualNumberLast4 = packageInfoModel.OcrInfo?.VirtualNumberLast4 ?? string.Empty,
                                OriginalContent = packageInfoModel.OcrInfo?.OriginalContent ?? string.Empty,
                                OcrDetailedInfos = packageInfoModel.OcrInfo?.OcrDetailedInfos?.Select(s =>
                                    new PackageCloudOcrDetailedInfo()
                                    {
                                        Address = s.Address,
                                        InformationType = (int)s.InformationType,
                                        Name = s.Name,
                                        Phone = s.Phone,
                                    })?.ToList(),
                            },
                            ImageInfos = packageInfoModel.ImageInfos?.Select(s =>
                                new PackageCloudImageInfo
                                {
                                    CameraSerialNumber = s.CameraSerialNumber,
                                    CameraName = s.CameraName,
                                    CustomCameraName = s.CustomCameraName,
                                    Type = s.Type,
                                    Image = LoadImageSnapshot(s.LocalPath)
                                })?.ToList(),
                            DeviceInfo = new PackageCloudDeviceInfo()
                            {
                                NodeName = settings.NodeName,
                                MachineCode = await _computer.GenerateMachineCode(),
                            },
                            CloudNvrCameraBindingInfos = nvrCameraBindingInfoModels?.Select(s =>
                                new PackageCloudNvrCameraBindingInfo
                                {
                                    Channel = s.Channel,
                                    IpAddress = s.IpAddress,
                                    Password = s.Password,
                                    Port = s.Port,
                                    Username = s.Username
                                })?.ToList()
                        }, token: token);

                        _eventBus.Publish(new CloudVideoUploadMessage
                        {
                            Barcode = packageInfoModel.BarCodeInfo?.Barcode ?? string.Empty,
                            IsSuccessful = cloudUploadResponse.IsSuccessful,
                            PanoramaImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0,
                            ScanImageCount = packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                            ScanTime = packageInfoModel.BarCodeInfo?.ScanTime ?? DateTime.Now
                        });

                        if (cloudUploadResponse.IsSuccessful)
                        {
                            return await _cloudVideoTransfers.SaveReceiptAsync(
                                packageInfoModel.Id,
                                new CloudVideoUploadReceipt(
                                    cloudUploadResponse.ResponseContent,
                                    cloudUploadResponse.TargetAddress,
                                    cloudUploadResponse.UploadTime,
                                    cloudUploadResponse.UploadContent,
                                    cloudUploadResponse.UploadDuration,
                                    packageInfoModel.ImageInfos?.Count(c => c.Type == 0) ?? 0,
                                    packageInfoModel.ImageInfos?.Count(c => c.Type == 1) ?? 0),
                                token);
                        }
                        return false;
                    }

                    return false;
                });
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
            }
            finally
            {
                if (gateEntered)
                {
                    uploadGate.Release();
                }
            }
        }

        private async Task<CloudUploadResponse> UploadCloudDataAsync(
            PackageCloudInfo package,
            CancellationToken token)
        {
            try
            {
                return await _cloud.UploadData(package, token: token);
            }
            finally
            {
                if (package.ImageInfos is not null)
                {
                    foreach (var imageInfo in package.ImageInfos)
                    {
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
        private static Image? LoadImageSnapshot(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                using var source = Image.FromFile(path);
                return new Bitmap(source);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                      or ArgumentException)
            {
                NLog.LogManager.GetCurrentClassLogger()
                    .Warn(e, $"读取待上传图片失败:{path}");
                return null;
            }
        }
    }
}
