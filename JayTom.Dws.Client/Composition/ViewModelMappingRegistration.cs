using DryIoc;
using System;
using System.Linq;
using Example;
using Prism.Ioc;
using System.IO;
using Prism.Mvvm;
using Prism.DryIoc;
using System.Windows;
using JayTom.Dws.Ocr;
using JayTom.Dws.Nvr;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Net.Http;
using System.IO.Ports;
using System.Threading;
using JayTom.Dws.Camera;
using JayTom.Dws.Plugin;
using System.Diagnostics;
using JayTom.Dws.Nvr.Nvr;
using JayTom.Dws.Interface;
using System.Globalization;
using System.Windows.Media;
using JayTom.Dws.Plugin.Ftp;
using System.Threading.Tasks;
using System.Windows.Interop;
using JayTom.Dws.Client.Views;
using JayTom.Dws.Plugin.Excel;
using JayTom.Dws.Plugin.Speech;
using System.Windows.Threading;
using JayTom.Dws.Data.LocalLog;
using JayTom.Dws.Client.Service;
using JayTom.Dws.Infrastructure;
using JayTom.Dws.Ocr.ExpressBill;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Plugin.SaveImage;
using JayTom.Dws.Client.ViewModels;
using Microsoft.Extensions.Hosting;
using JayTom.Dws.Interface.License;
using JayTom.Dws.Client.Views.Pages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Plugin.Tcp.TcpClient;
using JayTom.Dws.Plugin.Tcp.TcpServer;
using JayTom.Dws.Camera.BarCodeReader;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Domain.EventMediators;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Sorting;
using JayTom.Dws.Infrastructure.Service;
using JayTom.Dws.Client.ViewModels.Pages;
using Microsoft.Extensions.Configuration;
using JayTom.Dws.Client.ViewModels.Dialog;
using JayTom.Dws.Infrastructure.IComputer;
using JayTom.Dws.Plugin.Scale.StaticScale;
using JayTom.Dws.Client.ViewModels.Editors;
using JayTom.Dws.Plugin.Scale.DynamicScale;
using JayTom.Dws.Domain.Repository.LocalLog;
using JayTom.Dws.Interface.Cloud.CloudVideo;
using JayTom.Dws.Client.Service.TestService;
using JayTom.Dws.Client.Service.ResultOutput;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Domain.Repository.LocalData;
using JayTom.Dws.Domain.Service.CacheCleanup;
using JayTom.Dws.Client.Service.SyncSettings;
using JayTom.Dws.Domain.Service.ImageService;
using JayTom.Dws.Client.Service.ImageService;
using JayTom.Dws.Plugin.Device.KeyboardDevice;
using Microsoft.Extensions.DependencyInjection;
using JayTom.Dws.Plugin.Device.GrayscaleDevice;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Service.BackgroundService;
using JayTom.Dws.Client.Views.Editors.CloudService;
using JayTom.Dws.Client.Service.ProcessingServices;
using JayTom.Dws.Client.Service.ExternalDataService;
using JayTom.Dws.Infrastructure.Repository.LocalLog;
using DryIoc.Microsoft.DependencyInjection.Extension;
using JayTom.Dws.Client.ViewModels.Pages.Preferences;
using JayTom.Dws.Infrastructure.Repository.LocalConf;
using JayTom.Dws.Infrastructure.Repository.LocalData;
using JayTom.Dws.Client.Service.DefaultConfiguration;
using JayTom.Dws.Camera.Cameras.SmartCamera.Hikvision;
using JayTom.Dws.Client.ViewModels.Editors.CloudService;
using JayTom.Dws.Domain.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Views;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.Views.Pages.Preferences.LogsViews;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Client.Views.Pages.Preferences.AppSettings;
using JayTom.Dws.Client.Views.Pages.Preferences.CloudService;
using JayTom.Dws.Client.Service.Sorting.Communication.TcpComm;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.ViewModels;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using JayTom.Dws.Client.Service.Sorting.Communication.SerialComm;
using JayTom.Dws.Client.Views.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.AppSettings;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CloudConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Infrastructure.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CloudService;
using JayTom.Dws.Infrastructure.SignalR.CloudApi.ClientMessageHub;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Service.ResultOutput.Communication.TcpComm;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.LogsViewModel;
using JayTom.Dws.Client.Views.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.RuleConfig;
using JayTom.Dws.Client.Views.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Infrastructure.Repository.LocalConf.PackageSortingConfig.ConnectionParams;
using JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration.SortingMethodEditors;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages;
using JayTom.Dws.Client.ViewModels.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages;

namespace JayTom.Dws.Client.Composition
{
    /// <summary>集中维护视图与视图模型的显式映射。</summary>
    internal static class ViewModelMappingRegistration
    {
        /// <summary>注册全部显式视图模型映射。</summary>
        public static void Register()
        {            //绑定页面

            ViewModelLocationProvider.Register<ExportDialog, ExportDialogViewModel>();
            ViewModelLocationProvider.Register<LoadingDialog, LoadingDialogViewModel>();
            ViewModelLocationProvider.Register<DataTimeEditor, DataTimeEditorViewModel>();
            ViewModelLocationProvider.Register<BulkDeleteAccessDialog, BulkDeleteAccessViewModel>();
            ViewModelLocationProvider.Register<PackageExitLockEditor, PackageExitLockEditorViewModel>();
            ViewModelLocationProvider.Register<PackageExitDefinitionEditor, PackageExitDefinitionEditorViewModel>();
            ViewModelLocationProvider.Register<LogisticsCodeRecognitionEditor, LogisticsCodeRecognitionEditorViewModel>();
            ViewModelLocationProvider.Register<SortingInstructionBindingEditor, SortingInstructionBindingEditorViewModel>();
            ViewModelLocationProvider.Register<BarcodeSortingRuleEditor, BarcodeSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<WeightSortingRuleEditor, WeightSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<VolumeSortingRuleEditor, VolumeSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<LogisticsSortingRuleEditor, LogisticsSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<OcrSortingRuleEditor, OcrSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<ApiSortingRuleEditor, ApiSortingRuleEditorViewModel>();
            ViewModelLocationProvider.Register<CommunicationConnectionConfigEditor, CommunicationConnectionConfigEditorViewModel>();
            ViewModelLocationProvider.Register<RegularExpressionEditor, RegularExpressionEditorViewModel>();

            //Ipc/Nvr编辑
            ViewModelLocationProvider.Register<NvrIpcDeviceEditor, NvrIpcDeviceEditorViewModel>();
            ViewModelLocationProvider.Register<NvrCameraMappingEditor, NvrCameraMappingEditorViewModel>();
            ViewModelLocationProvider.Register<NvrBindingEditor, NvrBindingEditorViewModel>();
            ViewModelLocationProvider.Register<NvrWatermarkConfigEditor, NvrWatermarkConfigEditorViewModel>();

            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
            ViewModelLocationProvider.Register<SettingsPage, SettingsViewModel>();
            ViewModelLocationProvider.Register<PluginMarketplacePage, PluginMarketplaceViewModel>();
            ViewModelLocationProvider.Register<HomePage, HomeViewModel>();
            ViewModelLocationProvider.Register<StatusBarPage, StatusBarViewModel>();
            ViewModelLocationProvider.Register<ApiAccessDialog, ApiAccessViewModel>();
            ViewModelLocationProvider.Register<PackageDetailsDialog, PackageDetailsDialogViewModel>();
            ViewModelLocationProvider.Register<IpcPreviewDialog, IpcPreviewViewModel>();
            ViewModelLocationProvider.Register<NvrBindingPreviewViewDialog, NvrBindingPreviewViewDialogViewModel>();
            ViewModelLocationProvider.Register<NvrRecordingDialog, NvrRecordingViewModel>();

            ViewModelLocationProvider.Register<ApiTestDialog, ApiTestViewModel>();
            ViewModelLocationProvider.Register<DataManagementPage, DataManagementViewModel>();
            ViewModelLocationProvider.Register<CameraConfigurationPage, CameraConfigurationViewModel>();
            ViewModelLocationProvider.Register<BarcodeScannerCameraConfigPage, BarcodeScannerCameraConfigViewModel>();
            ViewModelLocationProvider.Register<PanoramaCameraConfigPage, PanoramaCameraConfigViewModel>();
            ViewModelLocationProvider.Register<VolumeCameraConfigPage, VolumeCameraConfigViewModel>();
            ViewModelLocationProvider.Register<CameraFinderPage, CameraFinderViewModel>();
            ViewModelLocationProvider.Register<UsbCameraSettingsPage, UsbCameraSettingsViewModel>();
            ViewModelLocationProvider.Register<AlgorithmSettingsPage, AlgorithmSettingsViewModel>();
            ViewModelLocationProvider.Register<NVRIPCDeviceManagementPage, NvrIpcDeviceManagementViewModel>();

            ViewModelLocationProvider.Register<APISettingsPage, ApiSettingsPageViewModel>();
            ViewModelLocationProvider.Register<SaveImageSettingsPage, SaveImageSettingsPageViewModel>();
            ViewModelLocationProvider.Register<BarcodeFilterSettingsPage, BarcodeFilterSettingsPageViewModel>();
            ViewModelLocationProvider.Register<ResultOutputSettingsPage, ResultOutputSettingsPageViewModel>();
            ViewModelLocationProvider.Register<ContentInputSettingsPage, ContentInputSettingsPageViewModel>();
            ViewModelLocationProvider.Register<CacheClearSettingsPage, CacheClearSettingsPageViewModel>();
            ViewModelLocationProvider.Register<BarcodeFilterSettingsPage, BarcodeFilterSettingsPageViewModel>();
            ViewModelLocationProvider.Register<WeightSettingPages, WeightSettingViewModel>();
            ViewModelLocationProvider.Register<VolumeSettingsPage, VolumeSettingsViewModel>();
            ViewModelLocationProvider.Register<AppSettingsPage, AppSettingsViewModel>();
            ViewModelLocationProvider.Register<LogManagerPage, LogManagerViewModel>();
            ViewModelLocationProvider.Register<VideoCameraSettingsDialog, VideoCameraSettingsViewModel>();
            ViewModelLocationProvider.Register<TriggerModeSelectionPage, TriggerModeSelectionViewModel>();
            ViewModelLocationProvider.Register<ScanCameraSelectionDialog, ScanCameraSelectionDialogViewModel>();
            ViewModelLocationProvider.Register<ResolutionConstraintDialog, ResolutionConstraintViewModel>();
            ViewModelLocationProvider.Register<CloudServicePage, CloudServicePageViewModel>();
            ViewModelLocationProvider.Register<PasswordValidationDialog, PasswordValidationDialogViewModel>();

            ViewModelLocationProvider.Register<NetworkVideoRecorderPage, NetworkVideoRecorderPageViewModel>();
            ViewModelLocationProvider.Register<PackageSortingSettingsPage, PackageSortingSettingsViewModel>();
            ViewModelLocationProvider.Register<OcrSettingsPage, OcrSettingsViewModel>();
            ViewModelLocationProvider.Register<WorkflowSettingsPage, WorkflowSettingsViewModel>();

            ViewModelLocationProvider.Register<LogisticsCodeRecognitionPage, LogisticsCodeRecognitionViewModel>();
            ViewModelLocationProvider.Register<PackageExitDefinitionPage, PackageExitDefinitionViewModel>();
            ViewModelLocationProvider.Register<SortingInstructionBindingPage, SortingInstructionBindingViewModel>();
            ViewModelLocationProvider.Register<SortingSchemeSettingsPage, SortingSchemeSettingsViewModel>();
            ViewModelLocationProvider.Register<CommunicationsSettingsPage, CommunicationsSettingsViewModel>();
            ViewModelLocationProvider.Register<SortingMethodPage, SortingMethodViewModel>();
            ViewModelLocationProvider.Register<PackageExitLockSettingsPage, PackageExitLockSettingsViewModel>();
            ViewModelLocationProvider.Register<StackedPackageDetectionSettingsPage, StackedPackageDetectionSettingsViewModel>();
            ViewModelLocationProvider.Register<SupplyCounterSettingsPage, SupplyCounterSettingsViewModel>();
            ViewModelLocationProvider.Register<GrayscaleDeviceSettingsPage, GrayscaleDeviceSettingsViewModel>();
            //分拣模式
            ViewModelLocationProvider.Register<BarcodeSortingPage, BarcodeSortingViewModel>();
            ViewModelLocationProvider.Register<WeightSortingPage, WeightSortingViewModel>();

            ViewModelLocationProvider.Register<VolumeSortingPage, VolumeSortingViewModel>();
            ViewModelLocationProvider.Register<LogisticsSortingPage, LogisticsSortingViewModel>();
            ViewModelLocationProvider.Register<OcrSortingPage, OcrSortingViewModel>();
            ViewModelLocationProvider.Register<ApiResponseSortingPage, ApiResponseSortingViewModel>();
            ViewModelLocationProvider.Register<CombinedWorkflowSortingPage, CombinedWorkflowSortingViewModel>();
            //程序设置
            ViewModelLocationProvider.Register<GridSettingsPage, GridSettingsViewModel>();
            ViewModelLocationProvider.Register<OtherSettingsPage, OtherSettingsViewModel>();
            ViewModelLocationProvider.Register<LicensePage, LicensePageViewModel>();
            ViewModelLocationProvider.Register<SyncSettingsPage, SyncSettingsViewModel>();
            ViewModelLocationProvider.Register<PassWordSettingsPage, PassWordSettingsViewModel>();

            //组包设置
            ViewModelLocationProvider.Register<CreatePackageSettingsPage, CreatePackageSettingsViewModel>();
            //日志
            ViewModelLocationProvider.Register<AppLogPage, AppLogPageViewModel>();
            ViewModelLocationProvider.Register<CameraLogPage, CameraLogPageViewModel>();
            ViewModelLocationProvider.Register<SortingLogPage, SortingLogPageViewModel>();
            ViewModelLocationProvider.Register<WeighingLogPage, WeighingLogPageViewModel>();
            ViewModelLocationProvider.Register<VolumeLogPage, VolumeLogPageViewModel>();
            ViewModelLocationProvider.Register<APILogPage, ApiLogPageViewModel>();
            ViewModelLocationProvider.Register<OutputLogPage, OutputLogPageViewModel>();
            ViewModelLocationProvider.Register<FTPLogPage, FtpLogPageViewModel>();
            ViewModelLocationProvider.Register<ExceptionLogPage, ExceptionLogPageViewModel>();
            //云端服务
            ViewModelLocationProvider.Register<CloudDataPage, CloudDataPageViewModel>();
            ViewModelLocationProvider.Register<CloudVideoPage, CloudVideoSettingsPageViewModel>();
            //Nvr绑定页面
            ViewModelLocationProvider.Register<NvrCameraBindingEditor, NvrCameraBindingEditorViewModel>();
            //接口
            ViewModelLocationProvider.Register<DefaultApiPage, DefaultApiPageViewModel>();
            ViewModelLocationProvider.Register<SzjyApiPage, SzjyApiPageViewModel>();
            ViewModelLocationProvider.Register<WdtFlagshipApiPage, WdtFlagshipApiPageViewModel>();
            ViewModelLocationProvider.Register<WdtWmsApiPage, WdtWmsApiPageViewModel>();
            ViewModelLocationProvider.Register<JtExpressApiPage, JtExpressApiPageViewModel>();
            ViewModelLocationProvider.Register<JtPolarDayApiPage, JtPolarDayApiPageViewModel>();
            ViewModelLocationProvider.Register<RoutDataApiPage, RoutDataApiViewPageModel>();
            ViewModelLocationProvider.Register<CaiNiaoApiPage, CaiNiaoApiPageViewModel>();
            ViewModelLocationProvider.Register<EshippingitApiPage, EshippingitApiPageViewModel>();
            //实时日志
            //实时日志
            ViewModelLocationProvider.Register<RealTimeLogPage, RealTimeLogViewModel>();
            //其他插件
            {
                ViewModelLocationProvider.Register<SunnenInputBarcodeControl, SunnenInputBarcodeViewModel>();
            }
        }
    }
}
