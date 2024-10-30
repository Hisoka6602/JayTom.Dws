using System;
using Prism.Mvvm;
using System.Net;
using System.Linq;
using Prism.Commands;
using Newtonsoft.Json;
using System.Windows.Input;
using JayTom.Dws.Domain.Dto;
using JayTom.Dws.Plugin.Tcp;
using System.Threading.Tasks;
using JayTom.Dws.Data.Package;
using MaterialDesignThemes.Wpf;
using JayTom.Dws.Data.LocalConf;
using NPOI.SS.Formula.Functions;
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
using JayTom.Dws.Client.Service.ExternalDataService.Communication.TcpComm;
using KeyboardDevice = JayTom.Dws.Plugin.Device.KeyboardDevice.KeyboardDevice;

namespace JayTom.Dws.Client.ViewModels.Pages.Preferences {

    public class ContentInputSettingsPageViewModel : SettingsPageTemplateViewModel {
        private readonly IKeyboardDeviceManager _keyboardDeviceManager;
        private readonly IClusterTcpInputManager _clusterTcpInputManager;
        private bool _isUseTcpInput;
        private bool _isUseControlInput;
        private ControlInputInfoModel _controlInputInfo = new();
        private TcpSettingsInfoModel _tcpSettingsInfo = new();
        private ObservableCollection<ItemBaseTemplateModel> _dataTemplate = new();
        private string _separator = string.Empty;
        private bool _isUseBarcodeScannerInput;
        private bool _isUseRegularFilter;
        private ObservableCollection<KeyboardDeviceItemInfoModel> _keyboardDeviceItemInfo = new();
        private ObservableCollection<TcpInputBindingInfoModel> _tcpInputBindingInfos = new();
        private string _startIpAddress = "127.0.0.1";
        private string _endIpAddress = "127.0.0.2";
        private bool _isTcpReconnecting;

        public ContentInputSettingsPageViewModel(IConfigRepository configRepository,
            IKeyboardDeviceManager keyboardDeviceManager, IClusterTcpInputManager clusterTcpInputManager) : base(configRepository) {
            _keyboardDeviceManager = keyboardDeviceManager;
            _clusterTcpInputManager = clusterTcpInputManager;

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

            _clusterTcpInputManager.ConnectionSuccessful += async (sender, model) => {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    var tcpInputBindingInfoModel = TcpInputBindingInfos.FirstOrDefault(f => f.IpAddress.Equals(model.IpAddress) &&
                        f.Port.Equals(model.Port));
                    if (tcpInputBindingInfoModel is not null) {
                        tcpInputBindingInfoModel.ConnectionStatus = TcpConnectionStatus.Connected;
                    }
                });
            };
            _clusterTcpInputManager.Disconnected += async (sender, model) => {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    var tcpInputBindingInfoModel = TcpInputBindingInfos.FirstOrDefault(f => f.IpAddress.Equals(model.IpAddress) &&
                        f.Port.Equals(model.Port));
                    if (tcpInputBindingInfoModel is not null) {
                        tcpInputBindingInfoModel.ConnectionStatus = TcpConnectionStatus.Disconnected;
                    }
                });
            };
            _clusterTcpInputManager.ConnectionFailed += async (sender, model) => {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    var tcpInputBindingInfoModel = TcpInputBindingInfos.FirstOrDefault(f =>
                        f.IpAddress.Equals(model.IpAddress) &&
                        f.Port.Equals(model.Port));
                    if (tcpInputBindingInfoModel is not null) {
                        tcpInputBindingInfoModel.ConnectionStatus = TcpConnectionStatus.ConnectionFailed;
                    }
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

        public ObservableCollection<TcpInputBindingInfoModel> TcpInputBindingInfos {
            get => _tcpInputBindingInfos;
            set => SetProperty(ref _tcpInputBindingInfos, value);
        }

        public ObservableCollection<KeyboardDeviceItemInfoModel> KeyboardDeviceItemInfo {
            get => _keyboardDeviceItemInfo;
            set => SetProperty(ref _keyboardDeviceItemInfo, value);
        }

        /// <summary>
        /// 起始IP地址
        /// </summary>
        public string StartIpAddress {
            get => _startIpAddress;
            set => SetProperty(ref _startIpAddress, value);
        }

        /// <summary>
        /// 结束IP地址
        /// </summary>
        public string EndIpAddress {
            get => _endIpAddress;
            set => SetProperty(ref _endIpAddress, value);
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 2000;

        /// <summary>
        /// Tcp是否正在重连中
        /// </summary>
        public bool IsTcpReconnecting {
            get => _isTcpReconnecting;
            set => SetProperty(ref _isTcpReconnecting, value);
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

        public ICommand NvrSettingsCommand => new DelegateCommand<object>(NvrSettingsDelegate);

        private async void NvrSettingsDelegate(object obj) {
            var nvrBindingEditor = new NvrBindingEditor();

            if (nvrBindingEditor.DataContext is not NvrBindingEditorViewModel model)
                return;

            // 抽取初始化方法
            NvrBindingParamInfoModel CreateNvrBindingParamInfo(SourceType sourceType, string displayIdentifier, string serialNumber) {
                return new NvrBindingParamInfoModel {
                    BindingSource = sourceType,
                    DisplayIdentifier = displayIdentifier,
                    SerialNumber = serialNumber
                };
            }

            // 处理 KeyboardDeviceItemInfoModel
            if (obj is KeyboardDeviceItemInfoModel info && !string.IsNullOrEmpty(info.DevicePath)) {
                model.NvrBindingParamInfoModel = CreateNvrBindingParamInfo(
                    SourceType.BarcodeScanner,
                    $"{info.DeviceName}-{info.ManufacturerName}",
                    info.DevicePath
                );
            }
            // 处理 TcpInputBindingInfoModel
            else if (obj is TcpInputBindingInfoModel tcpInfo && !string.IsNullOrEmpty(tcpInfo.IpAddress) && tcpInfo.Port > 0) {
                model.NvrBindingParamInfoModel = CreateNvrBindingParamInfo(
                    SourceType.Tcp,
                    $"{tcpInfo.IpAddress}-{tcpInfo.Port}",
                    $"{tcpInfo.IpAddress}-{tcpInfo.Port}"
                );
            }
            else {
                return; // 无效的 obj 参数直接返回
            }

            model.Identifier = Identifier;

            await DialogHost.Show(nvrBindingEditor, model.Identifier);
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
                    ControlInputInfo = new ControlInputInfo() {
                        IsReceiveBarcode = ControlInputInfo.IsReceiveBarcode,
                        IsReceiveHeight = ControlInputInfo.IsReceiveHeight,
                        IsReceiveLength = ControlInputInfo.IsReceiveLength,
                        IsReceiveVolume = ControlInputInfo.IsReceiveVolume,
                        IsReceiveWeight = ControlInputInfo.IsReceiveWeight,
                        IsReceiveWidth = ControlInputInfo.IsReceiveWidth,
                    },
                    KeyboardDevice = KeyboardDevice,
                    TcpInputBindingInfos = TcpInputBindingInfos.Select(s => new TcpInputBindingInfo {
                        IpAddress = s.IpAddress,
                        IsBound = s.IsBound,
                        Port = s.Port,
                    }).ToList()
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
            TcpInputBindingInfos = new ObservableCollection<TcpInputBindingInfoModel>(settingsDto.TcpInputBindingInfos.Select((s, i) => new TcpInputBindingInfoModel {
                Num = i + 1,
                IpAddress = s.IpAddress,
                IsBound = s.IsBound,
                Port = s.Port,
                ConnectionStatus = _clusterTcpInputManager.GetTcpInputInfo(s.IpAddress, s.Port)?.ConnectionStatus ==
                                   ConnectionStatus.Connected ? TcpConnectionStatus.Connected : TcpConnectionStatus.Disconnected
            }).ToList());

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

        /// <summary>
        /// 添加TCP连接命令
        /// </summary>
        public ICommand AddTcpConnectionCommand => new DelegateCommand<object>(AddTcpConnectionDelegate);

        private async void AddTcpConnectionDelegate(object obj) {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                if (string.IsNullOrEmpty(StartIpAddress) || Port <= 0) {
                    base.MessageQueue.Enqueue("起始Ip和端口都不能为空!");
                    return;
                }

                if (string.IsNullOrEmpty(EndIpAddress)) {
                    //判断IP+端口是否有重复
                    //添加一个
                    var any = TcpInputBindingInfos.Any(a => $"{a.IpAddress}&{a.Port}".Equals($"{StartIpAddress}&{Port}"));
                    if (any) {
                        base.MessageQueue.Enqueue("已存在相同项!");
                        return;
                    }
                    TcpInputBindingInfos.Add(new TcpInputBindingInfoModel() {
                        IpAddress = StartIpAddress,
                        Port = Port,
                    });
                }
                else {
                    var newItems = GetIpRange(StartIpAddress, EndIpAddress)
                        .Where(ip => TcpInputBindingInfos.All(a => $"{a.IpAddress}&{a.Port}" != $"{ip}&{Port}"))
                        .Select(ip => new TcpInputBindingInfoModel {
                            IpAddress = ip,
                            Port = Port,
                        })
                        .ToList();

                    TcpInputBindingInfos.AddRange(newItems);
                }

                for (var i = 0; i < TcpInputBindingInfos.Count; i++) {
                    TcpInputBindingInfos[i].Num = i + 1;
                }

                base.MessageQueue.Enqueue("添加成功");
            });
        }

        /// <summary>
        /// TCP全部重连命令
        /// </summary>
        public ICommand ReconnectAllTcpCommand => new DelegateCommand<object>(ReconnectAllTcpDelegate);

        private void ReconnectAllTcpDelegate(object obj) {
            if (!IsTcpReconnecting) {
                IsTcpReconnecting = true;
                Task.Run(async () => {
                    foreach (var tcpInputBindingInfoModel in TcpInputBindingInfos) {
                        tcpInputBindingInfoModel.ConnectionStatus = TcpConnectionStatus.Connecting;
                    }
                    var (key, value) = await _clusterTcpInputManager.ConnectBatch(TcpInputBindingInfos.Select(s => new TcpInputBindingInfo() {
                        IpAddress = s.IpAddress,
                        Port = s.Port,
                    }).ToList());
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        IsTcpReconnecting = false;
                        var tcpInputBindingInfoModels = TcpInputBindingInfos.Where(w => w.ConnectionStatus == TcpConnectionStatus.Connecting).ToList();
                        foreach (var model in tcpInputBindingInfoModels) {
                            model.ConnectionStatus = TcpConnectionStatus.ConnectionFailed;
                        }
                        base.MessageQueue.Enqueue(value);
                    });
                });
            }
        }

        /// <summary>
        /// Tcp全部删除
        /// </summary>
        public ICommand TcpDeleteAllCommand => new DelegateCommand<object>(TcpDeleteAllDelegate);

        private async void TcpDeleteAllDelegate(object obj) {
            await _clusterTcpInputManager.DisconnectAll();
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                TcpInputBindingInfos.Clear();
            });
        }

        /// <summary>
        /// TCP解绑命令
        /// </summary>
        public ICommand UnbindTcpCommand => new DelegateCommand<object>(UnbindTcpDelegate);

        private void UnbindTcpDelegate(object obj) {
            if (obj is TcpInputBindingInfoModel info) {
                info.IsBound = false;
            }
        }

        /// <summary>
        /// TCP删除命令
        /// </summary>
        public ICommand DeleteTcpCommand => new DelegateCommand<object>(DeleteTcpDelegate);

        private async void DeleteTcpDelegate(object obj) {
            if (obj is TcpInputBindingInfoModel info) {
                _clusterTcpInputManager.Disconnect(new TcpInputBindingInfo() {
                    IpAddress = info.IpAddress,
                    Port = info.Port,
                });
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    TcpInputBindingInfos.Remove(info);
                });

                for (var i = 0; i < TcpInputBindingInfos.Count; i++) {
                    TcpInputBindingInfos[i].Num = i + 1;
                }
                base.MessageQueue.Enqueue("删除成功");
            }
        }

        /// <summary>
        /// TCP绑定命令
        /// </summary>
        public ICommand BindTcpCommand => new DelegateCommand<object>(BindTcpDelegate);

        private void BindTcpDelegate(object obj) {
            if (obj is TcpInputBindingInfoModel info) {
                info.IsBound = true;
            }
        }

        /// <summary>
        /// TCP重新连接命令
        /// </summary>
        public ICommand ReconnectTcpCommand => new DelegateCommand<object>(ReconnectTcpDelegate);

        private void ReconnectTcpDelegate(object obj) {
            if (obj is TcpInputBindingInfoModel info && info.ConnectionStatus != TcpConnectionStatus.Connecting) {
                info.ConnectionStatus = TcpConnectionStatus.Connecting;
                Task.Run(async () => {
                    var connect = await _clusterTcpInputManager.Connect(new TcpInputBindingInfo() {
                        IpAddress = info.IpAddress,
                        Port = info.Port,
                    });
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        base.MessageQueue.Enqueue(connect ? "连接成功" : "连接失败");
                        if (!connect) {
                            info.ConnectionStatus = TcpConnectionStatus.ConnectionFailed;
                        }
                    });
                });
            }

            //_clusterTcpInputManager
        }

        public IEnumerable<string> GetIpRange(string startIp, string endIp) {
            // 将 IP 地址转换为整数
            uint start = IpToInt(startIp);
            uint end = IpToInt(endIp);

            // 使用 LINQ 生成范围内的所有 IP 地址
            return Enumerable.Range((int)start, (int)(end - start + 1))
                .Select(IpFromInt);
        }

        // 将 IP 地址转换为整数
        public uint IpToInt(string ipAddress) {
            return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(IPAddress.Parse(ipAddress).GetAddressBytes(), 0));
        }

        // 将整数转换为 IP 地址
        public string IpFromInt(int address) {
            return new IPAddress(BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder(address))).ToString();
        }
    }
}