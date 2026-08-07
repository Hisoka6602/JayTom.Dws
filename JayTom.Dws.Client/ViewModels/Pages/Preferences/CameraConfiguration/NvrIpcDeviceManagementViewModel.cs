using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows;
using System.Drawing;
using JayTom.Dws.Camera;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Domain.Manager;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Data.LocalConf.CameraConfig;
using JayTom.Dws.Data.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Client.ViewModels.Editors.Enums;
using JayTom.Dws.Client.Views.Editors.CloudService;
using JayTom.Dws.Client.Views.Dialog.CameraConfiguration;
using JayTom.Dws.Camera.Cameras.SecurityCamera.DaHuatech;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Domain.Repository.LocalConf.CameraConfig;
using JayTom.Dws.Domain.Repository.LocalConf.IpcNvrConfig;
using JayTom.Dws.Client.ViewModels.Dialog.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using JayTom.Dws.Infrastructure.Repository.LocalConf.IpcNvrConfig;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences.CameraConfiguration
{

    /// <summary>
    /// IPC/NVR管理
    /// </summary>
    public class NvrIpcDeviceManagementViewModel : BindableBase
    {
        private readonly IConfigRepository _configRepository;
        private readonly IIpcNvrConfigRepository _ipcNvrConfigRepository;
        private readonly IBarcodeScannerCameraConfigRepository _barcodeScannerCameraConfigRepository;
        private List<IpcNvrConfigInfoModel>? _ipcNvrConfigInfoModels;
        private List<BarcodeScannerCameraConfigInfoModel>? _scannerCameraConfigInfoModels;

        private ObservableCollection<IpcNvrItemInfoModel> _ipcNvrItemInfos = new();

        private bool _isRefreshing;
        private SnackbarMessageQueue _nvrIpcDeviceManagemenMessageQueue = new(TimeSpan.FromSeconds(2));
        private bool _isLoad;

        public ObservableCollection<IpcNvrItemInfoModel> IpcNvrItemInfos
        {
            get => _ipcNvrItemInfos;
            set => SetProperty(ref _ipcNvrItemInfos, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string Identifier => "CameraSettingDialog";

        public SnackbarMessageQueue NvrIpcDeviceManagemenMessageQueue
        {
            get => _nvrIpcDeviceManagemenMessageQueue;
            set => SetProperty(ref _nvrIpcDeviceManagemenMessageQueue, value);
        }

        public NvrIpcDeviceManagementViewModel(IConfigRepository configRepository,
            IIpcNvrConfigRepository ipcNvrConfigRepository,
            IBarcodeScannerCameraConfigRepository barcodeScannerCameraConfigRepository)
        {
            _configRepository = configRepository;
            _ipcNvrConfigRepository = ipcNvrConfigRepository;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private void LoadedDelegate(object obj)
        {
            if (!_isLoad)
            {
                _isLoad = true;
                RefreshDelegate(obj);
            }
        }

        /// <summary>
        /// 预览
        /// </summary>
        public ICommand PreviewCommand => new DelegateCommand<object>(PreviewDelegate);

        private async void PreviewDelegate(object obj)
        {
            //显示预览框
            if (AppContext.GetData("IsRunning") is true)
            {
                NvrIpcDeviceManagemenMessageQueue.Enqueue("请先停止运行再预览");
                return;
            }
            if (obj is IpcNvrItemInfoModel info)
            {
                var ipcPreviewDialog = new IpcPreviewDialog();
                if (ipcPreviewDialog.DataContext is IpcPreviewViewModel model)
                {
                    model.Identifier = Identifier;
                    model.IpcNvrItemInfo = info;

                    await DialogHost.Show(ipcPreviewDialog, model.Identifier);
                }
            }
        }

        /// <summary>
        /// 绑定
        /// </summary>
        public ICommand BindCommand => new DelegateCommand<object>(BindDelegate);

        private async void BindDelegate(object obj)
        {
            //显示绑定框
            if (AppContext.GetData("IsRunning") is true)
            {
                NvrIpcDeviceManagemenMessageQueue.Enqueue("请先停止运行再设置");
                return;
            }
            if (obj is IpcNvrItemInfoModel info)
            {
                var nvrCameraMappingEditor = new NvrCameraMappingEditor();
                if (nvrCameraMappingEditor.DataContext is NvrCameraMappingEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    model.IpcNvrItemInfo = info;
                    await DialogHost.Show(nvrCameraMappingEditor, model.Identifier);
                }
            }
        }

        /// <summary>
        /// 设置水印
        /// </summary>
        public ICommand SetWatermarkCommand => new DelegateCommand<object>(SetWatermarkDelegate);

        private async void SetWatermarkDelegate(object obj)
        {
            if (obj is IpcNvrItemInfoModel { Type: DeviceType.NVR } info)
            {
                var nvrWatermarkConfigEditor = new NvrWatermarkConfigEditor();
                if (nvrWatermarkConfigEditor.DataContext is NvrWatermarkConfigEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    model.IpcNvrItemInfo = info;
                    await DialogHost.Show(nvrWatermarkConfigEditor, model.Identifier);
                }
            }
        }

        /// <summary>
        /// 编辑
        /// </summary>
        public ICommand EditCommand => new DelegateCommand<object>(EditDelegate);

        private async void EditDelegate(object obj)
        {
            if (IsRefreshing) return;
            if (AppContext.GetData("IsRunning") is true)
            {
                NvrIpcDeviceManagemenMessageQueue.Enqueue("请先停止运行再设置");
                return;
            }
            if (obj is IpcNvrItemInfoModel info)
            {
                var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
                if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model)
                {
                    model.Identifier = Identifier;
                    model.ShowType = EditorOperationType.Edit;
                    model.IpcNvrItemInfo = info;

                    await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                    if (!string.IsNullOrEmpty(model.Message))
                    {
                        NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                    }

                    if (model.IsOk)
                    {
                        var insertOrUpdate = await _ipcNvrConfigRepository.InsertOrUpdate(new IpcNvrConfigInfoModel()
                        {
                            Brand = info.Brand,
                            Channel = info.Channel,
                            IpAddress = info.IpAddress,
                            Name = info.Name,
                            Password = info.Password,
                            Port = info.Port,
                            Type = (int)info.Type,
                            Username = info.Username,
                            SerialNumber = info.SerialNumber,
                            ChannelCount = info.ChannelCount
                        });
                        if (!insertOrUpdate)
                        {
                            NvrIpcDeviceManagemenMessageQueue.Enqueue("保存失败!");
                        }

                        RefreshDelegate(obj);
                    }
                }
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand => new DelegateCommand<object>(DeleteDelegate);

        private async void DeleteDelegate(object obj)
        {
            if (IsRefreshing) return;
            if (AppContext.GetData("IsRunning") is true)
            {
                NvrIpcDeviceManagemenMessageQueue.Enqueue("请先停止运行再删除");
                return;
            }
            if (obj is IpcNvrItemInfoModel info)
            {
                var ipcNvrConfigInfoModel = await _ipcNvrConfigRepository.FirstOrDefault(f => f.IpAddress.Equals(info.IpAddress));
                if (ipcNvrConfigInfoModel is not null)
                {
                    var delete = await _ipcNvrConfigRepository.Delete(ipcNvrConfigInfoModel);
                    if (!delete)
                    {
                        NvrIpcDeviceManagemenMessageQueue.Enqueue("删除失败!");
                    }

                    RefreshDelegate(obj);
                }
            }
        }

        public ICommand RefreshCommand => new DelegateCommand<object>(RefreshDelegate);

        /// <summary>
        /// 刷新
        /// </summary>
        /// <param name="obj"></param>
        private async void RefreshDelegate(object obj)
        {
            if (IsRefreshing) return;
            IsRefreshing = true;
            try
            {
                var ipcNvrConfigTask = _ipcNvrConfigRepository.MemoryCacheData();
                var scannerCameraConfigTask = _barcodeScannerCameraConfigRepository.MemoryCacheData();
                var sdkSelectorTask =
                    _configRepository.FirstOrDefaultEntity<CameraSdkSelectorDto>("CameraSdkSelector");
                await Task.WhenAll(ipcNvrConfigTask, scannerCameraConfigTask, sdkSelectorTask);

                _ipcNvrConfigInfoModels = await ipcNvrConfigTask;
                _scannerCameraConfigInfoModels = await scannerCameraConfigTask;
                var sdkSelectorDto = await sdkSelectorTask ?? new CameraSdkSelectorDto();
                List<CameraInfo> cameraList;
                if (sdkSelectorDto.IsUseDaHuaSecurityCameraSdk)
                {
                    // 原生SDK枚举可能同步阻塞，保留在线程池执行以避免卡住界面线程。
                    cameraList = await Task.Run(async () =>
                        await new DaHuatechSecurityCamera().EnumerateCameras() ?? []);
                }
                else
                {
                    cameraList = [];
                    NvrIpcDeviceManagemenMessageQueue.Enqueue("未选中任何IPC/NVR的SDK!");
                }

                var cameraBindingsByIp = CreateCameraBindingsByIp(_scannerCameraConfigInfoModels);
                var itemInfoModels = _ipcNvrConfigInfoModels?.Select(s => new IpcNvrItemInfoModel
                {
                    IsConfigured = true,
                    Id = s.Id,
                    DeviceName = s.Name,
                    IpAddress = s.IpAddress,
                    Port = s.Port,
                    Type = (DeviceType)s.Type,
                    Username = s.Username,
                    Password = s.Password,
                    Channel = s.Channel,
                    Brand = s.Brand,
                    ChannelCount = s.ChannelCount,
                    SerialNumber = s.SerialNumber,
                    BindingCameraSerialNumbers = CreateBindingCollection(s.IpAddress, cameraBindingsByIp)
                })?.ToList() ?? new List<IpcNvrItemInfoModel>();

                var configuredDevicesByIp = itemInfoModels
                    .Where(static item => !string.IsNullOrWhiteSpace(item.IpAddress))
                    .GroupBy(static item => item.IpAddress, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(static group => group.Key, static group => group.First(),
                        StringComparer.OrdinalIgnoreCase);
                var mergedDevices = new List<IpcNvrItemInfoModel>(cameraList.Count + itemInfoModels.Count);
                var deviceIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var camera in cameraList)
                {
                    var identity = GetDeviceIdentity(camera.IpAddress, camera.SerialNumber);
                    if (!deviceIdentities.Add(identity))
                    {
                        continue;
                    }

                    configuredDevicesByIp.TryGetValue(camera.IpAddress, out var configuredDevice);
                    mergedDevices.Add(new IpcNvrItemInfoModel
                    {
                        ChannelCount = configuredDevice?.ChannelCount > 0
                            ? configuredDevice.ChannelCount
                            : camera.CameraNvrInfo?.ChannelCount ?? 0,
                        IsConfigured = configuredDevice is not null,
                        DeviceName = camera.Name,
                        Id = configuredDevice?.Id ?? camera.Id,
                        SerialNumber = camera.SerialNumber,
                        IpAddress = camera.IpAddress,
                        Port = configuredDevice?.Port ?? camera.Port,
                        Username = configuredDevice?.Username ?? string.Empty,
                        Password = configuredDevice?.Password ?? string.Empty,
                        Channel = configuredDevice?.Channel ?? camera.CameraNvrInfo?.ChannelCount ?? 0,
                        Model = camera.Model,
                        Brand = camera.Brand,
                        Type = camera.Type == CameraType.NvrDevice ? DeviceType.NVR : DeviceType.IPC,
                        BindingCameraSerialNumbers =
                            CreateBindingCollection(camera.IpAddress, cameraBindingsByIp)
                    });
                }

                foreach (var configuredDevice in itemInfoModels)
                {
                    if (deviceIdentities.Add(GetDeviceIdentity(configuredDevice.IpAddress,
                            configuredDevice.SerialNumber)))
                    {
                        mergedDevices.Add(configuredDevice);
                    }
                }

                for (var index = 0; index < mergedDevices.Count; index++)
                {
                    mergedDevices[index].Num = index + 1;
                }

                IpcNvrItemInfos.Clear();
                IpcNvrItemInfos.AddRange(mergedDevices);

                var loginTasks = mergedDevices
                    .Where(static device =>
                        !string.IsNullOrWhiteSpace(device.Username) &&
                        !string.IsNullOrWhiteSpace(device.Password) &&
                        device.Brand.Equals("DaHua", StringComparison.OrdinalIgnoreCase))
                    .Select(LoginDeviceAsync)
                    .ToArray();
                await Task.WhenAll(loginTasks);
            }
            catch (Exception exception)
            {
                NvrIpcDeviceManagemenMessageQueue.Enqueue($"刷新IPC/NVR失败:{exception.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 按NVR地址预生成绑定相机，避免为每台设备重复扫描全部绑定关系。
        /// </summary>
        private static Dictionary<string, List<BarcodeScannerCameraItemInfoModel>> CreateCameraBindingsByIp(
            IEnumerable<BarcodeScannerCameraConfigInfoModel>? scannerCameras)
        {
            var cameraBindingsByIp =
                new Dictionary<string, List<BarcodeScannerCameraItemInfoModel>>(StringComparer.OrdinalIgnoreCase);
            if (scannerCameras is null)
            {
                return cameraBindingsByIp;
            }

            foreach (var scannerCamera in scannerCameras)
            {
                if (scannerCamera.NvrCameraBindingInfos is null)
                {
                    continue;
                }

                foreach (var binding in scannerCamera.NvrCameraBindingInfos)
                {
                    if (string.IsNullOrWhiteSpace(binding.IpAddress))
                    {
                        continue;
                    }

                    if (!cameraBindingsByIp.TryGetValue(binding.IpAddress, out var boundCameras))
                    {
                        boundCameras = [];
                        cameraBindingsByIp.Add(binding.IpAddress, boundCameras);
                    }

                    boundCameras.Add(new BarcodeScannerCameraItemInfoModel
                    {
                        Name = scannerCamera.Name,
                        CustomName = scannerCamera.CustomName,
                        CameraType = (CameraType)scannerCamera.CameraType,
                        SerialNumber = scannerCamera.SerialNumber,
                        IpAddress = scannerCamera.IpAddress,
                        Model = scannerCamera.Model,
                        Version = scannerCamera.Version,
                        ConnectionType = (CameraConnectionType)scannerCamera.ConnectionType
                    });
                }
            }

            return cameraBindingsByIp;
        }

        /// <summary>
        /// 创建独立的绑定相机集合，避免多个设备共享可变集合。
        /// </summary>
        private static ObservableCollection<BarcodeScannerCameraItemInfoModel> CreateBindingCollection(
            string ipAddress,
            IReadOnlyDictionary<string, List<BarcodeScannerCameraItemInfoModel>> cameraBindingsByIp)
        {
            return cameraBindingsByIp.TryGetValue(ipAddress, out var cameras)
                ? new ObservableCollection<BarcodeScannerCameraItemInfoModel>(cameras)
                : [];
        }

        /// <summary>
        /// 获取设备合并键，优先使用IP地址，缺少地址时回退到序列号。
        /// </summary>
        private static string GetDeviceIdentity(string ipAddress, string serialNumber)
        {
            return !string.IsNullOrWhiteSpace(ipAddress) ? $"IP:{ipAddress}" : $"序列号:{serialNumber}";
        }

        /// <summary>
        /// 在独立线程并发登录设备，并在界面上下文更新登录结果。
        /// </summary>
        private static async Task LoginDeviceAsync(IpcNvrItemInfoModel device)
        {
            device.Status = NvrStatus.LoggingIn;
            try
            {
                var (isLoggedIn, message, channelCount) = await Task.Run(async () =>
                {
                    var baseDaHuatech = BaseDaHuatech.CreateInstance();
                    var (loginSucceeded, loginMessage) =
                        await baseDaHuatech.LogIn(device.SerialNumber, device.Username, device.Password);
                    var detectedChannelCount = loginSucceeded
                        ? baseDaHuatech.GetLoggedInDeviceInfo(device.SerialNumber)?.LoggedInDeviceInfo?.nChanNum ?? 0
                        : 0;
                    return (loginSucceeded, loginMessage, detectedChannelCount);
                });

                device.Status = !isLoggedIn && message.Contains("未枚举", StringComparison.Ordinal)
                    ? NvrStatus.Offline
                    : isLoggedIn
                        ? NvrStatus.Online
                        : NvrStatus.LoginFailed;
                if (channelCount > 0)
                {
                    device.ChannelCount = channelCount;
                }
            }
            catch
            {
                device.Status = NvrStatus.LoginFailed;
            }
        }

        /// <summary>
        /// 添加
        /// </summary>
        public ICommand AddCommand => new DelegateCommand<object>(AddDelegate);

        private async void AddDelegate(object obj)
        {
            if (IsRefreshing) return;
            var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
            if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model)
            {
                model.Identifier = Identifier;
                model.ShowType = EditorOperationType.Add;
                await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.Message))
                {
                    NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                }

                if (model.IsOk)
                {
                    var insertOrUpdate = await _ipcNvrConfigRepository.InsertOrUpdate(new IpcNvrConfigInfoModel()
                    {
                        Brand = "DaHua",//当前只有大华
                        Channel = model.IpcNvrItemInfo.Channel,
                        IpAddress = model.IpcNvrItemInfo.IpAddress,
                        Name = model.IpcNvrItemInfo.Name,
                        Password = model.IpcNvrItemInfo.Password,
                        Port = model.IpcNvrItemInfo.Port,
                        Type = (int)model.IpcNvrItemInfo.Type,
                        Username = model.IpcNvrItemInfo.Username,
                        ChannelCount = 1
                    });

                    if (!insertOrUpdate)
                    {
                        NvrIpcDeviceManagemenMessageQueue.Enqueue("保存失败!");
                    }

                    RefreshDelegate(obj);
                }
            }
        }

        /// <summary>
        /// 批量改密
        /// </summary>
        public ICommand BatchChangePasswordCommand => new DelegateCommand<object>(BatchChangePasswordDelegate);

        private async void BatchChangePasswordDelegate(object obj)
        {
            if (IsRefreshing) return;
            if (AppContext.GetData("IsRunning") is true)
            {
                NvrIpcDeviceManagemenMessageQueue.Enqueue("请先停止运行再设置");
                return;
            }
            var nvrIpcDeviceEditor = new NvrIpcDeviceEditor();
            if (nvrIpcDeviceEditor.DataContext is NvrIpcDeviceEditorViewModel model)
            {
                model.Identifier = Identifier;
                model.ShowType = EditorOperationType.BatchChangePassword;
                model.SelectDevices.Clear();
                model.SelectDevices.AddRange(IpcNvrItemInfos.Where(w => w.IsSelect));
                await DialogHost.Show(nvrIpcDeviceEditor, model.Identifier);
                if (!string.IsNullOrEmpty(model.Message))
                {
                    NvrIpcDeviceManagemenMessageQueue.Enqueue(model.Message);
                }

                if (model.IsOk)
                {
                    var nvrConfigInfoModels = await _ipcNvrConfigRepository.MemoryCacheData();
                    var nvrConfigsByIp = nvrConfigInfoModels
                        .Where(static config => !string.IsNullOrWhiteSpace(config.IpAddress))
                        .GroupBy(static config => config.IpAddress,
                            StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(static group => group.Key,
                            static group => group.First(),
                            StringComparer.OrdinalIgnoreCase);

                    var ipcNvrConfigInfoModels = model.SelectDevices.Select(s =>
                    {
                        nvrConfigsByIp.TryGetValue(s.IpAddress, out var existingConfig);
                        return new IpcNvrConfigInfoModel
                        {
                            Brand = existingConfig?.Brand ?? "DaHua", //当前只有大华
                            Id = existingConfig?.Id ?? 0,
                            Channel = existingConfig?.Channel ?? s.Channel,
                            IpAddress = existingConfig?.IpAddress ?? s.IpAddress,
                            Port = existingConfig?.Port ?? s.Port,
                            Name = existingConfig?.Name ?? s.Name,
                            Type = existingConfig?.Type ?? (int)s.Type,
                            Password = model.IpcNvrItemInfo.Password,
                            Username = model.IpcNvrItemInfo.Username,
                            ChannelCount = existingConfig?.ChannelCount ?? s.ChannelCount,
                            SerialNumber = existingConfig?.SerialNumber ?? s.SerialNumber
                        };
                    }).ToList();

                    if (ipcNvrConfigInfoModels?.Any() == true)
                    {
                        var updateRange = await _ipcNvrConfigRepository.InsertOrUpdateRange(ipcNvrConfigInfoModels);
                        if (!updateRange)
                        {
                            NvrIpcDeviceManagemenMessageQueue.Enqueue("保存失败!");
                        }

                        RefreshDelegate(obj);
                    }
                }
            }
        }
    }
}
