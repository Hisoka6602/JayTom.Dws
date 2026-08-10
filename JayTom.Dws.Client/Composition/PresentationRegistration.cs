using JayTom.Dws.Client.HomeToolPlugin.SunnenPlugin.Views;
using JayTom.Dws.Client.Views.Dialog;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Client.Views.Editors;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Client.Views.Pages;
using JayTom.Dws.Client.Views.Pages.Preferences;
using JayTom.Dws.Client.Views.Pages.Preferences.ApiConfiguration;
using JayTom.Dws.Client.Views.Pages.Preferences.AppSettings;
using JayTom.Dws.Client.Views.Pages.Preferences.CameraConfiguration;
using JayTom.Dws.Client.Views.Pages.Preferences.CloudService;
using JayTom.Dws.Client.Views.Pages.Preferences.LogsViews;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration;
using JayTom.Dws.Client.Views.Pages.Preferences.PackageSortingConfiguration.SortingMethodPages;
using Prism.Ioc;

namespace JayTom.Dws.Client.Composition;

/// <summary>集中注册窗口、对话框与导航页面。</summary>
internal static class PresentationRegistration {
    /// <summary>注册桌面端全部界面入口。</summary>
    public static void RegisterPresentation(this IContainerRegistry registry) {
        registry.RegisterDialog<ApiAccessDialog>();
        registry.RegisterDialog<ApiTestDialog>();
        registry.RegisterDialog<PackageDetailsDialog>();
        registry.RegisterDialog<NvrBindingPreviewViewDialog>();
        registry.RegisterDialog<SunnenInputBarcodeControl>();
        registry.RegisterDialog<VideoCameraSettingsDialog>();
        registry.RegisterDialog<TriggerModeSelectionPage>();
        registry.RegisterDialog<ScanCameraSelectionDialog>();

        registry.RegisterForNavigation<PluginMarketplacePage>();
        registry.RegisterForNavigation<DataManagementPage>();
        registry.RegisterForNavigation<CameraConfigurationPage>();
        registry.RegisterForNavigation<APISettingsPage>();
        registry.RegisterForNavigation<SaveImageSettingsPage>();
        registry.RegisterForNavigation<BarcodeFilterSettingsPage>();
        registry.RegisterForNavigation<ResultOutputSettingsPage>();
        registry.RegisterForNavigation<ContentInputSettingsPage>();
        registry.RegisterForNavigation<CacheClearSettingsPage>();
        registry.RegisterForNavigation<WeightSettingPages>();
        registry.RegisterForNavigation<VolumeSettingsPage>();
        registry.RegisterForNavigation<LogManagerPage>();
        registry.RegisterForNavigation<PackageSortingSettingsPage>();
        registry.RegisterForNavigation<OcrSettingsPage>();
        registry.RegisterForNavigation<WorkflowSettingsPage>();
        registry.RegisterForNavigation<AppSettingsPage>();
        registry.RegisterForNavigation<CloudServicePage>();
        registry.RegisterForNavigation<CreatePackageSettingsPage>();
        registry.RegisterForNavigation<BarcodeScannerCameraConfigPage>();
        registry.RegisterForNavigation<CameraFinderPage>();
        registry.RegisterForNavigation<PanoramaCameraConfigPage>();
        registry.RegisterForNavigation<VolumeCameraConfigPage>();
        registry.RegisterForNavigation<UsbCameraSettingsPage>();
        registry.RegisterForNavigation<AlgorithmSettingsPage>();
        registry.RegisterForNavigation<NVRIPCDeviceManagementPage>();
        registry.RegisterForNavigation<LogisticsCodeRecognitionPage>();
        registry.RegisterForNavigation<PackageExitDefinitionPage>();
        registry.RegisterForNavigation<SortingInstructionBindingPage>();
        registry.RegisterForNavigation<SortingSchemeSettingsPage>();
        registry.RegisterForNavigation<CommunicationsSettingsPage>();
        registry.RegisterForNavigation<SortingMethodPage>();
        registry.RegisterForNavigation<PackageExitLockSettingsPage>();
        registry.RegisterForNavigation<StackedPackageDetectionSettingsPage>();
        registry.RegisterForNavigation<SupplyCounterSettingsPage>();
        registry.RegisterForNavigation<GrayscaleDeviceSettingsPage>();
        registry.RegisterForNavigation<GridSettingsPage>();
        registry.RegisterForNavigation<OtherSettingsPage>();
        registry.RegisterForNavigation<LicensePage>();
        registry.RegisterForNavigation<SyncSettingsPage>();
        registry.RegisterForNavigation<PassWordSettingsPage>();
        registry.RegisterForNavigation<CloudDataPage>();
        registry.RegisterForNavigation<CloudVideoPage>();
        registry.RegisterForNavigation<NetworkVideoRecorderPage>();
    }
}
