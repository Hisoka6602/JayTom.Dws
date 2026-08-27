using System;
using Prism.Mvvm;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.EventMediators;
using JayTom.Dws.Client.Service.Device;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Models.LocalConf.CloudConfig;
using JayTom.Dws.Models.LocalConf.CameraConfig;
using JayTom.Dws.Application.CameraConfigurations;
using JayTom.Dws.Client.Models.CloudSettingModel;
using SettingsChangedEvent = JayTom.Dws.Application.Events.SettingsChangedEvent;

namespace JayTom.Dws.Client.ViewModels.Editors.CloudService
{

    public class NvrCameraBindingEditorViewModel : BindableBase
    {
        /// <summary>应用内消息总线。</summary>
        private readonly JayTom.Dws.Application.Messaging.IEventBus _eventBus;
        private readonly IDeviceService _deviceService;
        private readonly ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> _barcodeScannerCameraConfigRepository;
        private readonly ICameraConfigurationCatalog<NvrCameraBindingInfoModel> _nvrCameraBindingRepository;
        private string _identifier = string.Empty;
        private string _message = string.Empty;
        private int _channel;

        private ObservableCollection<NvrCameraBindingItemInfoModel> _nvrCameraBindingItems = new()
        {
            new NvrCameraBindingItemInfoModel()
            {
                IsBinding = true,
                Num = 1,
                CameraSerialNumber = "序列号AAA1",
                CustomCameraName = "自定义名称1"
            },
            new NvrCameraBindingItemInfoModel()
            {
                IsBinding = false,
                Num = 2,
                CameraSerialNumber = "序列号AAA1",
                CustomCameraName = "自定义名称1"
            },
        };

        private bool _isAllSelect;
        private string _ipAddress = string.Empty;
        private int _port;
        private string _username = string.Empty;
        private string _password = string.Empty;

        public NvrCameraBindingEditorViewModel(IDeviceService deviceService,
            ICameraConfigurationCatalog<BarcodeScannerCameraConfigInfoModel> barcodeScannerCameraConfigRepository,
            ICameraConfigurationCatalog<NvrCameraBindingInfoModel> nvrCameraBindingRepository,
            JayTom.Dws.Application.Messaging.IEventBus eventBus)
        {
            _eventBus = eventBus;
            _deviceService = deviceService;
            _barcodeScannerCameraConfigRepository = barcodeScannerCameraConfigRepository;
            _nvrCameraBindingRepository = nvrCameraBindingRepository;
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 通道
        /// </summary>
        public int Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        /// <summary>
        /// 是否全选
        /// </summary>
        public bool IsAllSelect
        {
            get => _isAllSelect;
            set => SetProperty(ref _isAllSelect, value);
        }

        public ObservableCollection<NvrCameraBindingItemInfoModel> NvrCameraBindingItems
        {
            get => _nvrCameraBindingItems;
            set => SetProperty(ref _nvrCameraBindingItems, value);
        }

        public ICommand SelectedCommand => new DelegateCommand<object>(SelectedDelegate);

        private async void SelectedDelegate(object obj)
        {
            await UiThread.Dispatcher.InvokeAsync(() =>
            {
                foreach (var bindingItem in NvrCameraBindingItems)
                {
                    bindingItem.IsBinding = IsAllSelect;
                }
            });
        }

        public ICommand LoadedCommand => new DelegateCommand<object>(LoadedDelegate);

        private async void LoadedDelegate(object obj)
        {
            //加载扫码相机
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                NvrCameraBindingItems.Clear();
                if (_deviceService.CameraItems?.Any() != true &&
                    !_deviceService.RunningStatus)
                {
                    await _deviceService.RefreshCameraEnumerationAsync();
                }
                var configInfoModels = await _barcodeScannerCameraConfigRepository
                    .Select(s => s.Id > 0,
                        o => o.Id);
                var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.Select(s =>
                    s.Channel.Equals(Channel) &&
                    s.IpAddress.Equals(IpAddress) &&
                    s.Port.Equals(Port), o => o.Id);
                if (configInfoModels?.Any() == true)
                {
                    if (_deviceService.CameraItems?.Any() == true)
                    {
                        var num = 1;
                        foreach (var barcodeScannerCameraConfigInfoModel in configInfoModels)
                        {
                            var isExistingSerialNumber = _deviceService.CameraItems.Any(a =>
                                a.SerialNumber.Equals(barcodeScannerCameraConfigInfoModel.SerialNumber));

                            NvrCameraBindingItems.Add(new NvrCameraBindingItemInfoModel()
                            {
                                CameraSerialNumber = barcodeScannerCameraConfigInfoModel.SerialNumber,
                                CustomCameraName = isExistingSerialNumber ? barcodeScannerCameraConfigInfoModel.CustomName : $"{barcodeScannerCameraConfigInfoModel.CustomName}(无效)",
                                Num = num,
                                // IsBinding = nvrCameraBindingInfoModels.Any(a => a.BarcodeScannerSerialNumber.Equals(barcodeScannerCameraConfigInfoModel.SerialNumber)),
                            });
                            num++;
                        }
                    }
                }
            });
        }

        public ICommand SaveCommand => new DelegateCommand(SaveDelegate);

        private async void SaveDelegate()
        {
            //保存到表
            //先删除
            await UiThread.Dispatcher.InvokeAsyncUnwrapped(async () =>
            {
                var list = NvrCameraBindingItems.Where(w => w.IsBinding).Select(s => s.CameraSerialNumber)?.ToList() ??
                           new List<string>();
                var nvrCameraBindingInfoModels = await _nvrCameraBindingRepository.Select(s =>
                    (s.Channel.Equals(Channel) &&
                    s.IpAddress.Equals(IpAddress) &&
                    s.Port.Equals(Port)), o => o.Id);
                //|| list.Contains(s.BarcodeScannerSerialNumber)
                if (nvrCameraBindingInfoModels?.Any() == true)
                {
                    var deleteRange = await _nvrCameraBindingRepository.DeleteRange(nvrCameraBindingInfoModels);
                    if (!deleteRange)
                    {
                        Message = "保存失败";
                    }
                }
                //修改删除条件

                var cameraBindingInfoModels = NvrCameraBindingItems.Where(w => w.IsBinding).Select(s => new NvrCameraBindingInfoModel
                {
                    //BarcodeScannerSerialNumber = s.CameraSerialNumber,
                    Channel = Channel,
                    IpAddress = IpAddress,
                    Password = Password,
                    Port = Port,
                    Username = Username
                })?.ToList();
                var insertRange = await _nvrCameraBindingRepository.InsertRange(cameraBindingInfoModels ??
                    new List<NvrCameraBindingInfoModel>());
                if (!insertRange)
                {
                    Message = "保存失败";
                }
                else
                {
                    _eventBus.Publish(new SettingsChangedEvent
                    {
                        SettingsName = "NvrCameraBindingInfoModel"
                    });
                }
                if (DialogHost.IsDialogOpen(Identifier))
                {
                    DialogHost.Close(Identifier);
                }
            });
        }

        public ICommand CancelCommand => new DelegateCommand(CancelDelegate);

        private void CancelDelegate()
        {
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }
    }
}
