using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using System.Collections.Generic;
using JayTom.Dws.Plugin.UsbDevice;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.Cameras;
using JayTom.Dws.Domain.Dto.BaseInfoModels;
using JayTom.Dws.Domain.Repository.LocalConf;
using JayTom.Dws.Plugin.Device.KeyboardDevice;
using JayTom.Dws.Client.Models.ImageSettingModels;
using JayTom.Dws.Client.Models.SettingsCommomModels;
using JayTom.Dws.Client.Models.ContentInputSettingsModels;
using JayTom.Dws.Client.Views.Editors.CameraConfiguration;
using JayTom.Dws.Client.ViewModels.Editors.CameraConfiguration;
using KeyboardDevice = JayTom.Dws.Plugin.Device.KeyboardDevice.KeyboardDevice;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class ContentInputSettingsPageViewModel : SettingsPageTemplateViewModel {
        private readonly IKeyboardDeviceManager _keyboardDeviceManager;
        private bool _isUseTcpInput;
        private bool _isUseControlInput;
        private ControlInputInfoModel _controlInputInfo = new();
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private ObservableCollection<ItemBaseTemplateModel> _dataTemplate = new();
        private string _separator = string.Empty;
        private bool _isUseBarcodeScannerInput;
        private bool _isUseRegularFilter;
        private ObservableCollection<KeyboardDeviceItemInfoModel> _keyboardDeviceItemInfo = new();

        public ContentInputSettingsPageViewModel(IConfigRepository configRepository,
            IKeyboardDeviceManager keyboardDeviceManager) : base(configRepository) {
            _keyboardDeviceManager = keyboardDeviceManager;

            //监控Usb
            UsbManager.Instance.UsbDeviceInserted += (sender, args) => {
                Task.Run(async () => {
                    await Task.Delay(300);
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                        var keyboardDevices = await _keyboardDeviceManager.EnumerateKeyboardDevices();

                        var devices = keyboardDevices.Where(w => !KeyboardDeviceItemInfo.Any(a => !string.IsNullOrEmpty(a.DevicePath) && (a.DevicePath.Equals(w.DevicePath) &&
                                a.VendorId.Equals(w.VendorId) && a.ProductId.Equals(w.ProductId))))
                            .ToList();

                        if (devices.Any()) {
                            var infoModels = devices.Where(w => w is { DevicePath: not null, DeviceName: not null }).Select((s, i) => new KeyboardDeviceItemInfoModel {
                                DeviceName = s.DeviceName,
                                DevicePath = s.DevicePath,
                                IsConnected = s.IsConnected,
                                ManufacturerName = s.ManufacturerName,
                                ProductId = s.ProductId,
                                VendorId = s.VendorId,
                                HasBinding = (s is { ProductId: > 0, VendorId: > 0 } && KeyboardDevice.ProductId == s.ProductId && KeyboardDevice.VendorId == s.VendorId && KeyboardDevice.DevicePath == s.DevicePath),
                                Num = i + 1,
                                IsNewlyAdded = true
                            }).ToList();
                            KeyboardDeviceItemInfo.AddRange(infoModels);
                            for (var i = 0; i < KeyboardDeviceItemInfo.Count; i++) {
                                KeyboardDeviceItemInfo[i].Num = i + 1;
                            }
                            if (KeyboardDeviceItemInfo.All(a => !a.HasBinding)) {
                                KeyboardDevice = new KeyboardDevice();
                            }
                        }
                    });
                });
            };
            UsbManager.Instance.UsbDeviceRemoved += (sender, args) => {
                Task.Run(async () => {
                    await Task.Delay(300);
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                        var keyboardDevices = await _keyboardDeviceManager.EnumerateKeyboardDevices();

                        var devices = KeyboardDeviceItemInfo.Where(w => !keyboardDevices.Any(a => !string.IsNullOrEmpty(a.DevicePath) && (a.DevicePath.Equals(w.DevicePath) &&
                                a.VendorId.Equals(w.VendorId) && a.ProductId.Equals(w.ProductId))))
                            .ToList();

                        if (devices.Any()) {
                            foreach (var device in devices) {
                                KeyboardDeviceItemInfo.Remove(device);
                            }
                            for (var i = 0; i < KeyboardDeviceItemInfo.Count; i++) {
                                KeyboardDeviceItemInfo[i].Num = i + 1;
                                KeyboardDeviceItemInfo[i].IsNewlyAdded = false;
                            }
                            if (KeyboardDeviceItemInfo.All(a => !a.HasBinding)) {
                                KeyboardDevice = new KeyboardDevice();
                            }
                        }
                    });
                });
            };
        }

        /// <summary>
        /// 是否使用Tcp输入
        /// </summary>
        public bool IsUseTcpInput {
            get => _isUseTcpInput;
            set => SetProperty(ref _isUseTcpInput, value);
        }

        /// <summary>
        /// 是否使用控件输入
        /// </summary>
        public bool IsUseControlInput {
            get => _isUseControlInput;
            set => SetProperty(ref _isUseControlInput, value);
        }

        /// <summary>
        /// 是否使用扫码枪输入
        /// </summary>
        public bool IsUseBarcodeScannerInput {
            get => _isUseBarcodeScannerInput;
            set => SetProperty(ref _isUseBarcodeScannerInput, value);
        }

        /// <summary>
        /// 是否使用常规过滤
        /// </summary>
        public bool IsUseRegularFilter {
            get => _isUseRegularFilter;
            set => SetProperty(ref _isUseRegularFilter, value);
        }

        public ObservableCollection<ItemBaseTemplateModel> DataTemplate {
            get => _dataTemplate;
            set => SetProperty(ref _dataTemplate, value);
        }

        public string Separator {
            get => _separator;
            set => SetProperty(ref _separator, value);
        }

        /// <summary>
        /// 控件输入设置
        /// </summary>
        public ControlInputInfoModel ControlInputInfo {
            get => _controlInputInfo;
            set => SetProperty(ref _controlInputInfo, value);
        }

        /// <summary>
        /// Tcp设置
        /// </summary>
        public TcpSettingsInfoModel TcpSettingsInfo {
            get => _tcpSettingsInfo;
            set => SetProperty(ref _tcpSettingsInfo, value);
        }

        public ObservableCollection<KeyboardDeviceItemInfoModel> KeyboardDeviceItemInfo {
            get => _keyboardDeviceItemInfo;
            set => SetProperty(ref _keyboardDeviceItemInfo, value);
        }

        public JayTom.Dws.Plugin.Device.KeyboardDevice.KeyboardDevice KeyboardDevice { get; set; } = new();
        public override string Identifier => "SettingDialog";
        public override string SettingsName => "ContentInputSettings";

        /// <summary>
        /// 添加输入Item
        /// </summary>
        public ICommand AddInputItemCommand => new DelegateCommand<string>(AddInputItemDelegate);

        private async void AddInputItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                obj = obj.Replace("'", string.Empty);
                DataTemplate.Add(new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = DataTemplate.Count,
                    Type = 1,
                    ApplicationType = ItemApplicationType.DataInput
                });
            });
        }

        /// <summary>
        /// 添加分隔符
        /// </summary>
        public ICommand AddSeparatorItemCommand => new DelegateCommand<string>(AddSeparatorItemDelegate);

        private async void AddSeparatorItemDelegate(string obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                DataTemplate.Add(new ItemBaseTemplateModel() {
                    Content = obj,
                    Id = DataTemplate.Count,
                    Type = 2,
                    ApplicationType = ItemApplicationType.DataInput
                });
            });
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        public ICommand RemoveTemplateItemCommand => new DelegateCommand<ItemBaseTemplateModel>(RemoveTemplateItemDelegate);

        private async void RemoveTemplateItemDelegate(ItemBaseTemplateModel model) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (model.ApplicationType == ItemApplicationType.DataInput) {
                    DataTemplate.Remove(model);
                    foreach (var item in DataTemplate) {
                        if (item.Type == 0 && string.IsNullOrEmpty(item.Content) &&
                            DataTemplate.LastOrDefault() != item) {
                            DataTemplate.Remove(item);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 使用扫码枪
        /// </summary>
        public ICommand SwitchUseBarcodeScannerCommand => new DelegateCommand<object>(SwitchUseBarcodeScannerDelegate);

        private void SwitchUseBarcodeScannerDelegate(object obj) {
            if (IsUseBarcodeScannerInput) {
                RefreshBarcodeScanner();
            }
        }

        public ICommand BindScannerCommand => new DelegateCommand<KeyboardDeviceItemInfoModel>(BindScannerDelegate);

        private void BindScannerDelegate(KeyboardDeviceItemInfoModel obj) {
            if (obj is { ProductId: > 0, VendorId: > 0 } && !string.IsNullOrEmpty(obj.DevicePath)) {
                KeyboardDevice = new KeyboardDevice() {
                    DeviceName = obj.DeviceName,
                    DevicePath = obj.DevicePath,
                    ManufacturerName = obj.ManufacturerName,
                    ProductId = obj.ProductId,
                    VendorId = obj.VendorId,
                };
                RefreshBarcodeScanner();
            }
            else {
                base.MessageQueue.Enqueue("该扫码器无法绑定");
            }
        }

        public ICommand UnbindScannerCommand => new DelegateCommand<KeyboardDeviceItemInfoModel>(UnbindScannerDelegate);

        private void UnbindScannerDelegate(KeyboardDeviceItemInfoModel obj) {
            KeyboardDevice = new KeyboardDevice();
            RefreshBarcodeScanner();
        }

        public ICommand NvrSettingsCommand => new DelegateCommand<KeyboardDeviceItemInfoModel>(NvrSettingsDelegate);

        private async void NvrSettingsDelegate(KeyboardDeviceItemInfoModel obj) {
            var nvrBindingEditor = new NvrBindingEditor();
            if (nvrBindingEditor.DataContext is NvrBindingEditorViewModel model &&
               !string.IsNullOrEmpty(obj.DevicePath)) {
                model.Identifier = Identifier;
                model.NvrBindingParamInfoModel = new NvrBindingParamInfoModel() {
                    BindingSource = SourceType.BarcodeScanner,
                    DisplayIdentifier = $"{obj.DeviceName}-{obj.ManufacturerName}",
                    SerialNumber = obj.DevicePath
                };
                await DialogHost.Show(nvrBindingEditor, model.Identifier);
            }
        }

        public ICommand TcpInputNvrSettingsCommand => new DelegateCommand<object>(TcpInputNvrSettingsDelegate);

        private async void TcpInputNvrSettingsDelegate(object obj) {
            var nvrBindingEditor = new NvrBindingEditor();
            if (nvrBindingEditor.DataContext is NvrBindingEditorViewModel model &&
                TcpSettingsInfo.ConnectionMode != null) {
                model.Identifier = Identifier;

                var displayIdentifier = TcpSettingsInfo.ConnectionMode == TcpConnectionMode.Server ? $"{TcpSettingsInfo.ServerConfig.IpAddress}:{TcpSettingsInfo.ServerConfig.Port}" : $"{TcpSettingsInfo.ClientConfig.IpAddress}:{TcpSettingsInfo.ClientConfig.Port}";

                model.NvrBindingParamInfoModel = new NvrBindingParamInfoModel() {
                    BindingSource = SourceType.Tcp,
                    DisplayIdentifier = displayIdentifier,
                    SerialNumber = displayIdentifier
                };
                await DialogHost.Show(nvrBindingEditor, model.Identifier);
            }
            else {
                base.MessageQueue.Enqueue("未选择连接方式");
            }
        }

        public ICommand ControlInputNvrSettingsCommand => new DelegateCommand<object>(ControlInputNvrSettingsDelegate);

        private async void ControlInputNvrSettingsDelegate(object obj) {
            var nvrBindingEditor = new NvrBindingEditor();
            if (nvrBindingEditor.DataContext is NvrBindingEditorViewModel model) {
                model.Identifier = Identifier;

                model.NvrBindingParamInfoModel = new NvrBindingParamInfoModel() {
                    BindingSource = SourceType.Input,
                    DisplayIdentifier = Environment.MachineName,
                    SerialNumber = Environment.MachineName
                };
                await DialogHost.Show(nvrBindingEditor, model.Identifier);
            }
        }

        protected override async Task<bool> SaveSettingsProcess() {
            var insertOrUpdate = await _configRepository.InsertOrUpdate(new ConfigInfoModel() {
                ConfigName = SettingsName,
                Value = JsonConvert.SerializeObject(new ContentInputSettingsDto {
                    IsUseControlInput = IsUseControlInput,
                    IsUseTcpInput = IsUseTcpInput,
                    IsUseBarcodeScannerInput = IsUseBarcodeScannerInput,
                    IsUseRegularFilter = IsUseRegularFilter,
                    DataTemplate = DataTemplate.Select(s => new ItemTemplateInfo() {
                        ApplicationType = s.ApplicationType,
                        Content = s.Content,
                        Type = s.Type
                    })?.ToList() ?? new List<ItemTemplateInfo>(),
                    ControlInputInfo = new ControlInputInfo() {
                        IsReceiveBarcode = ControlInputInfo.IsReceiveBarcode,
                        IsReceiveHeight = ControlInputInfo.IsReceiveHeight,
                        IsReceiveLength = ControlInputInfo.IsReceiveLength,
                        IsReceiveVolume = ControlInputInfo.IsReceiveVolume,
                        IsReceiveWeight = ControlInputInfo.IsReceiveWeight,
                        IsReceiveWidth = ControlInputInfo.IsReceiveWidth,
                    },
                    TcpSettingsInfo = new TcpSettingsInfo() {
                        ConnectionMode = TcpSettingsInfo.ConnectionMode,
                        ClientConfig = new TcpInfo() {
                            IpAddress = TcpSettingsInfo.ClientConfig.IpAddress,
                            Port = TcpSettingsInfo.ClientConfig.Port,
                        },
                        ServerConfig = new TcpInfo() {
                            IpAddress = TcpSettingsInfo.ServerConfig.IpAddress,
                            Port = TcpSettingsInfo.ServerConfig.Port,
                        }
                    },
                    Separator = Separator,
                    KeyboardDevice = KeyboardDevice,
                })
            });
            base.MessageQueue.Enqueue($"{(insertOrUpdate ? Languages.Language.ResourceManager.GetString("SaveSuccessful") :
                Languages.Language.ResourceManager.GetString("SaveFailed"))}");
            return insertOrUpdate;
        }

        public override async void LoadedDelegate(object obj) {
            var settingsDto = await _configRepository.FirstOrDefaultEntity<ContentInputSettingsDto>(SettingsName) ?? new ContentInputSettingsDto();
            KeyboardDevice = settingsDto.KeyboardDevice;
            IsUseTcpInput = settingsDto.IsUseTcpInput;
            IsUseControlInput = settingsDto.IsUseControlInput;
            IsUseBarcodeScannerInput = settingsDto.IsUseBarcodeScannerInput;
            IsUseRegularFilter = settingsDto.IsUseRegularFilter;
            ControlInputInfo = new ControlInputInfoModel {
                IsReceiveBarcode = settingsDto.ControlInputInfo.IsReceiveBarcode,
                IsReceiveHeight = settingsDto.ControlInputInfo.IsReceiveHeight,
                IsReceiveLength = settingsDto.ControlInputInfo.IsReceiveLength,
                IsReceiveVolume = settingsDto.ControlInputInfo.IsReceiveVolume,
                IsReceiveWeight = settingsDto.ControlInputInfo.IsReceiveWeight,
                IsReceiveWidth = settingsDto.ControlInputInfo.IsReceiveWidth
            };
            TcpSettingsInfo = new TcpSettingsInfoModel {
                ConnectionMode = settingsDto.TcpSettingsInfo.ConnectionMode,
                ClientConfig = new TcpInfoModel() {
                    IpAddress = settingsDto.TcpSettingsInfo.ClientConfig.IpAddress,
                    Port = settingsDto.TcpSettingsInfo.ClientConfig.Port
                },
                ServerConfig = new TcpInfoModel() {
                    IpAddress = settingsDto.TcpSettingsInfo.ServerConfig.IpAddress,
                    Port = settingsDto.TcpSettingsInfo.ServerConfig.Port
                }
            };
            Separator = settingsDto.Separator;
            var templateModels = settingsDto.DataTemplate.Select(s => new ItemBaseTemplateModel() {
                ApplicationType = s.ApplicationType,
                Content = s.Content,
                Type = s.Type
            })?.ToList();
            DataTemplate.Clear();
            DataTemplate.AddRange(templateModels);
            if (IsUseBarcodeScannerInput) {
                RefreshBarcodeScanner();
            }
            //加载键盘
        }

        public void RefreshBarcodeScanner() {
            Task.Run(async () => {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                    KeyboardDeviceItemInfo.Clear();
                    var keyboardDevices = await _keyboardDeviceManager.EnumerateKeyboardDevices();

                    if (keyboardDevices.Any()) {
                        var infoModels = keyboardDevices.Where(w => w is { DevicePath: not null, DeviceName: not null }).Select((s, i) => new KeyboardDeviceItemInfoModel {
                            DeviceName = s.DeviceName,
                            DevicePath = s.DevicePath,
                            IsConnected = s.IsConnected,
                            ManufacturerName = s.ManufacturerName,
                            ProductId = s.ProductId,
                            VendorId = s.VendorId,
                            HasBinding = (s is { ProductId: > 0, VendorId: > 0 } && KeyboardDevice.ProductId == s.ProductId && KeyboardDevice.VendorId == s.VendorId && KeyboardDevice.DevicePath == s.DevicePath),
                            Num = i + 1,
                        }).ToList();

                        KeyboardDeviceItemInfo.AddRange(infoModels);
                        if (KeyboardDeviceItemInfo.All(a => !a.HasBinding)) {
                            KeyboardDevice = new KeyboardDevice();
                        }
                    }
                });
            });
        }
    }
}